using Fogo.Api.Inventario;
using Fogo.Api.Repositories;

namespace Fogo.Api.Application.Inventario;

public sealed class EquipamentoService(
    IEquipamentoRepository repositorio,
    TimeProvider relogio)
{
    public async Task<(IReadOnlyCollection<EquipamentoResposta> Itens, int Total)> ListarAsync(
        FiltrosEquipamentos filtros,
        CancellationToken cancelamento)
    {
        var total = await repositorio.ContarAsync(filtros, cancelamento);
        var entidades = await repositorio.ListarAsync(filtros, cancelamento);
        return (entidades.Select(EquipamentoResposta.De).ToArray(), total);
    }

    public async Task<EquipamentoResposta?> ConsultarAsync(Guid id, CancellationToken cancelamento)
    {
        var equipamento = await repositorio.ConsultarAsync(id, cancelamento);
        return equipamento is null ? null : EquipamentoResposta.De(equipamento);
    }

    public async Task<MapaEquipamentosResposta> ListarLocalizacoesAsync(int limite, CancellationToken cancelamento)
    {
        var total = await repositorio.ContarComCoordenadasAsync(cancelamento);
        var equipamentos = await repositorio.ListarComCoordenadasAsync(limite, cancelamento);
        var localizacoes = equipamentos.Select(equipamento => new LocalizacaoEquipamentoResposta(
            equipamento.Id,
            equipamento.Tipo,
            equipamento.NumeroSerie,
            equipamento.Bairro,
            equipamento.Cidade,
            equipamento.Uf,
            equipamento.Rua,
            equipamento.Referencia,
            equipamento.Latitude!.Value,
            equipamento.Longitude!.Value,
            equipamento.Situacao)).ToArray();

        return new MapaEquipamentosResposta(total, limite, total > localizacoes.Length, localizacoes);
    }

    public async Task<IndicadoresEquipamentosResposta> ConsultarIndicadoresAsync(CancellationToken cancelamento)
    {
        var porSituacao = await repositorio.ContarPorSituacaoAsync(cancelamento);
        var total = porSituacao.Sum(item => item.Quantidade);
        var manutencoesNecessarias = await repositorio.ContarManutencoesNecessariasAsync(cancelamento);
        return new IndicadoresEquipamentosResposta(total, porSituacao, manutencoesNecessarias);
    }

    public async Task<ResultadoOperacao<EquipamentoResposta>> CriarAsync(
        EquipamentoRequisicao requisicao,
        CancellationToken cancelamento)
    {
        var erros = ValidadorEquipamento.Validar(requisicao);
        if (erros.Count > 0)
        {
            return ResultadoOperacao<EquipamentoResposta>.Invalido(erros);
        }

        var agora = relogio.GetUtcNow();
        var equipamento = CriarEntidade(requisicao, agora);
        await repositorio.AdicionarAsync(equipamento, cancelamento);
        return ResultadoOperacao<EquipamentoResposta>.Sucesso(EquipamentoResposta.De(equipamento));
    }

    public async Task<ResultadoOperacao<EquipamentoResposta>> AtualizarAsync(
        Guid id,
        EquipamentoRequisicao requisicao,
        CancellationToken cancelamento)
    {
        var erros = ValidadorEquipamento.Validar(requisicao);
        if (erros.Count > 0)
        {
            return ResultadoOperacao<EquipamentoResposta>.Invalido(erros);
        }

        var equipamento = await repositorio.ConsultarParaAtualizacaoAsync(id, cancelamento);
        if (equipamento is null)
        {
            return ResultadoOperacao<EquipamentoResposta>.RecursoNaoEncontrado();
        }

        Aplicar(equipamento, requisicao);
        equipamento.AtualizadoEm = relogio.GetUtcNow();
        await repositorio.SalvarAlteracoesAsync(cancelamento);
        return ResultadoOperacao<EquipamentoResposta>.Sucesso(EquipamentoResposta.De(equipamento));
    }

    private static Equipamento CriarEntidade(EquipamentoRequisicao requisicao, DateTimeOffset agora)
    {
        var equipamento = new Equipamento
        {
            Tipo = requisicao.Tipo.Trim(),
            NumeroSerie = requisicao.NumeroSerie.Trim(),
            Cep = requisicao.Cep.Trim(),
            Bairro = requisicao.Bairro.Trim(),
            Cidade = requisicao.Cidade.Trim(),
            Uf = requisicao.Uf.Trim().ToUpperInvariant(),
            Rua = requisicao.Rua.Trim(),
            CriadoEm = agora,
            AtualizadoEm = agora
        };
        Aplicar(equipamento, requisicao);
        return equipamento;
    }

    private static void Aplicar(Equipamento equipamento, EquipamentoRequisicao requisicao)
    {
        equipamento.Tipo = requisicao.Tipo.Trim();
        equipamento.NumeroSerie = requisicao.NumeroSerie.Trim();
        equipamento.Marca = LimparOpcional(requisicao.Marca);
        equipamento.Modelo = LimparOpcional(requisicao.Modelo);
        equipamento.Cep = requisicao.Cep.Trim();
        equipamento.Bairro = requisicao.Bairro.Trim();
        equipamento.Cidade = requisicao.Cidade.Trim();
        equipamento.Uf = requisicao.Uf.Trim().ToUpperInvariant();
        equipamento.Rua = requisicao.Rua.Trim();
        equipamento.Referencia = LimparOpcional(requisicao.Referencia);
        equipamento.Latitude = requisicao.Latitude;
        equipamento.Longitude = requisicao.Longitude;
        equipamento.Situacao = requisicao.Situacao;
        equipamento.NecessitaManutencao = requisicao.NecessitaManutencao;
        equipamento.TipoManutencao = requisicao.TipoManutencao;
        equipamento.ProximaManutencao = requisicao.ProximaManutencao;
        equipamento.OrigemDados = requisicao.OrigemDados;
    }

    private static string? LimparOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}

public sealed record IndicadoresEquipamentosResposta(
    int TotalCadastrado,
    IReadOnlyCollection<ContagemSituacao> PorSituacao,
    int NecessitamManutencao);

public sealed class ResultadoOperacao<T>
{
    private ResultadoOperacao(T? valor, Dictionary<string, string[]> erros, bool naoEncontrado)
    {
        Valor = valor;
        Erros = erros;
        NaoEncontrado = naoEncontrado;
    }

    public T? Valor { get; }
    public Dictionary<string, string[]> Erros { get; }
    public bool NaoEncontrado { get; }
    public bool SucessoOperacao => Valor is not null && Erros.Count == 0 && !NaoEncontrado;

    public static ResultadoOperacao<T> Sucesso(T valor) => new(valor, [], false);
    public static ResultadoOperacao<T> Invalido(Dictionary<string, string[]> erros) => new(default, erros, false);
    public static ResultadoOperacao<T> RecursoNaoEncontrado() => new(default, [], true);
}
