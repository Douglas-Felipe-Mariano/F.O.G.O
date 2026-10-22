using System.Net;
using System.Net.Http.Json;
using Fogo.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Fogo.Tests;

public sealed class SaudeApiTests : IClassFixture<ConfiguracaoFogoWebApplicationFactory>
{
    private readonly HttpClient _cliente;

    public SaudeApiTests(ConfiguracaoFogoWebApplicationFactory fabrica)
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

public sealed class AutenticacaoTests : IClassFixture<ConfiguracaoFogoWebApplicationFactory>
{
    private readonly HttpClient _cliente;

    public AutenticacaoTests(ConfiguracaoFogoWebApplicationFactory fabrica)
    {
        _cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Entrar_SemOrigemPermitida_RecusaSemCriarSessao()
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/autenticacao/entrar")
        {
            Content = JsonContent.Create(new LoginRequisicao("inexistente", "Senha-forte-123!"))
        };
        requisicao.Headers.Add("Origin", "https://origem-nao-autorizada.example");

        var resposta = await _cliente.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task RotaProtegida_SemBearer_RecusaAcesso()
    {
        var resposta = await _cliente.GetAsync("/api/autenticacao/eu");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_SemOrigemPermitida_RecusaAcesso()
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/autenticacao/atualizar");
        requisicao.Headers.Add("Origin", "https://origem-nao-autorizada.example");

        var resposta = await _cliente.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Sair_SemOrigemPermitida_RecusaAcesso()
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/autenticacao/sair");
        requisicao.Headers.Add("Origin", "https://origem-nao-autorizada.example");

        var resposta = await _cliente.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

}