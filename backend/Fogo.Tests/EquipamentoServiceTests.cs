using Fogo.Api.Application.Inventario;
using Fogo.Api.Inventario;
using Fogo.Api.Repositories;
using Xunit;

namespace Fogo.Tests;

public sealed class EquipamentoServiceTests
{
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

    private sealed class RepositorioMapaFalso : IEquipamentoRepository
    {
        public FiltrosMapaEquipamentos? FiltrosContagem { get; private set; }
        public FiltrosMapaEquipamentos? FiltrosListagem { get; private set; }
        public int Limite { get; private set; }
        public FiltrosEquipamentos? FiltrosContagemInventario { get; private set; }
        public FiltrosEquipamentos? FiltrosListagemInventario { get; private set; }

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

        public Task<Equipamento?> ConsultarParaAtualizacaoAsync(Guid id, CancellationToken cancelamento) =>
            throw new NotImplementedException();

        public Task<IReadOnlyCollection<ContagemSituacao>> ContarPorSituacaoAsync(CancellationToken cancelamento) =>
            throw new NotImplementedException();

        public Task<int> ContarManutencoesNecessariasAsync(CancellationToken cancelamento) =>
            throw new NotImplementedException();

        public Task AdicionarAsync(Equipamento equipamento, CancellationToken cancelamento) =>
            throw new NotImplementedException();

        public Task SalvarAlteracoesAsync(CancellationToken cancelamento) =>
            throw new NotImplementedException();

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
