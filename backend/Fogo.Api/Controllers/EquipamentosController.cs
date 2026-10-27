using Fogo.Api.Application.Inventario;
using Fogo.Api.Inventario;
using Fogo.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fogo.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/equipamentos")]
public sealed class EquipamentosController(
    EquipamentoService service,
    IPublicadorEventosEquipamentos publicadorEventos) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginaEquipamentosResposta>> ListarAsync(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 25,
        [FromQuery] string? tipo = null,
        [FromQuery] SituacaoEquipamento? situacao = null,
        [FromQuery] string? bairro = null,
        [FromQuery] bool? necessitaManutencao = null,
        CancellationToken cancelamento = default)
    {
        if (pagina < 1 || tamanhoPagina is < 1 or > 100 || (long)(pagina - 1) * tamanhoPagina > int.MaxValue)
        {
            return ValidationProblem(ModelStateFrom(new Dictionary<string, string[]>
            {
                ["paginacao"] = ["A página deve ser maior que zero e o tamanho deve estar entre 1 e 100."]
            }));
        }

        if (situacao is { } valorSituacao
            && (!Enum.IsDefined(valorSituacao) || valorSituacao == SituacaoEquipamento.NaoInformado))
        {
            return ValidationProblem(ModelStateFrom(new Dictionary<string, string[]>
            {
                ["situacao"] = ["Informe uma situação válida para filtrar."]
            }));
        }

        var filtros = new FiltrosEquipamentos(pagina, tamanhoPagina, tipo, situacao, bairro, necessitaManutencao);
        var resultado = await service.ListarAsync(filtros, cancelamento);
        return Ok(new PaginaEquipamentosResposta(pagina, tamanhoPagina, resultado.Total, resultado.Itens));
    }

    [HttpGet("tipos")]
    public async Task<ActionResult<IReadOnlyCollection<string>>> ListarTiposAsync(CancellationToken cancelamento) =>
        Ok(await service.ListarTiposAsync(cancelamento));

    [HttpGet("{id:guid}", Name = "ConsultarEquipamento")]
    public async Task<ActionResult<EquipamentoResposta>> ConsultarAsync(Guid id, CancellationToken cancelamento)
    {
        var equipamento = await service.ConsultarAsync(id, cancelamento);
        return equipamento is null ? NotFound() : Ok(equipamento);
    }

    [HttpGet("localizacoes")]
    public async Task<ActionResult<MapaEquipamentosResposta>> ListarLocalizacoesAsync(
        [FromQuery] int limite = 500,
        [FromQuery] int deslocamento = 0,
        [FromQuery] string? tipo = null,
        [FromQuery] SituacaoEquipamento? situacao = null,
        [FromQuery] string? bairro = null,
        CancellationToken cancelamento = default)
    {
        if (limite is < 1 or > 1000 || deslocamento < 0)
        {
            return ValidationProblem(ModelStateFrom(new Dictionary<string, string[]>
            {
            ["paginacao"] = ["O limite deve estar entre 1 e 1000 e o deslocamento não pode ser negativo."]
            }));
        }

        if (situacao is { } valorSituacao
            && (!Enum.IsDefined(valorSituacao) || valorSituacao == SituacaoEquipamento.NaoInformado))
        {
            return ValidationProblem(ModelStateFrom(new Dictionary<string, string[]>
            {
                ["situacao"] = ["Informe uma situação válida para filtrar."]
            }));
        }

        var filtros = new FiltrosMapaEquipamentos(tipo, situacao, bairro);
        return Ok(await service.ListarLocalizacoesAsync(filtros, deslocamento, limite, cancelamento));
    }

    [HttpPost]
    public async Task<ActionResult<EquipamentoResposta>> CriarAsync(
        [FromBody] EquipamentoRequisicao requisicao,
        CancellationToken cancelamento)
    {
        var resultado = await service.CriarAsync(requisicao, cancelamento);
        if (!resultado.SucessoOperacao)
        {
            return resultado.Erros.Count > 0
                ? ValidationProblem(ModelStateFrom(resultado.Erros))
                : Problem(statusCode: 500);
        }

        var equipamento = resultado.Valor!;
        await publicadorEventos.PublicarAlteracaoAsync(
            equipamento.Id,
            "criado",
            equipamento.AtualizadoEm,
            cancelamento);
        return CreatedAtRoute("ConsultarEquipamento", new { id = equipamento.Id }, equipamento);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EquipamentoResposta>> AtualizarAsync(
        Guid id,
        [FromBody] EquipamentoRequisicao requisicao,
        CancellationToken cancelamento)
    {
        var resultado = await service.AtualizarAsync(id, requisicao, cancelamento);
        if (resultado.NaoEncontrado)
        {
            return NotFound();
        }

        if (!resultado.SucessoOperacao)
        {
            return resultado.Erros.Count > 0
                ? ValidationProblem(ModelStateFrom(resultado.Erros))
                : Problem(statusCode: 500);
        }

        var equipamentoAtualizado = resultado.Valor!;
        await publicadorEventos.PublicarAlteracaoAsync(
            equipamentoAtualizado.Id,
            "atualizado",
            equipamentoAtualizado.AtualizadoEm,
            cancelamento);

        return Ok(equipamentoAtualizado);
    }

    [HttpPost("{id:guid}/desativar")]
    public async Task<ActionResult<EquipamentoResposta>> DesativarAsync(Guid id, CancellationToken cancelamento)
    {
        var equipamento = await service.DesativarAsync(id, cancelamento);
        if (equipamento is null)
        {
            return NotFound();
        }

        await publicadorEventos.PublicarAlteracaoAsync(
            equipamento.Id,
            "atualizado",
            equipamento.AtualizadoEm,
            cancelamento);
        return Ok(equipamento);
    }

    private static Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary ModelStateFrom(
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

[ApiController]
[Authorize]
[Route("api/indicadores/equipamentos")]
public sealed class IndicadoresEquipamentosController(EquipamentoService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IndicadoresEquipamentosResposta>> ConsultarAsync(CancellationToken cancelamento) =>
        Ok(await service.ConsultarIndicadoresAsync(cancelamento));
}
