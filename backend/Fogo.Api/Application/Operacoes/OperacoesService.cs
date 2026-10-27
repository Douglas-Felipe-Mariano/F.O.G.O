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

    public async Task<PredicoesOperacionaisResposta> ConsultarPredicoesAsync(CancellationToken cancelamento)
    {
        var agora = relogio.GetUtcNow();
        var transito = await ConsultarTransitoAsync(cancelamento);
        var equipamentos = await banco.Equipamentos.AsNoTracking().ToListAsync(cancelamento);
        var acidentes = await banco.OcorrenciasAcidente.AsNoTracking()
            .Where(item => item.RegistradoEm >= agora.AddDays(-90))
            .ToListAsync(cancelamento);

        var regioes = new Dictionary<string, RegiaoPredicao>(StringComparer.OrdinalIgnoreCase);
        RegiaoPredicao ObterRegiao(string cidade, string bairro)
        {
            var cidadeLimpa = cidade.Trim();
            var bairroLimpo = string.IsNullOrWhiteSpace(bairro) ? "Município" : bairro.Trim();
            var chave = $"{cidadeLimpa}\u001f{bairroLimpo}";
            if (!regioes.TryGetValue(chave, out var regiao))
            {
                regiao = new RegiaoPredicao(cidadeLimpa, bairroLimpo);
                regioes.Add(chave, regiao);
            }

            return regiao;
        }

        foreach (var grupo in equipamentos.GroupBy(item => new
                 {
                     Cidade = item.Cidade.Trim(),
                     Bairro = string.IsNullOrWhiteSpace(item.Bairro) ? "Município" : item.Bairro.Trim()
                 }))
        {
            ObterRegiao(grupo.Key.Cidade, grupo.Key.Bairro).Equipamentos.AddRange(grupo);
        }

        foreach (var segmento in transito)
        {
            var regiao = EncontrarRegiaoProxima(regioes.Values, segmento.Cidade, segmento.Latitude, segmento.Longitude);
            (regiao ?? ObterRegiao(segmento.Cidade, "Município")).Transito.Add(segmento);
        }

        foreach (var acidente in acidentes)
        {
            var regiao = EncontrarRegiaoProxima(regioes.Values, acidente.Cidade, acidente.Latitude, acidente.Longitude);
            (regiao ?? ObterRegiao(acidente.Cidade, "Município")).Acidentes.Add(acidente);
        }

        var indicePorRegiao = new List<IndiceRiscoRegiaoResposta>();
        var recomendacoes = new List<RecomendacaoReforcoResposta>();
        foreach (var regiao in regioes.Values)
        {
            var congestionamentoPrevisto = regiao.Transito.Count > 0
                ? (int)Math.Round(regiao.Transito.Average(item => item.CongestionamentoPrevistoPercentual))
                : 0;
            var riscoEquipamentos = CalcularRiscoEquipamentos(regiao.Equipamentos, agora);
            var riscoAcidentes = CalcularRiscoAcidentes(regiao.Acidentes, agora);
            var temTransito = regiao.Transito.Count > 0;
            var indice = (int)Math.Clamp(Math.Round(
                congestionamentoPrevisto * 0.45
                + riscoEquipamentos * 0.30
                + riscoAcidentes * 0.25), 0, 100);
            var fatores = new List<string>();
            if (temTransito && congestionamentoPrevisto >= 45)
                fatores.Add($"congestionamento previsto em {congestionamentoPrevisto}%");
            if (riscoEquipamentos > 0)
                fatores.Add($"{regiao.Equipamentos.Count(item => EquipamentoEmRisco(item, agora))} equipamento(s) com falha ou manutenção indicada");
            if (regiao.Acidentes.Count > 0)
                fatores.Add($"{regiao.Acidentes.Count} acidente(s) registrado(s) nos últimos 90 dias");

            if (regiao.Equipamentos.Count == 0 && !temTransito && regiao.Acidentes.Count == 0)
                continue;

            var simulados = regiao.Transito.Any(item => item.OrigemDados == OrigemRegistro.Simulado)
                || regiao.Equipamentos.Any(item => item.OrigemDados == OrigemRegistro.Simulado);
            var classificacao = indice switch
            {
                >= 80 => "Crítico",
                >= 60 => "Alto",
                >= 35 => "Moderado",
                _ => "Baixo"
            };
            indicePorRegiao.Add(new IndiceRiscoRegiaoResposta(
                regiao.Bairro,
                regiao.Cidade,
                indice,
                classificacao,
                congestionamentoPrevisto,
                regiao.Equipamentos.Count(item => EquipamentoEmRisco(item, agora)),
                regiao.Equipamentos.Count,
                regiao.Acidentes.Count,
                regiao.Transito.Sum(item => item.AmostrasHistoricas),
                simulados,
                fatores));

            if (indice >= 45)
            {
                var referencia = regiao.Transito
                    .OrderByDescending(item => item.CongestionamentoPrevistoPercentual)
                    .Select(item => item.Nome)
                    .FirstOrDefault()
                    ?? regiao.Equipamentos.FirstOrDefault(item => EquipamentoEmRisco(item, agora))?.Rua
                    ?? regiao.Bairro;
                var quantidadeAgentes = indice >= 75 ? 2 : 1;
                recomendacoes.Add(new RecomendacaoReforcoResposta(
                    regiao.Cidade,
                    regiao.Bairro,
                    referencia,
                    indice,
                    quantidadeAgentes,
                    agora,
                    agora.AddHours(1),
                    $"Avaliar reforço preventivo de {quantidadeAgentes} agente(s); índice heurístico {indice}/100. Confirme as condições no local antes de mobilizar a equipe."));
            }
        }

        var pontosCalor = new Dictionary<(decimal Latitude, decimal Longitude), int>();
        void AdicionarPonto(decimal? latitude, decimal? longitude, int peso)
        {
            if (latitude is null || longitude is null || peso <= 0)
                return;
            var chave = (Math.Round(latitude.Value, 5), Math.Round(longitude.Value, 5));
            pontosCalor[chave] = Math.Min(100, pontosCalor.GetValueOrDefault(chave) + peso);
        }

        foreach (var segmento in transito)
            AdicionarPonto(segmento.Latitude, segmento.Longitude, segmento.CongestionamentoPrevistoPercentual);
        foreach (var equipamento in equipamentos.Where(item => EquipamentoEmRisco(item, agora)))
            AdicionarPonto(equipamento.Latitude, equipamento.Longitude, PontuacaoEquipamento(equipamento, agora));
        foreach (var acidente in acidentes)
            AdicionarPonto(acidente.Latitude, acidente.Longitude, PontuacaoAcidente(acidente, agora));

        return new PredicoesOperacionaisResposta(
            indicePorRegiao.OrderByDescending(item => item.IndiceRisco).ThenBy(item => item.Cidade).ThenBy(item => item.Bairro).ToArray(),
            recomendacoes.OrderByDescending(item => item.IndiceRisco).ThenBy(item => item.Cidade).ThenBy(item => item.Regiao).ToArray(),
            pontosCalor.Select(item => new PontoCalorResposta(item.Key.Latitude, item.Key.Longitude, item.Value)).ToArray(),
            agora,
            "Índice heurístico explicável (45% trânsito previsto, 30% falhas/manutenção, 25% acidentes dos últimos 90 dias). Não é probabilidade nem modelo treinado; trânsito e dados de origem simulada são identificados. Recomendações não despacham agentes.");
    }

    private static RegiaoPredicao? EncontrarRegiaoProxima(
        IEnumerable<RegiaoPredicao> regioes,
        string cidade,
        decimal latitude,
        decimal longitude)
    {
        return regioes
            .Where(item => item.Equipamentos.Count > 0
                && string.Equals(item.Cidade, cidade.Trim(), StringComparison.OrdinalIgnoreCase)
                && item.Equipamentos.Any(equipamento => equipamento.Latitude.HasValue && equipamento.Longitude.HasValue))
            .Select(item => new
            {
                Regiao = item,
                Distancia = item.Equipamentos
                    .Where(equipamento => equipamento.Latitude.HasValue && equipamento.Longitude.HasValue)
                    .Min(equipamento => DistanciaMetros(latitude, longitude, equipamento.Latitude!.Value, equipamento.Longitude!.Value))
            })
            .Where(item => item.Distancia <= 2500)
            .OrderBy(item => item.Distancia)
            .Select(item => item.Regiao)
            .FirstOrDefault();
    }

    private static double DistanciaMetros(decimal latitudeA, decimal longitudeA, decimal latitudeB, decimal longitudeB)
    {
        var deltaLatitude = (double)(latitudeA - latitudeB) * 111_320;
        var deltaLongitude = (double)(longitudeA - longitudeB) * 111_320 * Math.Cos((double)latitudeA * Math.PI / 180);
        return Math.Sqrt(deltaLatitude * deltaLatitude + deltaLongitude * deltaLongitude);
    }

    private static int CalcularRiscoEquipamentos(IEnumerable<Equipamento> equipamentos, DateTimeOffset agora) =>
        Math.Min(100, equipamentos.Sum(item => PontuacaoEquipamento(item, agora)));

    private static bool EquipamentoEmRisco(Equipamento equipamento, DateTimeOffset agora) =>
        PontuacaoEquipamento(equipamento, agora) > 0;

    private static int PontuacaoEquipamento(Equipamento equipamento, DateTimeOffset agora)
    {
        var pontuacao = equipamento.Situacao switch
        {
            SituacaoEquipamento.MauFuncionamento => 40,
            SituacaoEquipamento.EmManutencao => 30,
            SituacaoEquipamento.Furtado => 35,
            _ => 0
        };
        if (equipamento.NecessitaManutencao)
        {
            var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(agora, FusoBrasilia).DateTime);
            var manutencao = equipamento.TipoManutencao switch
            {
                TipoManutencao.Imediata => 50,
                TipoManutencao.Agendada or TipoManutencao.Periodica when equipamento.ProximaManutencao < hoje => 40,
                TipoManutencao.Agendada or TipoManutencao.Periodica when equipamento.ProximaManutencao <= hoje.AddDays(7) => 20,
                _ => 15
            };
            pontuacao = Math.Max(pontuacao, manutencao);
        }

        return pontuacao;
    }

    private static int CalcularRiscoAcidentes(IEnumerable<OcorrenciaAcidente> acidentes, DateTimeOffset agora) =>
        Math.Min(100, acidentes.Sum(item => PontuacaoAcidente(item, agora)));

    private static int PontuacaoAcidente(OcorrenciaAcidente acidente, DateTimeOffset agora)
    {
        var baseRisco = acidente.Grau switch
        {
            GrauAcidente.Leve => 15,
            GrauAcidente.Moderado => 30,
            GrauAcidente.Grave => 50,
            GrauAcidente.Critico => 70,
            _ => 15
        };
        baseRisco += acidente.HaFeridos ? 20 : 0;
        baseRisco += acidente.ImpactoTransito switch
        {
            ImpactoTransito.Lentidao => 5,
            ImpactoTransito.BloqueioParcial => 15,
            ImpactoTransito.InterdicaoTotal => 20,
            _ => 0
        };
        var idadeDias = Math.Max(0, (agora - acidente.RegistradoEm).TotalDays);
        var fatorRecencia = idadeDias <= 7 ? 1m : idadeDias <= 30 ? 0.7m : 0.4m;
        return (int)Math.Round(baseRisco * fatorRecencia);
    }

    private sealed class RegiaoPredicao(string cidade, string bairro)
    {
        public string Cidade { get; } = cidade;
        public string Bairro { get; } = bairro;
        public List<Equipamento> Equipamentos { get; } = [];
        public List<TransitoSegmentoResposta> Transito { get; } = [];
        public List<OcorrenciaAcidente> Acidentes { get; } = [];
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
