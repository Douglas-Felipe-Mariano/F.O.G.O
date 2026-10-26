using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Fogo.Api;
using Fogo.Api.Application.Inventario;
using Fogo.Api.Identity;
using Fogo.Api.Inventario;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

    [Fact]
    public async Task ListarEquipamentos_SemAutenticacao_RecusaAcesso()
    {
        var resposta = await _cliente.GetAsync("/api/equipamentos");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task ListarAgentes_SemAutenticacao_RecusaAcesso()
    {
        var resposta = await _cliente.GetAsync("/api/agentes");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
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
    public async Task Entrar_SemTokenCsrf_RecusaAcesso()
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/autenticacao/entrar")
        {
            Content = JsonContent.Create(new LoginRequisicao("inexistente", "Senha-forte-123!"))
        };
        requisicao.Headers.Add("Origin", "http://localhost:5173");

        var resposta = await _cliente.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Entrar_ComCookieCsrfSemHeaderIgual_RecusaAcesso()
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/autenticacao/entrar")
        {
            Content = JsonContent.Create(new LoginRequisicao("inexistente", "Senha-forte-123!"))
        };
        requisicao.Headers.Add("Origin", "http://localhost:5173");
        requisicao.Headers.Add("Cookie", "fogo.csrf=valor-cookie");
        requisicao.Headers.Add("X-CSRF-TOKEN", "valor-diferente");

        var resposta = await _cliente.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task EmitirCsrf_CriaCookieLegivelComPathRaiz()
    {
        var resposta = await _cliente.GetAsync("/api/autenticacao/csrf");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        var cookies = resposta.Headers.GetValues("Set-Cookie");
        var cookieCsrf = Assert.Single(cookies);
        Assert.Contains("fogo.csrf=", cookieCsrf);
        Assert.Contains("path=/", cookieCsrf, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("httponly", cookieCsrf, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RotaProtegida_SemBearer_RecusaAcesso()
    {
        var resposta = await _cliente.GetAsync("/api/autenticacao/eu");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task RotaNaoMapeada_RecusaAcessoPorPadrao()
    {
        var resposta = await _cliente.GetAsync("/api/rota-nao-definida");

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

public sealed class InventarioTests : IClassFixture<ConfiguracaoFogoWebApplicationFactory>
{
    private readonly HttpClient _cliente;
    private readonly ConfiguracaoFogoWebApplicationFactory _fabrica;

    public InventarioTests(ConfiguracaoFogoWebApplicationFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public void ControllersMvc_ExpõemRotasDeInventario()
    {
        var provider = _fabrica.Services.GetRequiredService<IActionDescriptorCollectionProvider>();
        var rotas = provider.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .Select(acao => acao.AttributeRouteInfo?.Template)
            .ToArray();

        Assert.Contains("api/equipamentos", rotas);
        Assert.Contains("api/equipamentos/{id:guid}", rotas);
        Assert.Contains("api/equipamentos/localizacoes", rotas);
        Assert.Contains("api/indicadores/equipamentos", rotas);
    }

    [Fact]
    public void ControllersMvc_SerializamEnumsEmCamelCase()
    {
        var opcoes = _fabrica.Services.GetRequiredService<IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>();
        var serializado = JsonSerializer.Serialize(SituacaoEquipamento.Operacional, opcoes.Value.JsonSerializerOptions);

        Assert.Equal("\"operacional\"", serializado);
    }

    [Fact]
    public async Task Listar_SemBearer_RecusaAcesso()
    {
        var resposta = await _cliente.GetAsync("/api/equipamentos");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Listar_ComSituacaoInvalida_SemAutenticacaoRecusaAcesso()
    {
        var resposta = await _cliente.GetAsync("/api/equipamentos?situacao=999");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Listar_ComPaginaForaDoLimite_SemAutenticacaoRecusaAcesso()
    {
        var resposta = await _cliente.GetAsync("/api/equipamentos?pagina=21474836&tamanhoPagina=100");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Indicadores_SemBearer_RecusaAcesso()
    {
        var resposta = await _cliente.GetAsync("/api/indicadores/equipamentos");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Localizacoes_SemBearer_RecusaAcesso()
    {
        var resposta = await _cliente.GetAsync("/api/equipamentos/localizacoes");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task HubEquipamentos_SemBearer_RecusaNegociacao()
    {
        var resposta = await _cliente.PostAsync("/hubs/equipamentos/negotiate?negotiateVersion=1", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Criar_SemBearer_RecusaAcesso()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/equipamentos", CriarRequisicaoValida());

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_SemBearer_RecusaAcesso()
    {
        var resposta = await _cliente.PutAsJsonAsync($"/api/equipamentos/{Guid.NewGuid()}", CriarRequisicaoValida());

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public void ValidarEquipamento_RejeitaEnumsNumericosForaDoContrato()
    {
        var requisicao = CriarRequisicaoValida() with
        {
            Situacao = (SituacaoEquipamento)999,
            OrigemDados = (OrigemRegistro)999
        };

        var erros = ValidadorEquipamento.Validar(requisicao);

        Assert.Contains(nameof(EquipamentoRequisicao.Situacao), erros.Keys);
        Assert.Contains(nameof(EquipamentoRequisicao.OrigemDados), erros.Keys);
    }

    [Fact]
    public void ValidarEquipamento_RequerCoordenadasCompletasESituacaoExplicita()
    {
        var requisicao = CriarRequisicaoValida() with
        {
            Latitude = -23.5m,
            Longitude = null,
            Situacao = SituacaoEquipamento.NaoInformado
        };

        var erros = ValidadorEquipamento.Validar(requisicao);

        Assert.Contains(nameof(EquipamentoRequisicao.Latitude), erros.Keys);
        Assert.Contains(nameof(EquipamentoRequisicao.Situacao), erros.Keys);
    }

    [Fact]
    public void ValidarEquipamento_ExigeDataQuandoManutencaoForAgendada()
    {
        var requisicao = CriarRequisicaoValida() with
        {
            NecessitaManutencao = true,
            TipoManutencao = TipoManutencao.Agendada,
            ProximaManutencao = null
        };

        var erros = ValidadorEquipamento.Validar(requisicao);

        Assert.Contains(nameof(EquipamentoRequisicao.ProximaManutencao), erros.Keys);
    }

    [Fact]
    public void ValidarEquipamento_RejeitaManutencaoForaDoContrato()
    {
        var requisicao = CriarRequisicaoValida() with
        {
            NecessitaManutencao = true,
            TipoManutencao = (TipoManutencao)999,
            ProximaManutencao = null
        };

        var erros = ValidadorEquipamento.Validar(requisicao);

        Assert.Contains(nameof(EquipamentoRequisicao.TipoManutencao), erros.Keys);
        Assert.DoesNotContain(nameof(EquipamentoRequisicao.ProximaManutencao), erros.Keys);
    }

    private static EquipamentoRequisicao CriarRequisicaoValida() => new(
        "Radar",
        "RAD-001",
        "Fabricante",
        "Modelo A",
        "01000-000",
        "Centro",
        "São Paulo",
        "SP",
        "Rua Um",
        null,
        -23.5m,
        -46.6m,
        SituacaoEquipamento.Operacional,
        false,
        null,
        null,
        OrigemRegistro.Manual);
}

public sealed class ModeloBancoTests
{
    [Fact]
    public void Modelo_UsaTabelasEmPortuguesEIncluiEquipamentosEAgentes()
    {
        var opcoes = new DbContextOptionsBuilder<FogoDbContext>()
            .UseNpgsql("Host=localhost;Database=fogo_model_test;Username=test;Password=test")
            .Options;
        using var banco = new FogoDbContext(opcoes);

        var tabelas = banco.Model.GetEntityTypes()
            .Select(tipo => tipo.GetTableName())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("usuarios", tabelas);
        Assert.Contains("perfis_acesso", tabelas);
        Assert.Contains("sessoes_atualizacao", tabelas);
        Assert.Contains("equipamentos", tabelas);
        Assert.Contains("agentes", tabelas);
        Assert.DoesNotContain("AspNetUsers", tabelas);
    }
}