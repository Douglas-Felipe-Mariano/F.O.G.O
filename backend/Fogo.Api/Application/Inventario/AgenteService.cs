using Fogo.Api.Inventario;
using Fogo.Api.Repositories;

namespace Fogo.Api.Application.Inventario;

public sealed class AgenteService(IAgenteRepository repositorio, TimeProvider relogio)
{
    public async Task<PaginaAgentesResposta> ListarAsync(FiltrosAgentes filtros, CancellationToken cancelamento)
    {
        var total = await repositorio.ContarAsync(filtros, cancelamento);
        var agentes = await repositorio.ListarAsync(filtros, cancelamento);
        return new PaginaAgentesResposta(
            filtros.Pagina,
            filtros.TamanhoPagina,
            total,
            agentes.Select(AgenteResposta.De).ToArray());
    }

    public async Task<AgenteResposta?> ConsultarAsync(Guid id, CancellationToken cancelamento)
    {
        var agente = await repositorio.ConsultarAsync(id, cancelamento);
        return agente is null ? null : AgenteResposta.De(agente);
    }

    public async Task<IndicadoresAgentesResposta> ConsultarIndicadoresAsync(CancellationToken cancelamento)
    {
        var porSituacao = await repositorio.ContarPorSituacaoAsync(cancelamento);
        return new IndicadoresAgentesResposta(porSituacao.Sum(item => item.Quantidade), porSituacao);
    }

    public async Task<MapaAgentesResposta> ListarLocalizacoesAsync(int limite, CancellationToken cancelamento)
    {
        var total = await repositorio.ContarComCoordenadasAsync(cancelamento);
        var agentes = await repositorio.ListarComCoordenadasAsync(limite, cancelamento);
        var localizacoes = agentes.Select(agente => new LocalizacaoAgenteResposta(
            agente.Id,
            agente.Nome,
            agente.CategoriaProfissional,
            agente.BaseOperacional,
            agente.Latitude!.Value,
            agente.Longitude!.Value,
            agente.Situacao)).ToArray();
        return new MapaAgentesResposta(total, limite, total > localizacoes.Length, localizacoes);
    }

    public async Task<ResultadoOperacao<AgenteResposta>> CriarAsync(AgenteRequisicao requisicao, CancellationToken cancelamento)
    {
        var erros = Validar(requisicao);
        if (erros.Count > 0)
        {
            return ResultadoOperacao<AgenteResposta>.Invalido(erros);
        }

        var agora = relogio.GetUtcNow();
        var agente = new Agente
        {
            Nome = requisicao.Nome.Trim(),
            CategoriaProfissional = requisicao.CategoriaProfissional.Trim(),
            BaseOperacional = requisicao.BaseOperacional.Trim(),
            Latitude = requisicao.Latitude,
            Longitude = requisicao.Longitude,
            Situacao = requisicao.Situacao,
            OrigemDados = requisicao.OrigemDados,
            CriadoEm = agora,
            AtualizadoEm = agora
        };
        await repositorio.AdicionarAsync(agente, cancelamento);
        return ResultadoOperacao<AgenteResposta>.Sucesso(AgenteResposta.De(agente));
    }

    public async Task<ResultadoOperacao<AgenteResposta>> AtualizarAsync(
        Guid id,
        AgenteRequisicao requisicao,
        CancellationToken cancelamento)
    {
        var erros = Validar(requisicao);
        if (erros.Count > 0)
        {
            return ResultadoOperacao<AgenteResposta>.Invalido(erros);
        }

        var agente = await repositorio.ConsultarParaAtualizacaoAsync(id, cancelamento);
        if (agente is null)
        {
            return ResultadoOperacao<AgenteResposta>.RecursoNaoEncontrado();
        }

        agente.Nome = requisicao.Nome.Trim();
        agente.CategoriaProfissional = requisicao.CategoriaProfissional.Trim();
        agente.BaseOperacional = requisicao.BaseOperacional.Trim();
        agente.Latitude = requisicao.Latitude;
        agente.Longitude = requisicao.Longitude;
        agente.Situacao = requisicao.Situacao;
        agente.OrigemDados = requisicao.OrigemDados;
        agente.AtualizadoEm = relogio.GetUtcNow();
        await repositorio.SalvarAlteracoesAsync(cancelamento);
        return ResultadoOperacao<AgenteResposta>.Sucesso(AgenteResposta.De(agente));
    }

    private static Dictionary<string, string[]> Validar(AgenteRequisicao requisicao)
    {
        var erros = new Dictionary<string, string[]>(StringComparer.Ordinal);
        ValidarObrigatorio(requisicao.Nome, 160, nameof(requisicao.Nome), erros);
        ValidarObrigatorio(requisicao.CategoriaProfissional, 120, nameof(requisicao.CategoriaProfissional), erros);
        ValidarObrigatorio(requisicao.BaseOperacional, 240, nameof(requisicao.BaseOperacional), erros);

        if (requisicao.Latitude.HasValue != requisicao.Longitude.HasValue)
        {
            erros[nameof(requisicao.Latitude)] = ["Informe latitude e longitude juntas, ou deixe ambas vazias."];
        }

        if (requisicao.Latitude is < -90 or > 90)
        {
            erros[nameof(requisicao.Latitude)] = ["A latitude deve estar entre -90 e 90."];
        }

        if (requisicao.Longitude is < -180 or > 180)
        {
            erros[nameof(requisicao.Longitude)] = ["A longitude deve estar entre -180 e 180."];
        }

        if (!Enum.IsDefined(requisicao.Situacao))
        {
            erros[nameof(requisicao.Situacao)] = ["Informe uma situação operacional válida."];
        }

        if (!Enum.IsDefined(requisicao.OrigemDados) || requisicao.OrigemDados == OrigemRegistro.NaoInformada)
        {
            erros[nameof(requisicao.OrigemDados)] = ["Informe se o registro é manual, importado ou simulado."];
        }

        return erros;
    }

    private static void ValidarObrigatorio(string? valor, int maximo, string campo, IDictionary<string, string[]> erros)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            erros[campo] = ["Este campo é obrigatório."];
        }
        else if (valor.Trim().Length > maximo)
        {
            erros[campo] = [$"O campo deve ter no máximo {maximo} caracteres."];
        }
    }
}
