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

        var resposta = await servico.ListarLocalizacoesAsync(filtros, 2, CancellationToken.None);

        Assert.Equal(filtros, repositorio.FiltrosContagem);
        Assert.Equal(filtros, repositorio.FiltrosListagem);
        Assert.Equal(2, repositorio.Limite);
        Assert.Equal(3, resposta.TotalComCoordenadas);
        Assert.Equal(2, resposta.Limite);
        Assert.True(resposta.ResultadoTruncado);
        Assert.Equal(2, resposta.Itens.Count);
        Assert.All(resposta.Itens, item => Assert.NotEqual(Guid.Empty, item.Id));
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
        public int Limite { get; private set; }
        public FiltrosEquipamentos? FiltrosContagemInventario { get; private set; }
        public FiltrosEquipamentos? FiltrosListagemInventario { get; private set; }
        public Equipamento? EquipamentoParaAtualizacao { get; init; }
        public Equipamento? EquipamentoAdicionado { get; private set; }
        public int ConsultasParaAtualizacao { get; private set; }
        public bool SalvouAlteracoes { get; private set; }

        public Task<int> ContarComCoordenadasAsync(FiltrosMapaEquipamentos filtros, CancellationToken cancelamento)
        {
            FiltrosContagem = filtros;
            return Task.FromResult(3);
        }

        public Task<IReadOnlyCollection<Equipamento>> ListarComCoordenadasAsync(
            FiltrosMapaEquipamentos filtros,
            int limite,
            CancellationToken cancelamento)
        {
            FiltrosListagem = filtros;
            Limite = limite;
            IReadOnlyCollection<Equipamento> equipamentos =
            [
                CriarEquipamento("RAD-001", -23.5m, -46.6m),
                CriarEquipamento("RAD-002", -23.6m, -46.7m)
            ];
            return Task.FromResult(equipamentos);
        }

        public Task<int> ContarAsync(FiltrosEquipamentos filtros, CancellationToken cancelamento)
        {
            FiltrosContagemInventario = filtros;
            return Task.FromResult(0);
        }

        public Task<IReadOnlyCollection<Equipamento>> ListarAsync(FiltrosEquipamentos filtros, CancellationToken cancelamento)
        {
            FiltrosListagemInventario = filtros;
            return Task.FromResult<IReadOnlyCollection<Equipamento>>([]);
        }

        public Task<Equipamento?> ConsultarAsync(Guid id, CancellationToken cancelamento) =>
            throw new NotImplementedException();

        public Task<Equipamento?> ConsultarParaAtualizacaoAsync(Guid id, CancellationToken cancelamento)
        {
            ConsultasParaAtualizacao++;
            return Task.FromResult(EquipamentoParaAtualizacao);
        }

        public Task<IReadOnlyCollection<ContagemSituacao>> ContarPorSituacaoAsync(CancellationToken cancelamento) =>
            throw new NotImplementedException();

        public Task<int> ContarManutencoesNecessariasAsync(CancellationToken cancelamento) =>
            throw new NotImplementedException();

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
            Cep = "01000-000",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Uf = "SP",
            Rua = "Rua Um",
            Latitude = latitude,
            Longitude = longitude,
            Situacao = SituacaoEquipamento.Operacional,
            OrigemDados = OrigemRegistro.Manual
        };
    }
}
