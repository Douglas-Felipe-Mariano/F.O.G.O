using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Fogo.Tests;

public sealed class SaudeApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _cliente;

    public SaudeApiTests(WebApplicationFactory<Program> fabrica)
    {
        _cliente = fabrica.CreateClient();
    }

    [Fact]
    public async Task ConsultarSaude_RetornaEstadoDisponivel()
    {
        var resposta = await _cliente.GetAsync("/api/saude");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var conteudo = await resposta.Content.ReadFromJsonAsync<RespostaSaude>();
        Assert.NotNull(conteudo);
        Assert.Equal("disponivel", conteudo.Estado);
    }

    private sealed record RespostaSaude(string Estado);
}