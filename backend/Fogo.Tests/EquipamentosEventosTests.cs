using Fogo.Api;
using Fogo.Api.Application.Inventario;
using Fogo.Api.Controllers;
using Fogo.Api.Inventario;
using Fogo.Api.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Fogo.Tests;

public sealed class EquipamentosEventosTests : IClassFixture<ConfiguracaoFogoWebApplicationFactory>
{
    private readonly ConfiguracaoFogoWebApplicationFactory _fabrica;

    public EquipamentosEventosTests(ConfiguracaoFogoWebApplicationFactory fabrica)
    {
        _fabrica = fabrica;
    }

    [Fact]
    public async Task Criar_PublicaEventoDepoisDaPersistencia()
    {
        var repositorio = new RepositorioEquipamentoFalso();
        var publicador = new PublicadorEquipamentosFalso(() => repositorio.Salvo);
        var controller = CriarController(repositorio, publicador);

        var resposta = await controller.CriarAsync(CriarRequisicaoValida(), CancellationToken.None);

        var resultado = Assert.IsType<CreatedAtActionResult>(resposta.Result);
        var equipamento = Assert.IsType<EquipamentoResposta>(resultado.Value);
        var evento = Assert.Single(publicador.Eventos);
        Assert.True(repositorio.Salvo);
        Assert.True(publicador.RecebeuDepoisDaPersistencia);
        Assert.Equal(equipamento.Id, evento.EquipamentoId);
        Assert.Equal("criado", evento.Operacao);
        Assert.Equal(equipamento.AtualizadoEm, evento.AtualizadoEm);
    }

    [Fact]
    public async Task Criar_ComDadosInvalidos_NaoPublicaEvento()
    {
        var repositorio = new RepositorioEquipamentoFalso();
        var publicador = new PublicadorEquipamentosFalso(() => repositorio.Salvo);
        var controller = CriarController(repositorio, publicador);

        var resposta = await controller.CriarAsync(CriarRequisicaoValida() with { Tipo = " " }, CancellationToken.None);

        Assert.IsAssignableFrom<ObjectResult>(resposta.Result);
        Assert.Empty(publicador.Eventos);
        Assert.False(repositorio.Salvo);
    }

    [Fact]
    public async Task Atualizar_PublicaEventoDepoisDeSalvar()
    {
        var equipamentoExistente = CriarEntidade(Guid.NewGuid());
        var repositorio = new RepositorioEquipamentoFalso(equipamentoExistente);
        var publicador = new PublicadorEquipamentosFalso(() => repositorio.Salvo);
        var controller = CriarController(repositorio, publicador);

        var resposta = await controller.AtualizarAsync(
            equipamentoExistente.Id,
            CriarRequisicaoValida() with { Situacao = SituacaoEquipamento.EmManutencao },
            CancellationToken.None);

        var resultado = Assert.IsType<OkObjectResult>(resposta.Result);
        var equipamento = Assert.IsType<EquipamentoResposta>(resultado.Value);
        var evento = Assert.Single(publicador.Eventos);
        Assert.True(publicador.RecebeuDepoisDaPersistencia);
        Assert.Equal(equipamentoExistente.Id, evento.EquipamentoId);
        Assert.Equal("atualizado", evento.Operacao);
        Assert.Equal(equipamento.AtualizadoEm, evento.AtualizadoEm);
    }

    [Fact]
    public async Task Atualizar_EquipamentoInexistente_NaoPublicaEvento()
    {
        var repositorio = new RepositorioEquipamentoFalso();
        var publicador = new PublicadorEquipamentosFalso(() => repositorio.Salvo);
        var controller = CriarController(repositorio, publicador);

        var resposta = await controller.AtualizarAsync(
            Guid.NewGuid(),
            CriarRequisicaoValida(),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(resposta.Result);
        Assert.Empty(publicador.Eventos);
        Assert.False(repositorio.Salvo);
    }

    private EquipamentosController CriarController(
        RepositorioEquipamentoFalso repositorio,
        PublicadorEquipamentosFalso publicador)
    {
        var controller = new EquipamentosController(
            new EquipamentoService(repositorio, TimeProvider.System),
            publicador)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = _fabrica.Services
                }
            }
        };

        return controller;
    }

    private static EquipamentoRequisicao CriarRequisicaoValida() => new(
        "Radar",
        "RAD-TESTE-001",
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

    private static Equipamento CriarEntidade(Guid id) => new()
    {
        Id = id,
        Tipo = "Radar",
        NumeroSerie = "RAD-TESTE-002",
        Cep = "01000-000",
        Bairro = "Centro",
        Cidade = "São Paulo",
        Uf = "SP",
        Rua = "Rua Um",
        Latitude = -23.5m,
        Longitude = -46.6m,
        Situacao = SituacaoEquipamento.Operacional,
        OrigemDados = OrigemRegistro.Manual
    };

    private sealed class PublicadorEquipamentosFalso(Func<bool> persistido) : IPublicadorEventosEquipamentos
    {
        public List<EventoEquipamentoAlterado> Eventos { get; } = [];
        public bool RecebeuDepoisDaPersistencia { get; private set; }

        public Task PublicarAlteracaoAsync(
            Guid equipamentoId,
            string operacao,
            DateTimeOffset atualizadoEm,
            CancellationToken cancelamento)
        {
            RecebeuDepoisDaPersistencia = persistido();
            Eventos.Add(new EventoEquipamentoAlterado(equipamentoId, operacao, atualizadoEm));
            return Task.CompletedTask;
        }
    }

    private sealed class RepositorioEquipamentoFalso(Equipamento? existente = null) : IEquipamentoRepository
    {
        private readonly Equipamento? _existente = existente;
        public bool Salvo { get; private set; }

        public Task AdicionarAsync(Equipamento equipamento, CancellationToken cancelamento)
        {
            Salvo = true;
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken cancelamento)
        {
            Salvo = true;
            return Task.CompletedTask;
        }

        public Task<Equipamento?> ConsultarParaAtualizacaoAsync(Guid id, CancellationToken cancelamento) =>
            Task.FromResult(_existente?.Id == id ? _existente : null);

        public Task<int> ContarAsync(FiltrosEquipamentos filtros, CancellationToken cancelamento) =>
            Task.FromResult(0);

        public Task<IReadOnlyCollection<Equipamento>> ListarAsync(FiltrosEquipamentos filtros, CancellationToken cancelamento) =>
            Task.FromResult<IReadOnlyCollection<Equipamento>>([]);

        public Task<Equipamento?> ConsultarAsync(Guid id, CancellationToken cancelamento) =>
            Task.FromResult<Equipamento?>(null);

        public Task<int> ContarComCoordenadasAsync(FiltrosMapaEquipamentos filtros, CancellationToken cancelamento) =>
            Task.FromResult(0);

        public Task<IReadOnlyCollection<Equipamento>> ListarComCoordenadasAsync(
            FiltrosMapaEquipamentos filtros,
            int limite,
            CancellationToken cancelamento) =>
            Task.FromResult<IReadOnlyCollection<Equipamento>>([]);

        public Task<IReadOnlyCollection<ContagemSituacao>> ContarPorSituacaoAsync(CancellationToken cancelamento) =>
            Task.FromResult<IReadOnlyCollection<ContagemSituacao>>([]);

        public Task<int> ContarManutencoesNecessariasAsync(CancellationToken cancelamento) =>
            Task.FromResult(0);
    }
}

public sealed class PublicadorEventosEquipamentosTests
{
    [Fact]
    public async Task PublicarAlteracao_EnviaNomeEContratoDoEventoAoHub()
    {
        var proxy = new ClienteFalso();
        var publicador = CriarPublicador(proxy);
        var id = Guid.NewGuid();
        var atualizadoEm = DateTimeOffset.UtcNow;

        await publicador.PublicarAlteracaoAsync(id, "criado", atualizadoEm, CancellationToken.None);

        Assert.Equal("equipamentoAlterado", proxy.NomeEvento);
        var evento = Assert.IsType<EventoEquipamentoAlterado>(Assert.Single(proxy.Argumentos!));
        Assert.Equal(id, evento.EquipamentoId);
        Assert.Equal("criado", evento.Operacao);
        Assert.Equal(atualizadoEm, evento.AtualizadoEm);
    }

    [Fact]
    public async Task PublicarAlteracao_ClienteFalha_NaoPropagaErroAoFluxoDeGravacao()
    {
        var proxy = new ClienteFalso { Falhar = true };
        var publicador = CriarPublicador(proxy);

        var excecao = await Record.ExceptionAsync(() => publicador.PublicarAlteracaoAsync(
            Guid.NewGuid(),
            "atualizado",
            DateTimeOffset.UtcNow,
            CancellationToken.None));

        Assert.Null(excecao);
    }

    private static PublicadorEventosEquipamentos CriarPublicador(ClienteFalso proxy) =>
        new(new ContextoHubFalso(new ClientesHubFalsos(proxy)), NullLogger<PublicadorEventosEquipamentos>.Instance);

    private sealed class ContextoHubFalso(IHubClients clientes) : IHubContext<EquipamentosHub>
    {
        public IHubClients Clients { get; } = clientes;
        public IGroupManager Groups => throw new NotSupportedException();
    }

    private sealed class ClientesHubFalsos(IClientProxy proxy) : IHubClients
    {
        public IClientProxy All => proxy;
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => proxy;
        public IClientProxy Client(string connectionId) => proxy;
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => proxy;
        public IClientProxy Group(string groupName) => proxy;
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => proxy;
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => proxy;
        public IClientProxy User(string userId) => proxy;
        public IClientProxy Users(IReadOnlyList<string> userIds) => proxy;
    }

    private sealed class ClienteFalso : IClientProxy
    {
        public string? NomeEvento { get; private set; }
        public object?[]? Argumentos { get; private set; }
        public bool Falhar { get; init; }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            NomeEvento = method;
            Argumentos = args;
            return Falhar ? Task.FromException(new InvalidOperationException("Conexão encerrada.")) : Task.CompletedTask;
        }
    }
}
