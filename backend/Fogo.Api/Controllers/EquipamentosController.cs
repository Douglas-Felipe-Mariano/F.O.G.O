using Fogo.Api.Application.Inventario;
using Fogo.Api.Inventario;
using Fogo.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fogo.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/equipamentos")]
public sealed class EquipamentosController(EquipamentoService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginaEquipamentosResposta>> ListarAsync(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 25,
        [FromQuery] string? tipo = null,
        [FromQuery] SituacaoEquipamento? situacao = null,
        [FromQuery] string? bairro = null,
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

        var filtros = new FiltrosEquipamentos(pagina, tamanhoPagina, tipo, situacao, bairro);
        var resultado = await service.ListarAsync(filtros, cancelamento);
        return Ok(new PaginaEquipamentosResposta(pagina, tamanhoPagina, resultado.Total, resultado.Itens));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EquipamentoResposta>> ConsultarAsync(Guid id, CancellationToken cancelamento)
    {
        var equipamento = await service.ConsultarAsync(id, cancelamento);
        return equipamento is null ? NotFound() : Ok(equipamento);
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
        return CreatedAtAction(nameof(ConsultarAsync), new { id = equipamento.Id }, equipamento);
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

        return Ok(resultado.Valor);
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
