using Fogo.Api.Application.Operacoes;
using Fogo.Api.Operacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Fogo.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/operacoes")]
public sealed class OperacoesController(OperacoesService service) : ControllerBase
{
    [HttpGet("predicoes")]
    public async Task<ActionResult<PredicoesOperacionaisResposta>> ConsultarPredicoesAsync(CancellationToken cancelamento) =>
        Ok(await service.ConsultarPredicoesAsync(cancelamento));

    [HttpGet("transito")]
    public async Task<ActionResult<IReadOnlyCollection<TransitoSegmentoResposta>>> ConsultarTransitoAsync(CancellationToken cancelamento) =>
        Ok(await service.ConsultarTransitoAsync(cancelamento));

    [HttpGet("alertas")]
    public async Task<ActionResult<IReadOnlyCollection<AlertaOperacionalResposta>>> ConsultarAlertasAsync(CancellationToken cancelamento) =>
        Ok(await service.ConsultarAlertasAsync(cancelamento));

    [HttpGet("acidentes")]
    public async Task<ActionResult<IReadOnlyCollection<AcidenteResposta>>> ListarAcidentesAsync(CancellationToken cancelamento) =>
        Ok(await service.ListarAcidentesAsync(cancelamento));

    [HttpGet("acidentes/{id:guid}", Name = "ConsultarAcidente")]
    public async Task<ActionResult<AcidenteResposta>> ConsultarAcidenteAsync(Guid id, CancellationToken cancelamento)
    {
        var acidente = await service.ConsultarAcidenteAsync(id, cancelamento);
        return acidente is null ? NotFound() : Ok(acidente);
    }

    [HttpPost("acidentes")]
    public async Task<ActionResult<AcidenteResposta>> RegistrarAcidenteAsync(
        [FromBody] AcidenteRequisicao requisicao,
        CancellationToken cancelamento)
    {
        var erros = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(requisicao.LocalDescricao) || requisicao.LocalDescricao.Trim().Length > 240)
        {
            erros["localDescricao"] = ["Informe o local do acidente (até 240 caracteres)."];
        }
        if (string.IsNullOrWhiteSpace(requisicao.Cidade) || requisicao.Cidade.Trim().Length > 120)
        {
            erros["cidade"] = ["Informe a cidade (até 120 caracteres)."];
        }
        if (requisicao.Latitude is < -90 or > 90) erros["latitude"] = ["A latitude deve estar entre -90 e 90."];
        if (requisicao.Longitude is < -180 or > 180) erros["longitude"] = ["A longitude deve estar entre -180 e 180."];
        if (!Enum.IsDefined(requisicao.Grau)) erros["grau"] = ["Informe um grau de acidente válido."];
        if (!Enum.IsDefined(requisicao.ImpactoTransito)) erros["impactoTransito"] = ["Informe um impacto no trânsito válido."];
        if (erros.Count > 0) return ValidationProblem(CriarModelState(erros));

        var acidente = await service.RegistrarAcidenteAsync(requisicao, cancelamento);
        return CreatedAtRoute("ConsultarAcidente", new { id = acidente.Id }, acidente);
    }

    [HttpPost("acidentes/{id:guid}/despachar")]
    public async Task<ActionResult<AcidenteResposta>> SolicitarDespachoAsync(
        Guid id,
        [FromBody] DespachoAgenteRequisicao requisicao,
        CancellationToken cancelamento)
    {
        var resultado = await service.SolicitarDespachoAsync(id, requisicao.AgenteId, cancelamento);
        if (resultado.Acidente is null) return NotFound();
        if (resultado.AgenteNaoEncontrado)
        {
            return ValidationProblem(CriarModelState(new Dictionary<string, string[]>
            {
                ["agenteId"] = ["O agente informado não existe."]
            }));
        }

        return Ok(resultado.Acidente);
    }

    [HttpPost("acidentes/{id:guid}/acionar-socorro")]
    public async Task<ActionResult<AcidenteResposta>> SolicitarSocorroAsync(Guid id, CancellationToken cancelamento)
    {
        var acidente = await service.SolicitarSocorroAsync(id, cancelamento);
        return acidente is null ? NotFound() : Ok(acidente);
    }

    [HttpPost("acidentes/{id:guid}/encerrar")]
    public async Task<ActionResult<AcidenteResposta>> EncerrarAcidenteAsync(Guid id, CancellationToken cancelamento)
    {
        var acidente = await service.EncerrarAcidenteAsync(id, cancelamento);
        return acidente is null ? NotFound() : Ok(acidente);
    }

    private static ModelStateDictionary CriarModelState(IReadOnlyDictionary<string, string[]> erros)
    {
        var estado = new ModelStateDictionary();
        foreach (var (campo, mensagens) in erros)
        {
            foreach (var mensagem in mensagens) estado.AddModelError(campo, mensagem);
        }
        return estado;
    }
}
