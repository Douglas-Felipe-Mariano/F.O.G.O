using Fogo.Api.Identity;
using Fogo.Api.Inventario;
using Fogo.Api.Operacoes;
using Microsoft.EntityFrameworkCore;

namespace Fogo.Api.Application.Operacoes;

public sealed class OperacoesService(FogoDbContext banco, TimeProvider relogio)
{
    private static readonly TimeZoneInfo FusoBrasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public async Task<IReadOnlyCollection<TransitoSegmentoResposta>> ConsultarTransitoAsync(CancellationToken cancelamento)
    {
        var agora = relogio.GetUtcNow();
        var observacoes = await banco.ObservacoesTransito.AsNoTracking()
            .Where(item => item.RegistradaEm >= agora.AddDays(-28))
            .OrderBy(item => item.RegistradaEm)
            .ToListAsync(cancelamento);
        var porSegmento = observacoes.GroupBy(item => item.SegmentoId).ToDictionary(grupo => grupo.Key, grupo => grupo.ToArray());
        var segmentos = await banco.SegmentosTransito.AsNoTracking()
            .OrderBy(item => item.Cidade).ThenBy(item => item.Nome)
            .ToListAsync(cancelamento);
        var horizonte = agora.AddHours(1);
        var horarioPrevistoLocal = TimeZoneInfo.ConvertTime(horizonte, FusoBrasilia);
        var respostas = new List<TransitoSegmentoResposta>(segmentos.Count);

        foreach (var segmento in segmentos)
        {
            if (!porSegmento.TryGetValue(segmento.Id, out var historico) || historico.Length == 0)
            {
                continue;
            }

            var ultima = historico[^1];
            var amostrasComparaveis = historico.Where(item =>
            {
                var local = TimeZoneInfo.ConvertTime(item.RegistradaEm, FusoBrasilia);
                return local.DayOfWeek == horarioPrevistoLocal.DayOfWeek && local.Hour == horarioPrevistoLocal.Hour;
            }).ToArray();
            if (amostrasComparaveis.Length == 0)
            {
                amostrasComparaveis = historico.TakeLast(Math.Min(7, historico.Length)).ToArray();
            }

            var volumePrevisto = (int)Math.Round(amostrasComparaveis.Average(item => item.VeiculosUltimaHora));
            var velocidadePrevista = decimal.Round(amostrasComparaveis.Average(item => item.VelocidadeMediaKmh), 1);
            var congestionamentoPrevisto = (int)Math.Round(amostrasComparaveis.Average(item => item.CongestionamentoPercentual));
            var confianca = amostrasComparaveis.Length >= 8 ? "media" : amostrasComparaveis.Length >= 4 ? "baixa" : "muitoBaixa";
            respostas.Add(new TransitoSegmentoResposta(
                segmento.Id,
                segmento.Nome,
                segmento.Cidade,
                segmento.ReferenciaMapa,
                segmento.Latitude,
                segmento.Longitude,
                ultima.VeiculosUltimaHora,
                ultima.VelocidadeMediaKmh,
                ultima.CongestionamentoPercentual,
                ultima.RegistradaEm,
                ultima.OrigemDados,
                volumePrevisto,
                velocidadePrevista,
                congestionamentoPrevisto,
                amostrasComparaveis.Length,
                confianca,
                horizonte));
        }

        return respostas;
    }

    public async Task<IReadOnlyCollection<AlertaOperacionalResposta>> ConsultarAlertasAsync(CancellationToken cancelamento)
    {
        var agora = relogio.GetUtcNow();
        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(agora, FusoBrasilia).DateTime);
        var fimJanela = hoje.AddDays(7);
        var alertas = new List<AlertaOperacionalResposta>();
        var equipamentos = await banco.Equipamentos.AsNoTracking()
            .Where(item => item.NecessitaManutencao)
            .ToListAsync(cancelamento);

        foreach (var equipamento in equipamentos)
        {
            var (tipo, severidade, titulo, descricao) = equipamento.TipoManutencao switch
            {
                TipoManutencao.Imediata => ("manutencaoImediata", "critica", "Manutenção imediata", $"{equipamento.Tipo} · série {equipamento.NumeroSerie} requer manutenção imediata."),
                TipoManutencao.Agendada or TipoManutencao.Periodica when equipamento.ProximaManutencao is { } dataVencida && dataVencida < hoje => ("manutencaoVencida", "alta", "Manutenção vencida", $"{equipamento.Tipo} · série {equipamento.NumeroSerie}; prazo {equipamento.ProximaManutencao:yyyy-MM-dd}."),
                TipoManutencao.Agendada or TipoManutencao.Periodica when equipamento.ProximaManutencao is { } dataProxima && dataProxima <= fimJanela => ("manutencaoProxima", "media", "Manutenção próxima", $"{equipamento.Tipo} · série {equipamento.NumeroSerie}; prevista para {equipamento.ProximaManutencao:yyyy-MM-dd}."),
                _ => (string.Empty, string.Empty, string.Empty, string.Empty)
            };
            if (tipo.Length == 0)
            {
                continue;
            }

            alertas.Add(new AlertaOperacionalResposta(
                tipo, severidade, titulo, descricao, equipamento.Id, equipamento.Cidade,
                equipamento.Latitude, equipamento.Longitude, equipamento.OrigemDados, agora));
        }

        var equipamentosAtipicos = await banco.Equipamentos.AsNoTracking()
            .Where(item => item.Situacao == SituacaoEquipamento.MauFuncionamento
                || item.Situacao == SituacaoEquipamento.Furtado)
            .ToListAsync(cancelamento);
        foreach (var equipamento in equipamentosAtipicos)
        {
            var furtado = equipamento.Situacao == SituacaoEquipamento.Furtado;
            alertas.Add(new AlertaOperacionalResposta(
                furtado ? "equipamentoFurtado" : "equipamentoMauFuncionamento",
                furtado ? "critica" : "alta",
                furtado ? "Equipamento informado como furtado" : "Equipamento com mau funcionamento",
                $"{equipamento.Tipo} · série {equipamento.NumeroSerie}; situação declarada no cadastro.",
                equipamento.Id, equipamento.Cidade, equipamento.Latitude, equipamento.Longitude, equipamento.OrigemDados, equipamento.AtualizadoEm));
        }

        var transito = await ConsultarTransitoAsync(cancelamento);
        var observacoesRecentes = await banco.ObservacoesTransito.AsNoTracking()
            .Where(item => item.RegistradaEm >= agora.AddDays(-28))
            .ToListAsync(cancelamento);
        foreach (var segmento in transito)
        {
            var historicoSegmento = observacoesRecentes
                .Where(item => item.SegmentoId == segmento.Id && item.RegistradaEm < segmento.AmostraEm)
                .ToArray();
            if (historicoSegmento.Length == 0)
            {
                continue;
            }

            var mediaHistorica = historicoSegmento.Average(item => item.CongestionamentoPercentual);
            if (segmento.CongestionamentoPercentual >= mediaHistorica + 25)
            {
                alertas.Add(new AlertaOperacionalResposta(
                    "transitoAtipico", "alta", "Volume de trânsito fora do padrão",
                    $"Índice atual de {segmento.CongestionamentoPercentual}% supera em pelo menos 25 pontos a média histórica ({Math.Round(mediaHistorica)}%). Regra heurística; validar no local.",
                    segmento.Id, segmento.Cidade, segmento.Latitude, segmento.Longitude, segmento.OrigemDados, segmento.AmostraEm));
            }

            if (segmento.CongestionamentoPrevistoPercentual >= 75)
            {
                alertas.Add(new AlertaOperacionalResposta(
                    "congestionamentoPrevisto", "media", "Possível congestionamento na próxima hora",
                    $"Projeção heurística de {segmento.CongestionamentoPrevistoPercentual}% para {segmento.HorizontePrevisto:HH:mm} (horário de Brasília), baseada em {segmento.AmostrasHistoricas} amostra(s) histórica(s) comparável(is).",
                    segmento.Id, segmento.Cidade, segmento.Latitude, segmento.Longitude, segmento.OrigemDados, agora));
            }
        }

        var acidentesAbertos = await banco.OcorrenciasAcidente.AsNoTracking()
            .Where(item => item.Situacao != SituacaoAcidente.Encerrado)
            .ToListAsync(cancelamento);
        alertas.AddRange(acidentesAbertos.Select(acidente => new AlertaOperacionalResposta(
            "acidenteAberto",
            acidente.Grau is GrauAcidente.Grave or GrauAcidente.Critico || acidente.HaFeridos ? "critica" : "alta",
            "Acidente em atendimento",
            $"{acidente.LocalDescricao} · grau {acidente.Grau} · impacto {acidente.ImpactoTransito}; atendimento não confirmado por integração externa.",
            acidente.Id, acidente.Cidade, acidente.Latitude, acidente.Longitude, OrigemRegistro.Manual, acidente.RegistradoEm)));

        return alertas.OrderByDescending(item => item.Severidade switch { "critica" => 3, "alta" => 2, "media" => 1, _ => 0 })
            .ThenBy(item => item.Cidade).ToArray();
    }

    public async Task<IReadOnlyCollection<AcidenteResposta>> ListarAcidentesAsync(CancellationToken cancelamento)
    {
        var acidentes = await banco.OcorrenciasAcidente.AsNoTracking()
            .OrderByDescending(item => item.Situacao != SituacaoAcidente.Encerrado)
            .ThenByDescending(item => item.RegistradoEm)
            .ToListAsync(cancelamento);
        return acidentes.Select(AcidenteResposta.De).ToArray();
    }

    public async Task<AcidenteResposta?> ConsultarAcidenteAsync(Guid acidenteId, CancellationToken cancelamento)
    {
        var acidente = await banco.OcorrenciasAcidente.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == acidenteId, cancelamento);
        return acidente is null ? null : AcidenteResposta.De(acidente);
    }

    public async Task<AcidenteResposta> RegistrarAcidenteAsync(AcidenteRequisicao requisicao, CancellationToken cancelamento)
    {
        var acidente = new OcorrenciaAcidente
        {
            LocalDescricao = requisicao.LocalDescricao.Trim(),
            Cidade = requisicao.Cidade.Trim(),
            Latitude = requisicao.Latitude,
            Longitude = requisicao.Longitude,
            Grau = requisicao.Grau,
            ImpactoTransito = requisicao.ImpactoTransito,
            HaFeridos = requisicao.HaFeridos,
            RegistradoEm = relogio.GetUtcNow()
        };
        banco.OcorrenciasAcidente.Add(acidente);
        await banco.SaveChangesAsync(cancelamento);
        return AcidenteResposta.De(acidente);
    }

    public async Task<(AcidenteResposta? Acidente, bool AgenteNaoEncontrado)> SolicitarDespachoAsync(
        Guid acidenteId,
        Guid? agenteId,
        CancellationToken cancelamento)
    {
        var acidente = await banco.OcorrenciasAcidente.FirstOrDefaultAsync(item => item.Id == acidenteId, cancelamento);
        if (acidente is null)
        {
            return (null, false);
        }

        if (agenteId.HasValue && !await banco.Agentes.AnyAsync(item => item.Id == agenteId.Value, cancelamento))
        {
            return (AcidenteResposta.De(acidente), true);
        }

        acidente.DespachoSolicitadoEm ??= relogio.GetUtcNow();
        acidente.AgenteDesignadoId = agenteId ?? acidente.AgenteDesignadoId;
        if (acidente.Situacao == SituacaoAcidente.Aberto)
        {
            acidente.Situacao = SituacaoAcidente.AtendimentoSolicitado;
        }
        await banco.SaveChangesAsync(cancelamento);
        return (AcidenteResposta.De(acidente), false);
    }

    public async Task<AcidenteResposta?> SolicitarSocorroAsync(Guid acidenteId, CancellationToken cancelamento)
    {
        var acidente = await banco.OcorrenciasAcidente.FirstOrDefaultAsync(item => item.Id == acidenteId, cancelamento);
        if (acidente is null)
        {
            return null;
        }

        acidente.SocorroSolicitadoEm ??= relogio.GetUtcNow();
        if (acidente.Situacao == SituacaoAcidente.Aberto)
        {
            acidente.Situacao = SituacaoAcidente.AtendimentoSolicitado;
        }
        await banco.SaveChangesAsync(cancelamento);
        return AcidenteResposta.De(acidente);
    }

    public async Task<AcidenteResposta?> EncerrarAcidenteAsync(Guid acidenteId, CancellationToken cancelamento)
    {
        var acidente = await banco.OcorrenciasAcidente.FirstOrDefaultAsync(item => item.Id == acidenteId, cancelamento);
        if (acidente is null)
        {
            return null;
        }

        acidente.Situacao = SituacaoAcidente.Encerrado;
        await banco.SaveChangesAsync(cancelamento);
        return AcidenteResposta.De(acidente);
    }
}
