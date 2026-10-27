using Fogo.Api.Application.Inventario;
using Fogo.Api.Inventario;
using Fogo.Api.Repositories;
using Xunit;

namespace Fogo.Tests;

public sealed class EquipamentoServiceTests
{
    [Fact]
    public async Task CriarAsync_RequisicaoValida_NormalizaCamposEPersisteComHorarioAtual()
    {
        var agora = new DateTimeOffset(2026, 10, 24, 12, 30, 0, TimeSpan.Zero);
        var repositorio = new RepositorioMapaFalso();
        var servico = new EquipamentoService(repositorio, new RelogioFalso(agora));

        var resultado = await servico.CriarAsync(CriarRequisicao(), CancellationToken.None);

        Assert.True(resultado.SucessoOperacao);
        var equipamento = Assert.IsType<Equipamento>(repositorio.EquipamentoAdicionado);
        Assert.Equal("Radar", equipamento.Tipo);
        Assert.Equal("SER-001", equipamento.NumeroSerie);
        Assert.Equal("SP", equipamento.Uf);
        Assert.Equal("Marca", equipamento.Marca);
        Assert.Equal("Referência", equipamento.Referencia);
        var caracteristica = new Dictionary<string, string> { ["Visão noturna"] = "Sim" };
        var resultadoComCaracteristicas = await servico.CriarAsync(
            CriarRequisicao() with { Caracteristicas = caracteristica },
            CancellationToken.None);
        Assert.Equal("Sim", resultadoComCaracteristicas.Valor!.Caracteristicas["Visão noturna"]);
        Assert.Equal(OrigemRegistro.Manual, equipamento.OrigemDados);
        Assert.Equal(agora, equipamento.CriadoEm);
        Assert.Equal(agora, equipamento.AtualizadoEm);
        Assert.Equal(equipamento.Id, resultado.Valor!.Id);
    }

    [Fact]
    public async Task CriarAsync_RequisicaoInvalida_NaoPersiste()
    {
        var repositorio = new RepositorioMapaFalso();
        var servico = new EquipamentoService(repositorio, TimeProvider.System);

        var resultado = await servico.CriarAsync(CriarRequisicao() with { Tipo = " " }, CancellationToken.None);

        Assert.False(resultado.SucessoOperacao);
        Assert.Contains(nameof(EquipamentoRequisicao.Tipo), resultado.Erros.Keys);
        Assert.Null(repositorio.EquipamentoAdicionado);
    }

    [Fact]
    public async Task AtualizarAsync_RequisicaoValida_AplicaCamposEAtualizaHorarioSemAlterarCriacao()
    {
        var criadoEm = new DateTimeOffset(2026, 10, 20, 8, 0, 0, TimeSpan.Zero);
        var atualizadoEm = new DateTimeOffset(2026, 10, 24, 12, 30, 0, TimeSpan.Zero);
        var existente = CriarEquipamentoParaAtualizacao();
        existente.CriadoEm = criadoEm;
        existente.Marca = "Marca antiga";
        var repositorio = new RepositorioMapaFalso { EquipamentoParaAtualizacao = existente };
        var servico = new EquipamentoService(repositorio, new RelogioFalso(atualizadoEm));

        var resultado = await servico.AtualizarAsync(existente.Id, CriarRequisicao(), CancellationToken.None);

        Assert.True(resultado.SucessoOperacao);
        Assert.True(repositorio.SalvouAlteracoes);
        Assert.Equal("SER-001", existente.NumeroSerie);
        Assert.Equal("SP", existente.Uf);
        Assert.Null(existente.Modelo);
        Assert.Equal(criadoEm, existente.CriadoEm);
        Assert.Equal(atualizadoEm, existente.AtualizadoEm);
        Assert.Equal(existente.Id, resultado.Valor!.Id);
    }

    [Fact]
    public async Task AtualizarAsync_EquipamentoInexistente_NaoSalvaAlteracoes()
    {
        var repositorio = new RepositorioMapaFalso();
        var servico = new EquipamentoService(repositorio, TimeProvider.System);

        var resultado = await servico.AtualizarAsync(Guid.NewGuid(), CriarRequisicao(), CancellationToken.None);

        Assert.True(resultado.NaoEncontrado);
        Assert.False(repositorio.SalvouAlteracoes);
    }

    [Fact]
    public async Task AtualizarAsync_RequisicaoInvalida_NaoConsultaNemSalva()
    {
        var repositorio = new RepositorioMapaFalso();
        var servico = new EquipamentoService(repositorio, TimeProvider.System);

        var resultado = await servico.AtualizarAsync(Guid.NewGuid(), CriarRequisicao() with { Tipo = " " }, CancellationToken.None);

        Assert.False(resultado.SucessoOperacao);
        Assert.Equal(0, repositorio.ConsultasParaAtualizacao);
        Assert.False(repositorio.SalvouAlteracoes);
    }

    [Fact]
    public async Task ListarLocalizacoes_AplicaFiltrosELimiteEIndicaTruncamento()
    {
        var filtros = new FiltrosMapaEquipamentos("Radar", SituacaoEquipamento.Operacional, "Centro");
        var repositorio = new RepositorioMapaFalso();
        var servico = new EquipamentoService(repositorio, TimeProvider.System);

        var resposta = await servico.ListarLocalizacoesAsync(filtros, 1, 2, CancellationToken.None);

        Assert.Equal(filtros, repositorio.FiltrosContagem);
        Assert.Equal(filtros, repositorio.FiltrosListagem);
        Assert.Equal(1, repositorio.Deslocamento);
        Assert.Equal(2, repositorio.Limite);
        Assert.Equal(4, resposta.TotalComCoordenadas);
        Assert.Equal(2, resposta.Limite);
        Assert.True(resposta.ResultadoTruncado);
        Assert.Equal(2, resposta.Itens.Count);
        Assert.All(resposta.Itens, item => Assert.NotEqual(Guid.Empty, item.Id));
        var primeiro = resposta.Itens.First();
        Assert.Equal("01000-000", primeiro.Cep);
        Assert.Equal("Centro", primeiro.Bairro);
        Assert.Equal("Radar", primeiro.Tipo);
        Assert.Equal("RAD-002", primeiro.NumeroSerie);
        Assert.Equal("Fabricante", primeiro.Marca);
        Assert.Equal("ZX", primeiro.Modelo);
        Assert.Equal("50", primeiro.Caracteristicas["Limite"]);
        Assert.True(primeiro.NecessitaManutencao);
        Assert.Equal(TipoManutencao.Agendada, primeiro.TipoManutencao);
        Assert.Equal(new DateOnly(2026, 11, 1), primeiro.ProximaManutencao);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Listar_AplicaFiltroDeNecessidadeDeManutencao(bool necessitaManutencao)
    {
        var filtros = new FiltrosEquipamentos(1, 25, null, null, null, necessitaManutencao);
        var repositorio = new RepositorioMapaFalso();
        var servico = new EquipamentoService(repositorio, TimeProvider.System);

        await servico.ListarAsync(filtros, CancellationToken.None);

        Assert.Equal(filtros, repositorio.FiltrosListagemInventario);
        Assert.Equal(filtros, repositorio.FiltrosContagemInventario);
    }

    [Fact]
    public async Task Listar_RetornaTotalEItensMapeadosDoRepositorio()
    {
        var filtros = new FiltrosEquipamentos(2, 10, "Radar", SituacaoEquipamento.Operacional, "Centro");
        var equipamento = CriarEquipamentoParaAtualizacao();
        equipamento.Marca = "Fabricante";
        var repositorio = new RepositorioMapaFalso
        {
            TotalInventario = 17,
            ItensInventario = [equipamento]
        };
        var servico = new EquipamentoService(repositorio, TimeProvider.System);

        var (itens, total) = await servico.ListarAsync(filtros, CancellationToken.None);

        Assert.Equal(17, total);
        var resposta = Assert.Single(itens);
        Assert.Equal(equipamento.Id, resposta.Id);
        Assert.Equal(equipamento.NumeroSerie, resposta.NumeroSerie);
        Assert.Equal("Fabricante", resposta.Marca);
        Assert.Equal(filtros, repositorio.FiltrosContagemInventario);
        Assert.Equal(filtros, repositorio.FiltrosListagemInventario);
    }

    [Fact]
    public async Task ConsultarAsync_EquipamentoExistente_RetornaRespostaMapeada()
    {
        var equipamento = CriarEquipamentoParaAtualizacao();
        equipamento.Marca = "Fabricante";
        var repositorio = new RepositorioMapaFalso { EquipamentoConsultado = equipamento };
        var servico = new EquipamentoService(repositorio, TimeProvider.System);

        var resposta = await servico.ConsultarAsync(equipamento.Id, CancellationToken.None);

        Assert.NotNull(resposta);
        Assert.Equal(equipamento.Id, repositorio.IdConsultado);
        Assert.Equal(equipamento.Id, resposta.Id);
        Assert.Equal(equipamento.Tipo, resposta.Tipo);
        Assert.Equal(equipamento.NumeroSerie, resposta.NumeroSerie);
        Assert.Equal("Fabricante", resposta.Marca);
    }

    [Fact]
    public async Task ConsultarAsync_EquipamentoInexistente_RetornaNulo()
    {
        var repositorio = new RepositorioMapaFalso();
        var servico = new EquipamentoService(repositorio, TimeProvider.System);

        var resposta = await servico.ConsultarAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(resposta);
    }

    [Fact]
    public async Task ConsultarIndicadoresAsync_SomaSituacoesEIncluiTotalDeManutencoes()
    {
        IReadOnlyCollection<ContagemSituacao> contagens =
        [
            new(SituacaoEquipamento.Operacional, 8),
            new(SituacaoEquipamento.EmManutencao, 3)
        ];
        var repositorio = new RepositorioMapaFalso
        {
            ContagensPorSituacao = contagens,
            TotalManutencoes = 2,
            TotalManutencoesImediatas = 1,
            TotalManutencoesVencidas = 2,
            TotalManutencoesProximas = 1,
            ResumoLocalidades = [new("Centro", "São Paulo", "SP", 12, 12, 8, 4, 1, 2, 1)],
            Projecao = [new("Centro", "São Paulo", "SP", new DateOnly(2026, 10, 25), 1)]
        };
        var servico = new EquipamentoService(repositorio, new RelogioFalso(new DateTimeOffset(2026, 10, 24, 12, 30, 0, TimeSpan.Zero)));

        var indicadores = await servico.ConsultarIndicadoresAsync(CancellationToken.None);

        Assert.Equal(11, indicadores.TotalCadastrado);
        Assert.Equal(contagens, indicadores.PorSituacao);
        Assert.Equal(2, indicadores.NecessitamManutencao);
        Assert.Equal(1, indicadores.ManutencoesImediatas);
        Assert.Equal(11, indicadores.TotalInstalados);
        Assert.Equal(8, indicadores.Operacionais);
        Assert.Equal(3, indicadores.Indisponiveis);
        Assert.Equal(72.7m, indicadores.PercentualDisponibilidade);
        Assert.Equal(2, indicadores.ManutencoesVencidas);
        Assert.Equal(1, indicadores.ManutencoesProximas);
        Assert.Single(indicadores.PorLocalidade);
        Assert.Single(indicadores.ProjecaoManutencao);
        Assert.Equal(new DateOnly(2026, 10, 24), indicadores.InicioProjecao);
        Assert.Equal(new DateOnly(2026, 11, 23), indicadores.FimProjecao);
        Assert.Equal(new DateOnly(2026, 10, 24), repositorio.InicioAviso);
        Assert.Equal(new DateOnly(2026, 10, 31), repositorio.FimAviso);

    }

    [Fact]
    public async Task ConsultarIndicadores_SemEquipamentosInstalados_DisponibilidadeFicaIndefinida()
    {
        var servico = new EquipamentoService(new RepositorioMapaFalso(), TimeProvider.System);

        var indicadores = await servico.ConsultarIndicadoresAsync(CancellationToken.None);

        Assert.Equal(0, indicadores.TotalInstalados);
        Assert.Null(indicadores.PercentualDisponibilidade);
    }

    private static EquipamentoRequisicao CriarRequisicao() => new(
        " Radar ",
        " SER-001 ",
        " Marca ",
        "   ",
        " 01000-000 ",
        " Centro ",
        " São Paulo ",
        " sp ",
        " Rua Um ",
        " Referência ",
        -23.5m,
        -46.6m,
        SituacaoEquipamento.Operacional,
        false,
        null,
        null,
        OrigemRegistro.Manual);

    private static Equipamento CriarEquipamentoParaAtualizacao() => new()
    {
        Tipo = "Radar",
        NumeroSerie = "SER-ANTIGO",
        Cep = "01000-000",
        Bairro = "Centro",
        Cidade = "São Paulo",
        Uf = "SP",
        Rua = "Rua Antiga",
        Latitude = -23.5m,
        Longitude = -46.6m,
        Situacao = SituacaoEquipamento.Operacional,
        OrigemDados = OrigemRegistro.Manual
    };

    private sealed class RelogioFalso(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }

    private sealed class RepositorioMapaFalso : IEquipamentoRepository
    {
        public FiltrosMapaEquipamentos? FiltrosContagem { get; private set; }
        public FiltrosMapaEquipamentos? FiltrosListagem { get; private set; }
        public int Deslocamento { get; private set; }
        public int Limite { get; private set; }
        public FiltrosEquipamentos? FiltrosContagemInventario { get; private set; }
        public FiltrosEquipamentos? FiltrosListagemInventario { get; private set; }
        public Equipamento? EquipamentoParaAtualizacao { get; init; }
        public Equipamento? EquipamentoConsultado { get; init; }
        public Guid? IdConsultado { get; private set; }
        public int TotalInventario { get; init; }
        public IReadOnlyCollection<Equipamento> ItensInventario { get; init; } = [];
        public IReadOnlyCollection<ContagemSituacao> ContagensPorSituacao { get; init; } = [];
        public int TotalManutencoes { get; init; }
        public int TotalManutencoesImediatas { get; init; }
        public int TotalManutencoesVencidas { get; init; }
        public int TotalManutencoesProximas { get; init; }
        public DateOnly? InicioAviso { get; private set; }
        public DateOnly? FimAviso { get; private set; }
        public IReadOnlyCollection<ResumoOperacionalLocalidade> ResumoLocalidades { get; init; } = [];
        public IReadOnlyCollection<ProjecaoManutencaoLocalidade> Projecao { get; init; } = [];
        public Equipamento? EquipamentoAdicionado { get; private set; }
        public int ConsultasParaAtualizacao { get; private set; }
        public bool SalvouAlteracoes { get; private set; }

        public Task<int> ContarComCoordenadasAsync(FiltrosMapaEquipamentos filtros, CancellationToken cancelamento)
        {
            FiltrosContagem = filtros;
            return Task.FromResult(4);
        }

        public Task<IReadOnlyCollection<Equipamento>> ListarComCoordenadasAsync(
            FiltrosMapaEquipamentos filtros,
            int deslocamento,
            int limite,
            CancellationToken cancelamento)
        {
            FiltrosListagem = filtros;
            Deslocamento = deslocamento;
            Limite = limite;
            IReadOnlyCollection<Equipamento> equipamentos =
            [
                CriarEquipamento("RAD-001", -23.5m, -46.6m),
                CriarEquipamento("RAD-002", -23.6m, -46.7m),
                CriarEquipamento("RAD-003", -23.7m, -46.8m)
            ];
            return Task.FromResult<IReadOnlyCollection<Equipamento>>(equipamentos.Skip(deslocamento).Take(limite).ToArray());
        }

        public Task<int> ContarAsync(FiltrosEquipamentos filtros, CancellationToken cancelamento)
        {
            FiltrosContagemInventario = filtros;
            return Task.FromResult(TotalInventario);
        }

        public Task<IReadOnlyCollection<Equipamento>> ListarAsync(FiltrosEquipamentos filtros, CancellationToken cancelamento)
        {
            FiltrosListagemInventario = filtros;
            return Task.FromResult(ItensInventario);
        }

        public Task<Equipamento?> ConsultarAsync(Guid id, CancellationToken cancelamento)
        {
            IdConsultado = id;
            return Task.FromResult(EquipamentoConsultado);
        }

        public Task<Equipamento?> ConsultarParaAtualizacaoAsync(Guid id, CancellationToken cancelamento)
        {
            ConsultasParaAtualizacao++;
            return Task.FromResult(EquipamentoParaAtualizacao);
        }

        public Task<IReadOnlyCollection<ContagemSituacao>> ContarPorSituacaoAsync(CancellationToken cancelamento) =>
            Task.FromResult(ContagensPorSituacao);

        public Task<int> ContarManutencoesNecessariasAsync(CancellationToken cancelamento) =>
            Task.FromResult(TotalManutencoes);

        public Task<int> ContarManutencoesImediatasAsync(CancellationToken cancelamento) =>
            Task.FromResult(TotalManutencoesImediatas);

        public Task<int> ContarManutencoesComDataAteAsync(DateOnly limite, CancellationToken cancelamento) =>
            Task.FromResult(TotalManutencoesVencidas);

        public Task<int> ContarManutencoesEntreAsync(DateOnly inicio, DateOnly fim, CancellationToken cancelamento)
        {
            InicioAviso = inicio;
            FimAviso = fim;
            return Task.FromResult(TotalManutencoesProximas);
        }

        public Task<IReadOnlyCollection<ResumoOperacionalLocalidade>> ConsultarResumoPorLocalidadeAsync(
            DateOnly hoje,
            DateOnly fimJanelaAviso,
            CancellationToken cancelamento) => Task.FromResult(ResumoLocalidades);

        public Task<IReadOnlyCollection<ProjecaoManutencaoLocalidade>> ConsultarProjecaoManutencaoAsync(
            DateOnly hoje,
            DateOnly fimHorizonte,
            CancellationToken cancelamento) => Task.FromResult(Projecao);

        public Task AdicionarAsync(Equipamento equipamento, CancellationToken cancelamento)
        {
            EquipamentoAdicionado = equipamento;
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken cancelamento)
        {
            SalvouAlteracoes = true;
            return Task.CompletedTask;
        }

        private static Equipamento CriarEquipamento(string numeroSerie, decimal latitude, decimal longitude) => new()
        {
            Tipo = "Radar",
            NumeroSerie = numeroSerie,
            Marca = "Fabricante",
            Modelo = "ZX",
            Caracteristicas = new Dictionary<string, string> { ["Limite"] = "50" },
            Cep = "01000-000",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Uf = "SP",
            Rua = "Rua Um",
            Latitude = latitude,
            Longitude = longitude,
            Situacao = SituacaoEquipamento.Operacional,
            NecessitaManutencao = true,
            TipoManutencao = TipoManutencao.Agendada,
            ProximaManutencao = new DateOnly(2026, 11, 1),
            OrigemDados = OrigemRegistro.Manual
        };
    }
}
