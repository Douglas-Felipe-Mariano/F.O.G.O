using Fogo.Api.Application.Operacoes;
using Fogo.Api.Controllers;
using Fogo.Api.Operacoes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Fogo.Tests;

public sealed class OperacoesControllerTests : IClassFixture<ConfiguracaoFogoWebApplicationFactory>
{
    private readonly ConfiguracaoFogoWebApplicationFactory _fabrica;

    public OperacoesControllerTests(ConfiguracaoFogoWebApplicationFactory fabrica) => _fabrica = fabrica;

    [Fact]
    public async Task RegistrarAcidente_CoordenadaEEnumsInvalidos_RetornaErrosDeValidacao()
    {
        var controlador = CriarControlador();
        var requisicao = new AcidenteRequisicao(
            "Cruzamento central",
            "Tupã",
            91,
            -50,
            (GrauAcidente)99,
            (ImpactoTransito)99,
            false);

        var resposta = await controlador.RegistrarAcidenteAsync(requisicao, CancellationToken.None);

        var erro = Assert.IsAssignableFrom<BadRequestObjectResult>(resposta.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, erro.StatusCode);
        var detalhes = Assert.IsType<ValidationProblemDetails>(erro.Value);
        Assert.Contains("latitude", detalhes.Errors.Keys);
        Assert.Contains("grau", detalhes.Errors.Keys);
        Assert.Contains("impactoTransito", detalhes.Errors.Keys);
    }

    [Fact]
    public async Task RegistrarAcidente_LocalObrigatorio_RetornaErroDeValidacao()
    {
        var controlador = CriarControlador();
        var requisicao = new AcidenteRequisicao(
            " ",
            "Tupã",
            -21.93m,
            -50.51m,
            GrauAcidente.Leve,
            ImpactoTransito.Nenhum,
            false);

        var resposta = await controlador.RegistrarAcidenteAsync(requisicao, CancellationToken.None);

        var erro = Assert.IsAssignableFrom<BadRequestObjectResult>(resposta.Result);
        var detalhes = Assert.IsType<ValidationProblemDetails>(erro.Value);
        Assert.Contains("localDescricao", detalhes.Errors.Keys);
    }

    [Fact]
    public async Task EndpointsDeOperacoes_ExigemAutenticacao()
    {
        using var cliente = _fabrica.CreateClient();

        var respostaTransito = await cliente.GetAsync("/api/operacoes/transito");
        var respostaAcidentes = await cliente.GetAsync("/api/operacoes/acidentes");

        Assert.Equal(StatusCodes.Status401Unauthorized, (int)respostaTransito.StatusCode);
        Assert.Equal(StatusCodes.Status401Unauthorized, (int)respostaAcidentes.StatusCode);
    }

    private OperacoesController CriarControlador() => new(new OperacoesService(null!, TimeProvider.System))
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = _fabrica.Services }
        }
    };
}
