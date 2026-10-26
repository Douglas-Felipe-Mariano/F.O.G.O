using Fogo.Api;
using Fogo.Api.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fogo.Tests;

public sealed class ConfiguracaoAutenticacaoTests
{
    [Fact]
    public void AdicionarAutenticacaoFogo_DevelopmentSemOrigens_UsaOrigemLocalPadrao()
    {
        var builder = CriarBuilder("Development", null);

        builder.AdicionarAutenticacaoFogo();

        Assert.Equal(["http://localhost:5173"], ObterOrigens(builder));
    }

    [Theory]
    [InlineData("Production", null)]
    [InlineData("Production", " ")]
    [InlineData("Staging", null)]
    public void AdicionarAutenticacaoFogo_ForaDeDevelopmentSemOrigens_LancaExcecao(
        string ambiente,
        string? origens)
    {
        var builder = CriarBuilder(ambiente, origens);

        var excecao = Assert.Throws<InvalidOperationException>(() => builder.AdicionarAutenticacaoFogo());

        Assert.Contains("FOGO_ORIGINS", excecao.Message);
    }

    [Theory]
    [InlineData("ftp://portal.example")]
    [InlineData("https://portal.example/caminho")]
    [InlineData("https://usuario@portal.example")]
    [InlineData("https://portal.example?modo=teste")]
    [InlineData("https://portal.example;;https://outro.example")]
    public void AdicionarAutenticacaoFogo_ComOrigemInvalida_LancaExcecao(string origens)
    {
        var builder = CriarBuilder("Production", origens);

        var excecao = Assert.Throws<InvalidOperationException>(() => builder.AdicionarAutenticacaoFogo());

        Assert.Contains("FOGO_ORIGINS", excecao.Message);
    }

    [Fact]
    public void AdicionarAutenticacaoFogo_ComOrigensExplicitas_NormalizaERegistraLista()
    {
        var builder = CriarBuilder("Production", " https://portal.example/ ; http://localhost:8080 ");

        builder.AdicionarAutenticacaoFogo();

        Assert.Equal(["https://portal.example", "http://localhost:8080"], ObterOrigens(builder));
    }

    [Fact]
    public void AdicionarAutenticacaoFogo_ConfiguracaoLocalPermiteLocalhostE127001()
    {
        var builder = CriarBuilder("Production", "http://localhost:8080;http://127.0.0.1:8080");

        builder.AdicionarAutenticacaoFogo();

        Assert.Equal(["http://localhost:8080", "http://127.0.0.1:8080"], ObterOrigens(builder));
    }

    [Theory]
    [InlineData("http://localhost:8080", true)]
    [InlineData("http://127.0.0.1:8080", true)]
    [InlineData("http://127.0.0.1:8081", false)]
    public void OrigemPermitida_ValidaOrigemDoNavegadorContraAllowList(string origem, bool esperado)
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Headers.Origin = origem;
        IReadOnlyCollection<string> permitidas = ["http://localhost:8080", "http://127.0.0.1:8080"];

        Assert.Equal(esperado, ProtecaoRequisicao.OrigemPermitida(contexto, permitidas));
    }

    private static WebApplicationBuilder CriarBuilder(string ambiente, string? origens)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = ambiente });
        var configuracao = new Dictionary<string, string?>
        {
            ["FOGO_JWT_KEY"] = "chave-jwt-falsa-somente-para-testes-com-32-bytes",
            ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=teste;Username=teste;Password=nao-usar",
            ["FOGO_ORIGINS"] = origens
        };
        builder.Configuration.AddInMemoryCollection(configuracao);
        return builder;
    }

    private static IReadOnlyCollection<string> ObterOrigens(WebApplicationBuilder builder)
    {
        var registro = Assert.Single(builder.Services, descriptor => descriptor.ServiceType == typeof(IReadOnlyCollection<string>));
        return Assert.IsAssignableFrom<IReadOnlyCollection<string>>(registro.ImplementationInstance);
    }
}
