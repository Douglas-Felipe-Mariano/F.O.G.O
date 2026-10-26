using Fogo.Api.Application.Inventario;
using Fogo.Api.Inventario;
using Fogo.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fogo.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/agentes")]
public sealed class AgentesController(
    AgenteService servico,
    IPublicadorEventosAgentes publicadorEventos) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginaAgentesResposta>> ListarAsync(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 25,
        [FromQuery] string? categoriaProfissional = null,
        [FromQuery] SituacaoAgente? situacao = null,
        CancellationToken cancelamento = default)
    {
        if (pagina < 1 || tamanhoPagina is < 1 or > 100 || (long)(pagina - 1) * tamanhoPagina > int.MaxValue)
        {
            return ValidationProblem(CriarErros(new Dictionary<string, string[]>
            {
                ["paginacao"] = ["A página deve ser maior que zero e o tamanho deve estar entre 1 e 100."]
            }));
        }

        if (situacao is { } situacaoInformada && !Enum.IsDefined(situacaoInformada))
        {
            return ValidationProblem(CriarErros(new Dictionary<string, string[]>
            {
                ["situacao"] = ["Informe uma situação válida para filtrar."]
            }));
        }

        return Ok(await servico.ListarAsync(
            new FiltrosAgentes(pagina, tamanhoPagina, categoriaProfissional, situacao),
            cancelamento));
    }

    [HttpGet("indicadores")]
    public async Task<ActionResult<IndicadoresAgentesResposta>> IndicadoresAsync(CancellationToken cancelamento) =>
        Ok(await servico.ConsultarIndicadoresAsync(cancelamento));

    [HttpGet("localizacoes")]
    public async Task<ActionResult<MapaAgentesResposta>> LocalizacoesAsync(
        [FromQuery] int limite = 500,
        [FromQuery] int deslocamento = 0,
        CancellationToken cancelamento = default)
    {
        if (limite is < 1 or > 1000 || deslocamento < 0)
        {
            return ValidationProblem(CriarErros(new Dictionary<string, string[]>
            {
                ["paginacao"] = ["O limite deve estar entre 1 e 1000 e o deslocamento não pode ser negativo."]
            }));
        }

        return Ok(await servico.ListarLocalizacoesAsync(deslocamento, limite, cancelamento));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AgenteResposta>> ConsultarAsync(Guid id, CancellationToken cancelamento)
    {
        var agente = await servico.ConsultarAsync(id, cancelamento);
        return agente is null ? NotFound() : Ok(agente);
    }

    [HttpPost]
    public async Task<ActionResult<AgenteResposta>> CriarAsync(AgenteRequisicao requisicao, CancellationToken cancelamento)
    {
        var resultado = await servico.CriarAsync(requisicao, cancelamento);
        if (!resultado.SucessoOperacao)
        {
            return resultado.Erros.Count > 0
                ? ValidationProblem(CriarErros(resultado.Erros))
                : Problem(statusCode: 500);
        }

        var agente = resultado.Valor!;
        await publicadorEventos.PublicarAlteracaoAsync(agente.Id, "criado", agente.AtualizadoEm, cancelamento);
        return CreatedAtAction(nameof(ConsultarAsync), new { id = agente.Id }, agente);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AgenteResposta>> AtualizarAsync(
        Guid id,
        AgenteRequisicao requisicao,
        CancellationToken cancelamento)
    {
        var resultado = await servico.AtualizarAsync(id, requisicao, cancelamento);
        if (resultado.NaoEncontrado)
        {
            return NotFound();
        }

        if (!resultado.SucessoOperacao)
        {
            return resultado.Erros.Count > 0
                ? ValidationProblem(CriarErros(resultado.Erros))
                : Problem(statusCode: 500);
        }

        var agente = resultado.Valor!;
        await publicadorEventos.PublicarAlteracaoAsync(agente.Id, "atualizado", agente.AtualizadoEm, cancelamento);
        return Ok(agente);
    }

    private static Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary CriarErros(
        IReadOnlyDictionary<string, string[]> erros)
    {
        var estado = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
        foreach (var (campo, mensagens) in erros)
        {
            foreach (var mensagem in mensagens)
            {
                estado.AddModelError(campo, mensagem);
            }
        }

        return estado;
    }
}
