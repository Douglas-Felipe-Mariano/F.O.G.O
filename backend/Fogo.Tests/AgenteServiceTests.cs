using Fogo.Api.Application.Inventario;
using Fogo.Api.Controllers;
using Fogo.Api.Inventario;
using Fogo.Api.Repositories;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Fogo.Tests;

public sealed class AgenteServiceTests
{
    [Fact]
    public async Task CriarAsync_RequisicaoValida_NormalizaCamposRegistraHorarioEPersiste()
    {
        var agora = new DateTimeOffset(2026, 10, 26, 14, 0, 0, TimeSpan.Zero);
        var repositorio = new RepositorioAgentesFalso();
        var servico = new AgenteService(repositorio, new RelogioFalso(agora));

        var resultado = await servico.CriarAsync(CriarRequisicao(), CancellationToken.None);

        Assert.True(resultado.SucessoOperacao);
        var agente = Assert.IsType<Agente>(repositorio.AgenteAdicionado);
        Assert.Equal("Ana Silva", agente.Nome);
        Assert.Equal("Equipe de campo", agente.CategoriaProfissional);
        Assert.Equal("Base Centro", agente.BaseOperacional);
        Assert.Equal(agora, agente.CriadoEm);
        Assert.Equal(agora, agente.AtualizadoEm);
        Assert.Equal(OrigemRegistro.Manual, agente.OrigemDados);
        Assert.Equal(agente.Id, resultado.Valor!.Id);
        var localizacao = Assert.Single(repositorio.LocalizacoesHistoricasAdicionadas);
        Assert.Equal(agente.Id, localizacao.AgenteId);
        Assert.Equal(agora, localizacao.RegistradaEm);
        Assert.Equal(agente.Latitude, localizacao.Latitude);
        Assert.Equal(agente.Longitude, localizacao.Longitude);
    }

    [Fact]
    public async Task CriarAsync_CoordenadaIncompleta_NaoPersiste()
    {
        var repositorio = new RepositorioAgentesFalso();
        var servico = new AgenteService(repositorio, TimeProvider.System);

        var resultado = await servico.CriarAsync(CriarRequisicao() with { Longitude = null }, CancellationToken.None);

        Assert.False(resultado.SucessoOperacao);
        Assert.Contains(nameof(AgenteRequisicao.Latitude), resultado.Erros.Keys);
        Assert.Null(repositorio.AgenteAdicionado);
        Assert.Empty(repositorio.LocalizacoesHistoricasAdicionadas);
    }

    [Fact]
    public async Task CriarAsync_CamposNulos_NaoLancaExcecaoENaoPersiste()
    {
        var repositorio = new RepositorioAgentesFalso();
        var servico = new AgenteService(repositorio, TimeProvider.System);
        var requisicao = new AgenteRequisicao(null!, null!, null!, null, null, SituacaoAgente.Disponivel, OrigemRegistro.Manual);

        var resultado = await servico.CriarAsync(requisicao, CancellationToken.None);

        Assert.False(resultado.SucessoOperacao);
        Assert.Contains(nameof(AgenteRequisicao.Nome), resultado.Erros.Keys);
        Assert.Null(repositorio.AgenteAdicionado);
    }

    [Fact]
    public async Task AtualizarAsync_AgenteExistente_PreservaCriacaoEAtualizaDados()
    {
        var agente = CriarAgente();
        var criadoEm = new DateTimeOffset(2026, 10, 20, 8, 0, 0, TimeSpan.Zero);
        var atualizadoEm = new DateTimeOffset(2026, 10, 26, 14, 0, 0, TimeSpan.Zero);
        agente.CriadoEm = criadoEm;
        var repositorio = new RepositorioAgentesFalso { AgenteParaAtualizacao = agente };
        var servico = new AgenteService(repositorio, new RelogioFalso(atualizadoEm));

        var resultado = await servico.AtualizarAsync(agente.Id, CriarRequisicao() with { Situacao = SituacaoAgente.EmAtendimento }, CancellationToken.None);

        Assert.True(resultado.SucessoOperacao);
        Assert.True(repositorio.SalvouAlteracoes);
        Assert.Equal(SituacaoAgente.EmAtendimento, agente.Situacao);
        Assert.Equal(criadoEm, agente.CriadoEm);
        Assert.Equal(atualizadoEm, agente.AtualizadoEm);
    }

    [Fact]
    public async Task AtualizarAsync_AlteraCoordenadas_AdicionaHistoricoAntesDoSaveUnico()
    {
        var agente = CriarAgente();
        var repositorio = new RepositorioAgentesFalso { AgenteParaAtualizacao = agente };
        var servico = new AgenteService(repositorio, new RelogioFalso(new DateTimeOffset(2026, 10, 26, 14, 0, 0, TimeSpan.Zero)));

        var resultado = await servico.AtualizarAsync(
            agente.Id,
            CriarRequisicao() with { Latitude = -21.9m, Longitude = -50.5m },
            CancellationToken.None);

        Assert.True(resultado.SucessoOperacao);
        var localizacao = Assert.Single(repositorio.LocalizacoesHistoricasAdicionadas);
        Assert.Equal(agente.Id, localizacao.AgenteId);
        Assert.Equal(-21.9m, localizacao.Latitude);
        Assert.Equal(-50.5m, localizacao.Longitude);
        Assert.True(repositorio.SalvouAlteracoes);
    }

    [Fact]
    public async Task InativarAsync_AgenteExistente_AtualizaSituacaoEPreservaDadosEHistorico()
    {
        var criadoEm = new DateTimeOffset(2026, 10, 20, 8, 0, 0, TimeSpan.Zero);
        var atualizadoEm = new DateTimeOffset(2026, 10, 27, 14, 0, 0, TimeSpan.Zero);
        var agente = CriarAgente();
        agente.CriadoEm = criadoEm;
        var repositorio = new RepositorioAgentesFalso { AgenteParaAtualizacao = agente };
        var servico = new AgenteService(repositorio, new RelogioFalso(atualizadoEm));

        var resposta = await servico.InativarAsync(agente.Id, CancellationToken.None);

        Assert.NotNull(resposta);
        Assert.Equal(SituacaoAgente.Inativo, agente.Situacao);
        Assert.Equal(atualizadoEm, agente.AtualizadoEm);
        Assert.Equal(criadoEm, agente.CriadoEm);
        Assert.Equal("Ana Silva", agente.Nome);
        Assert.Equal(agente.Id, resposta.Id);
        Assert.True(repositorio.SalvouAlteracoes);
        Assert.Empty(repositorio.LocalizacoesHistoricasAdicionadas);
    }

    [Fact]
    public async Task InativarAsync_AgenteInexistente_RetornaNuloESemSalvar()
    {
        var repositorio = new RepositorioAgentesFalso();
        var servico = new AgenteService(repositorio, TimeProvider.System);

        var resposta = await servico.InativarAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(resposta);
        Assert.False(repositorio.SalvouAlteracoes);
    }

    [Fact]
    public async Task InativarController_PublicaEventoDepoisDaPersistencia()
    {
        var agente = CriarAgente();
        var repositorio = new RepositorioAgentesFalso { AgenteParaAtualizacao = agente };
        var publicador = new PublicadorAgentesFalso(() => repositorio.SalvouAlteracoes);
        var controller = new AgentesController(
            new AgenteService(repositorio, TimeProvider.System),
            publicador);

        var resposta = await controller.InativarAsync(agente.Id, CancellationToken.None);

        var resultado = Assert.IsType<OkObjectResult>(resposta.Result);
        var agenteAtualizado = Assert.IsType<AgenteResposta>(resultado.Value);
        var evento = Assert.Single(publicador.Eventos);
        Assert.Equal(SituacaoAgente.Inativo, agenteAtualizado.Situacao);
        Assert.True(publicador.PublicadoDepoisDeSalvar);
        Assert.Equal(agente.Id, evento.AgenteId);
        Assert.Equal("atualizado", evento.Operacao);
    }

    [Fact]
    public async Task InativarController_AgenteInexistente_NaoPublicaEvento()
    {
        var repositorio = new RepositorioAgentesFalso();
        var publicador = new PublicadorAgentesFalso(() => repositorio.SalvouAlteracoes);
        var controller = new AgentesController(
            new AgenteService(repositorio, TimeProvider.System),
            publicador);

        var resposta = await controller.InativarAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(resposta.Result);
        Assert.Empty(publicador.Eventos);
        Assert.False(repositorio.SalvouAlteracoes);
    }

    [Fact]
    public async Task ListarAsync_RetornaPaginacaoEMapeamento()
    {
        var agente = CriarAgente();
        var filtros = new FiltrosAgentes(2, 10, "Equipe de campo", SituacaoAgente.Disponivel);
        var repositorio = new RepositorioAgentesFalso { Total = 13, Itens = [agente] };
        var servico = new AgenteService(repositorio, TimeProvider.System);

        var resposta = await servico.ListarAsync(filtros, CancellationToken.None);

        Assert.Equal(2, resposta.Pagina);
        Assert.Equal(13, resposta.TotalRegistros);
        Assert.Equal(agente.Id, Assert.Single(resposta.Itens).Id);
        Assert.Equal(filtros, repositorio.Filtros);
    }

    [Fact]
    public async Task ConsultarTrajetoDiarioAsync_RetornaSomenteHistoricoRegistradoDaDataSolicitada()
    {
        var agente = CriarAgente();
        var data = new DateOnly(2026, 10, 26);
        var pontoDoDia = new HistoricoLocalizacaoAgente
        {
            AgenteId = agente.Id,
            Latitude = -21.9m,
            Longitude = -50.5m,
            RegistradaEm = new DateTimeOffset(2026, 10, 26, 14, 0, 0, TimeSpan.Zero)
        };
        var repositorio = new RepositorioAgentesFalso
        {
            AgenteParaAtualizacao = agente,
            HistoricoParaConsulta =
            [
                pontoDoDia,
                new HistoricoLocalizacaoAgente
                {
                    AgenteId = agente.Id,
                    Latitude = -22m,
                    Longitude = -50m,
                    RegistradaEm = new DateTimeOffset(2026, 10, 25, 14, 0, 0, TimeSpan.Zero)
                }
            ]
        };

        var resposta = await new HistoricoAgentesService(repositorio)
            .ConsultarTrajetoDiarioAsync(agente.Id, data, CancellationToken.None);

        Assert.NotNull(resposta);
        Assert.True(resposta.HistoricoDisponivel);
        Assert.Equal(data, repositorio.DataHistoricoSolicitada);
        Assert.Equal(pontoDoDia.Id, Assert.Single(resposta.Localizacoes).Id);
    }

    [Fact]
    public async Task ConsultarUltimasLocalizacoesAsync_RetornaOrdemCronologicaEIdentificaDadosDemonstrativos()
    {
        var agente = CriarAgente();
        agente.OrigemDados = OrigemRegistro.Simulado;
        var primeiroRegistro = new HistoricoLocalizacaoAgente
        {
            AgenteId = agente.Id,
            Latitude = -21.9m,
            Longitude = -50.5m,
            RegistradaEm = new DateTimeOffset(2026, 10, 26, 13, 0, 0, TimeSpan.Zero)
        };
        var registroMaisRecente = new HistoricoLocalizacaoAgente
        {
            AgenteId = agente.Id,
            Latitude = -21.8m,
            Longitude = -50.4m,
            RegistradaEm = new DateTimeOffset(2026, 10, 26, 14, 0, 0, TimeSpan.Zero)
        };
        var repositorio = new RepositorioAgentesFalso
        {
            AgenteParaAtualizacao = agente,
            HistoricoParaConsulta = [registroMaisRecente, primeiroRegistro]
        };

        var resposta = await new HistoricoAgentesService(repositorio)
            .ConsultarUltimasLocalizacoesAsync(agente.Id, 10, CancellationToken.None);

        Assert.NotNull(resposta);
        Assert.True(resposta.HistoricoDisponivel);
        Assert.Equal(OrigemRegistro.Simulado, resposta.OrigemDados);
        Assert.Contains("demonstrativo", resposta.Mensagem, StringComparison.OrdinalIgnoreCase);
        Assert.Equal([primeiroRegistro.Id, registroMaisRecente.Id], resposta.Localizacoes.Select(item => item.Id));
        Assert.Equal(10, repositorio.LimiteHistoricoRecente);
    }

    [Fact]
    public async Task ConsultarHistoricoPaginadoAsync_FiltraPeriodoEPaginaTodosOsRegistros()
    {
        var agente = CriarAgente();
        var repositorio = new RepositorioAgentesFalso
        {
            AgenteParaAtualizacao = agente,
            HistoricoParaConsulta =
            [
                new HistoricoLocalizacaoAgente { AgenteId = agente.Id, Latitude = -21m, Longitude = -50m, RegistradaEm = new DateTimeOffset(2026, 10, 25, 23, 59, 0, TimeSpan.Zero) },
                new HistoricoLocalizacaoAgente { AgenteId = agente.Id, Latitude = -22m, Longitude = -51m, RegistradaEm = new DateTimeOffset(2026, 10, 26, 10, 0, 0, TimeSpan.Zero) },
                new HistoricoLocalizacaoAgente { AgenteId = agente.Id, Latitude = -23m, Longitude = -52m, RegistradaEm = new DateTimeOffset(2026, 10, 26, 11, 0, 0, TimeSpan.Zero) }
            ]
        };

        var resposta = await new HistoricoAgentesService(repositorio).ConsultarHistoricoPaginadoAsync(
            agente.Id,
            new DateOnly(2026, 10, 26),
            new DateOnly(2026, 10, 26),
            deslocamento: 1,
            limite: 1,
            CancellationToken.None);

        Assert.NotNull(resposta);
        Assert.Equal(2, resposta.TotalRegistros);
        Assert.False(resposta.ResultadoTruncado);
        Assert.Equal(-23m, Assert.Single(resposta.Localizacoes).Latitude);
    }

    [Fact]
    public async Task ConsultarIndicadoresAsync_SomaContagensInformadas()
    {
        IReadOnlyCollection<ContagemSituacaoAgente> contagens =
        [
            new(SituacaoAgente.Disponivel, 4),
            new(SituacaoAgente.EmAtendimento, 2),
            new(SituacaoAgente.Indisponivel, 3),
            new(SituacaoAgente.Inativo, 5)
        ];
        var repositorio = new RepositorioAgentesFalso { Contagens = contagens };
        var servico = new AgenteService(repositorio, TimeProvider.System);

        var indicadores = await servico.ConsultarIndicadoresAsync(CancellationToken.None);

        Assert.Equal(14, indicadores.TotalCadastrado);
        Assert.Equal(6, indicadores.EmAtividadeDeclarada);
        Assert.Equal(contagens, indicadores.PorSituacao);
    }

    [Fact]
    public async Task ListarLocalizacoesAsync_RespeitaLimiteEIndicaTruncamento()
    {
        var agenteSimulado = CriarAgente();
        agenteSimulado.OrigemDados = OrigemRegistro.Simulado;
        var agentes = new[] { CriarAgente(), agenteSimulado, CriarAgente() };
        var repositorio = new RepositorioAgentesFalso { TotalLocalizacoes = 4, Localizacoes = agentes };
        var servico = new AgenteService(repositorio, TimeProvider.System);

        var mapa = await servico.ListarLocalizacoesAsync(1, 2, CancellationToken.None);

        Assert.Equal(4, mapa.TotalComCoordenadas);
        Assert.Equal(2, mapa.Limite);
        Assert.Equal(1, repositorio.DeslocamentoLocalizacoes);
        Assert.True(mapa.ResultadoTruncado);
        Assert.Equal(2, mapa.Itens.Count);
        Assert.Equal(agentes[1].Nome, mapa.Itens.First().Nome);
        Assert.Equal(OrigemRegistro.Simulado, mapa.Itens.First().OrigemDados);
    }

    private static AgenteRequisicao CriarRequisicao() => new(
        " Ana Silva ", " Equipe de campo ", " Base Centro ", -23.5m, -46.6m,
        SituacaoAgente.Disponivel, OrigemRegistro.Manual);

    private static Agente CriarAgente() => new()
    {
        Nome = "Ana Silva",
        CategoriaProfissional = "Equipe de campo",
        BaseOperacional = "Base Centro",
        Latitude = -23.5m,
        Longitude = -46.6m,
        Situacao = SituacaoAgente.Disponivel,
        OrigemDados = OrigemRegistro.Manual
    };

    private sealed class RelogioFalso(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }

    private sealed class PublicadorAgentesFalso(Func<bool> foiSalvo) : IPublicadorEventosAgentes
    {
        public List<EventoAgenteAlterado> Eventos { get; } = [];
        public bool PublicadoDepoisDeSalvar { get; private set; }

        public Task PublicarAlteracaoAsync(Guid agenteId, string operacao, DateTimeOffset atualizadoEm, CancellationToken cancelamento)
        {
            PublicadoDepoisDeSalvar = foiSalvo();
            Eventos.Add(new EventoAgenteAlterado(agenteId, operacao, atualizadoEm));
            return Task.CompletedTask;
        }
    }

    private sealed class RepositorioAgentesFalso : IAgenteRepository
    {
        public int Total { get; init; }
        public IReadOnlyCollection<Agente> Itens { get; init; } = [];
        public IReadOnlyCollection<ContagemSituacaoAgente> Contagens { get; init; } = [];
        public int TotalLocalizacoes { get; init; }
        public int DeslocamentoLocalizacoes { get; private set; }
        public IReadOnlyCollection<Agente> Localizacoes { get; init; } = [];
        public FiltrosAgentes? Filtros { get; private set; }
        public Agente? AgenteAdicionado { get; private set; }
        public Agente? AgenteParaAtualizacao { get; init; }
        public bool SalvouAlteracoes { get; private set; }
        public List<HistoricoLocalizacaoAgente> LocalizacoesHistoricasAdicionadas { get; } = [];
        public IReadOnlyCollection<HistoricoLocalizacaoAgente> HistoricoParaConsulta { get; init; } = [];
        public DateOnly? DataHistoricoSolicitada { get; private set; }
        public int LimiteHistoricoRecente { get; private set; }

        public Task<int> ContarAsync(FiltrosAgentes filtros, CancellationToken cancelamento)
        {
            Filtros = filtros;
            return Task.FromResult(Total);
        }

        public Task<IReadOnlyCollection<Agente>> ListarAsync(FiltrosAgentes filtros, CancellationToken cancelamento)
        {
            Filtros = filtros;
            return Task.FromResult(Itens);
        }

        public Task<Agente?> ConsultarAsync(Guid id, CancellationToken cancelamento) => Task.FromResult(AgenteParaAtualizacao);
        public Task<Agente?> ConsultarParaAtualizacaoAsync(Guid id, CancellationToken cancelamento) => Task.FromResult(AgenteParaAtualizacao);
        public Task<int> ContarComCoordenadasAsync(CancellationToken cancelamento) => Task.FromResult(TotalLocalizacoes);
        public Task<IReadOnlyCollection<Agente>> ListarComCoordenadasAsync(int deslocamento, int limite, CancellationToken cancelamento)
        {
            DeslocamentoLocalizacoes = deslocamento;
            return Task.FromResult<IReadOnlyCollection<Agente>>(Localizacoes.Skip(deslocamento).Take(limite).ToArray());
        }
        public Task<IReadOnlyCollection<ContagemSituacaoAgente>> ContarPorSituacaoAsync(CancellationToken cancelamento) => Task.FromResult(Contagens);

        public Task AdicionarAsync(Agente agente, CancellationToken cancelamento)
        {
            AgenteAdicionado = agente;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<HistoricoLocalizacaoAgente>> ListarHistoricoAsync(Guid agenteId, DateOnly data, CancellationToken cancelamento)
        {
            DataHistoricoSolicitada = data;
            return Task.FromResult<IReadOnlyCollection<HistoricoLocalizacaoAgente>>(
                HistoricoParaConsulta.Where(localizacao => localizacao.AgenteId == agenteId
                    && DateOnly.FromDateTime(localizacao.RegistradaEm.UtcDateTime) == data).ToArray());
        }

        public Task<IReadOnlyCollection<HistoricoLocalizacaoAgente>> ListarHistoricoRecenteAsync(Guid agenteId, int limite, CancellationToken cancelamento)
        {
            LimiteHistoricoRecente = limite;
            return Task.FromResult<IReadOnlyCollection<HistoricoLocalizacaoAgente>>(
                HistoricoParaConsulta.Where(localizacao => localizacao.AgenteId == agenteId)
                    .OrderByDescending(localizacao => localizacao.RegistradaEm)
                    .Take(limite)
                    .ToArray());
        }

        public Task<int> ContarHistoricoAsync(Guid agenteId, DateTimeOffset? inicio, DateTimeOffset? fimExclusivo, CancellationToken cancelamento) =>
            Task.FromResult(FiltrarHistorico(agenteId, inicio, fimExclusivo).Count());

        public Task<IReadOnlyCollection<HistoricoLocalizacaoAgente>> ListarHistoricoPaginadoAsync(
            Guid agenteId,
            DateTimeOffset? inicio,
            DateTimeOffset? fimExclusivo,
            int deslocamento,
            int limite,
            CancellationToken cancelamento) =>
            Task.FromResult<IReadOnlyCollection<HistoricoLocalizacaoAgente>>(FiltrarHistorico(agenteId, inicio, fimExclusivo)
                .OrderBy(localizacao => localizacao.RegistradaEm)
                .Skip(deslocamento)
                .Take(limite)
                .ToArray());

        private IReadOnlyCollection<HistoricoLocalizacaoAgente> FiltrarHistorico(Guid agenteId, DateTimeOffset? inicio, DateTimeOffset? fimExclusivo) =>
            HistoricoParaConsulta.Where(localizacao => localizacao.AgenteId == agenteId
                && (!inicio.HasValue || localizacao.RegistradaEm >= inicio.Value)
                && (!fimExclusivo.HasValue || localizacao.RegistradaEm < fimExclusivo.Value)).ToArray();

        public Task AdicionarLocalizacaoHistoricaAsync(HistoricoLocalizacaoAgente localizacao, CancellationToken cancelamento)
        {
            LocalizacoesHistoricasAdicionadas.Add(localizacao);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken cancelamento)
        {
            SalvouAlteracoes = true;
            return Task.CompletedTask;
        }
    }
}
