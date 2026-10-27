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

    public async Task<MapaEquipamentosResposta> ListarLocalizacoesAsync(
        FiltrosMapaEquipamentos filtros,
        int deslocamento,
        int limite,
        CancellationToken cancelamento)
    {
        var total = await repositorio.ContarComCoordenadasAsync(filtros, cancelamento);
        var equipamentos = await repositorio.ListarComCoordenadasAsync(filtros, deslocamento, limite, cancelamento);
        var localizacoes = equipamentos.Select(equipamento => new LocalizacaoEquipamentoResposta(
            equipamento.Id,
            equipamento.Tipo,
            equipamento.NumeroSerie,
            equipamento.Marca,
            equipamento.Modelo,
            new Dictionary<string, string>(equipamento.Caracteristicas),
            equipamento.Cep,
            equipamento.Bairro,
            equipamento.Cidade,
            equipamento.Uf,
            equipamento.Rua,
            equipamento.Referencia,
            equipamento.Latitude!.Value,
            equipamento.Longitude!.Value,
            equipamento.Situacao,
            equipamento.NecessitaManutencao,
            equipamento.TipoManutencao,
            equipamento.ProximaManutencao,
            equipamento.OrigemDados)).ToArray();

        return new MapaEquipamentosResposta(total, limite, (long)deslocamento + localizacoes.Length < total, localizacoes);
    }

    public async Task<IndicadoresEquipamentosResposta> ConsultarIndicadoresAsync(CancellationToken cancelamento)
    {
        var porSituacao = await repositorio.ContarPorSituacaoAsync(cancelamento);
        var total = porSituacao.Sum(item => item.Quantidade);
        var naoInstalados = porSituacao
            .Where(item => item.Situacao == SituacaoEquipamento.AindaNaoInstalado)
            .Sum(item => item.Quantidade);
        var instalados = total - naoInstalados;
        var operacionais = porSituacao
            .Where(item => item.Situacao == SituacaoEquipamento.Operacional)
            .Sum(item => item.Quantidade);
        var indisponiveis = instalados - operacionais;
        var manutencoesNecessarias = await repositorio.ContarManutencoesNecessariasAsync(cancelamento);
        var manutencoesImediatas = await repositorio.ContarManutencoesImediatasAsync(cancelamento);
        var hoje = DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime);
        var fimJanelaAviso = hoje.AddDays(7);
        var manutencoesVencidas = await repositorio.ContarManutencoesComDataAteAsync(hoje, cancelamento);
        var manutencoesProximas = await repositorio.ContarManutencoesEntreAsync(hoje, fimJanelaAviso, cancelamento);
        var resumoPorLocalidade = await repositorio.ConsultarResumoPorLocalidadeAsync(hoje, fimJanelaAviso, cancelamento);
        var horizonteProjecao = hoje.AddDays(30);
        var projecaoManutencao = await repositorio.ConsultarProjecaoManutencaoAsync(hoje, horizonteProjecao, cancelamento);
        decimal? percentualDisponibilidade = instalados == 0
            ? null
            : decimal.Round(operacionais * 100m / instalados, 1);
        return new IndicadoresEquipamentosResposta(
            total,
            porSituacao,
            manutencoesNecessarias,
            manutencoesImediatas,
            instalados,
            operacionais,
            indisponiveis,
            percentualDisponibilidade,
            manutencoesVencidas,
            manutencoesProximas,
            resumoPorLocalidade,
            projecaoManutencao,
            hoje,
            horizonteProjecao);
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
        equipamento.Caracteristicas = requisicao.Caracteristicas is null
            ? []
            : new Dictionary<string, string>(requisicao.Caracteristicas);
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
    int NecessitamManutencao,
    int ManutencoesImediatas,
    int TotalInstalados,
    int Operacionais,
    int Indisponiveis,
    decimal? PercentualDisponibilidade,
    int ManutencoesVencidas,
    int ManutencoesProximas,
    IReadOnlyCollection<ResumoOperacionalLocalidade> PorLocalidade,
    IReadOnlyCollection<ProjecaoManutencaoLocalidade> ProjecaoManutencao,
    DateOnly InicioProjecao,
    DateOnly FimProjecao);

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
