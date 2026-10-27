using Fogo.Api.Inventario;

namespace Fogo.Api.Application.Inventario;

public static class ValidadorEquipamento
{
    public static Dictionary<string, string[]> Validar(EquipamentoRequisicao requisicao)
    {
        var erros = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        void Adicionar(string campo, string mensagem)
        {
            if (!erros.TryGetValue(campo, out var mensagens))
            {
                mensagens = [];
                erros[campo] = mensagens;
            }

            mensagens.Add(mensagem);
        }

        ValidarObrigatorio(requisicao.Tipo, 60, nameof(requisicao.Tipo), Adicionar);
        ValidarObrigatorio(requisicao.NumeroSerie, 100, nameof(requisicao.NumeroSerie), Adicionar);
        ValidarObrigatorio(requisicao.Cep, 20, nameof(requisicao.Cep), Adicionar);
        ValidarObrigatorio(requisicao.Bairro, 120, nameof(requisicao.Bairro), Adicionar);
        ValidarObrigatorio(requisicao.Cidade, 120, nameof(requisicao.Cidade), Adicionar);
        ValidarObrigatorio(requisicao.Uf, 2, nameof(requisicao.Uf), Adicionar);
        ValidarObrigatorio(requisicao.Rua, 180, nameof(requisicao.Rua), Adicionar);
        ValidarOpcional(requisicao.Marca, 100, nameof(requisicao.Marca), Adicionar);
        ValidarOpcional(requisicao.Modelo, 120, nameof(requisicao.Modelo), Adicionar);
        ValidarOpcional(requisicao.Referencia, 240, nameof(requisicao.Referencia), Adicionar);
        if (requisicao.Caracteristicas is { } caracteristicas)
        {
            if (caracteristicas.Count > 30)
            {
                Adicionar(nameof(requisicao.Caracteristicas), "Informe no máximo 30 características por equipamento.");
            }

            foreach (var (chave, valor) in caracteristicas)
            {
                if (string.IsNullOrWhiteSpace(chave) || chave.Trim().Length > 60)
                {
                    Adicionar(nameof(requisicao.Caracteristicas), "Cada nome de característica deve ter entre 1 e 60 caracteres.");
                    break;
                }

                if (valor is null || valor.Length > 500)
                {
                    Adicionar(nameof(requisicao.Caracteristicas), "Cada valor de característica deve ter no máximo 500 caracteres.");
                    break;
                }
            }
        }

        if (requisicao.Latitude.HasValue != requisicao.Longitude.HasValue)
        {
            Adicionar(nameof(requisicao.Latitude), "Informe latitude e longitude juntas, ou deixe ambas vazias.");
        }

        if (requisicao.Latitude is < -90 or > 90)
        {
            Adicionar(nameof(requisicao.Latitude), "A latitude deve estar entre -90 e 90.");
        }

        if (requisicao.Longitude is < -180 or > 180)
        {
            Adicionar(nameof(requisicao.Longitude), "A longitude deve estar entre -180 e 180.");
        }

        if (!Enum.IsDefined(requisicao.Situacao) || requisicao.Situacao == SituacaoEquipamento.NaoInformado)
        {
            Adicionar(nameof(requisicao.Situacao), "Informe uma situação prevista para o equipamento.");
        }

        if (!Enum.IsDefined(requisicao.OrigemDados) || requisicao.OrigemDados == OrigemRegistro.NaoInformada)
        {
            Adicionar(nameof(requisicao.OrigemDados), "Informe se o registro é manual, importado ou simulado.");
        }

        if (requisicao.NecessitaManutencao != requisicao.TipoManutencao.HasValue)
        {
            Adicionar(nameof(requisicao.TipoManutencao), "Informe o tipo de manutenção somente quando houver manutenção necessária.");
        }

        var tipoManutencaoValido = !requisicao.TipoManutencao.HasValue
            || Enum.IsDefined(requisicao.TipoManutencao.Value);
        if (!tipoManutencaoValido)
        {
            Adicionar(nameof(requisicao.TipoManutencao), "O tipo de manutenção informado é inválido.");
        }

        if (tipoManutencaoValido
            && requisicao.TipoManutencao.HasValue
            && (requisicao.TipoManutencao.Value is TipoManutencao.Agendada or TipoManutencao.Periodica)
            && requisicao.ProximaManutencao is null)
        {
            Adicionar(nameof(requisicao.ProximaManutencao), "Informe a data da próxima manutenção agendada ou periódica.");
        }

        if ((requisicao.TipoManutencao is null or TipoManutencao.Imediata)
            && requisicao.ProximaManutencao is not null)
        {
            Adicionar(nameof(requisicao.ProximaManutencao), "A próxima data só se aplica a manutenção agendada ou periódica.");
        }

        return erros.ToDictionary(item => item.Key, item => item.Value.ToArray(), StringComparer.Ordinal);
    }

    private static void ValidarObrigatorio(string? valor, int maximo, string campo, Action<string, string> adicionar)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            adicionar(campo, "Este campo é obrigatório.");
        }
        else if (valor.Trim().Length > maximo)
        {
            adicionar(campo, $"O campo deve ter no máximo {maximo} caracteres.");
        }
    }

    private static void ValidarOpcional(string? valor, int maximo, string campo, Action<string, string> adicionar)
    {
        if (valor?.Trim().Length > maximo)
        {
            adicionar(campo, $"O campo deve ter no máximo {maximo} caracteres.");
        }
    }
}
