using System.Net.Http.Headers;
using System.Text.Json;
using Fogo.Api.Inventario;
using Microsoft.Extensions.Caching.Memory;

namespace Fogo.Api.Application.Inventario;

public sealed class RadarOpenStreetMapService(HttpClient clienteHttp, IMemoryCache cache)
{
    private const string ChaveCache = "radares-osm-tupa-regiao";
    private const string UrlOverpass = "https://overpass-api.de/api/interpreter";
    private const string Consulta = "[out:json][timeout:12];nwr[\"highway\"=\"speed_camera\"](-22.20,-50.90,-21.65,-50.10);out center tags;";
    private static readonly TimeSpan ValidadeCache = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ValidadeFalha = TimeSpan.FromMinutes(1);

    public async Task<RadaresOpenStreetMapResposta> ConsultarAsync(CancellationToken cancelamento)
    {
        return await cache.GetOrCreateAsync(ChaveCache, async entrada =>
        {
            try
            {
                using var requisicao = new HttpRequestMessage(HttpMethod.Post, UrlOverpass)
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["data"] = Consulta })
                };
                requisicao.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                using var resposta = await clienteHttp.SendAsync(
                    requisicao,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancelamento);

                if (!resposta.IsSuccessStatusCode)
                {
                    entrada.AbsoluteExpirationRelativeToNow = ValidadeFalha;
                    return Indisponivel();
                }

                await using var fluxo = await resposta.Content.ReadAsStreamAsync(cancelamento);
                using var documento = await JsonDocument.ParseAsync(fluxo, cancellationToken: cancelamento);
                var itens = LerRadares(documento.RootElement);
                entrada.AbsoluteExpirationRelativeToNow = ValidadeCache;
                return new RadaresOpenStreetMapResposta(true, "OpenStreetMap / Overpass API", string.Empty, itens);
            }
            catch (OperationCanceledException) when (!cancelamento.IsCancellationRequested)
            {
                entrada.AbsoluteExpirationRelativeToNow = ValidadeFalha;
                return Indisponivel();
            }
            catch (HttpRequestException)
            {
                entrada.AbsoluteExpirationRelativeToNow = ValidadeFalha;
                return Indisponivel();
            }
            catch (JsonException)
            {
                entrada.AbsoluteExpirationRelativeToNow = ValidadeFalha;
                return Indisponivel();
            }
        }) ?? Indisponivel();
    }

    private static IReadOnlyCollection<RadarOpenStreetMapItem> LerRadares(JsonElement raiz)
    {
        if (!raiz.TryGetProperty("elements", out var elementos) || elementos.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var radares = new List<RadarOpenStreetMapItem>();
        foreach (var elemento in elementos.EnumerateArray())
        {
            if (!elemento.TryGetProperty("id", out var idElemento)
                || !idElemento.TryGetInt64(out var id)
                || !elemento.TryGetProperty("type", out var tipoElemento))
            {
                continue;
            }

            var tipoElementoOsm = tipoElemento.GetString();
            if (tipoElementoOsm is not ("node" or "way" or "relation"))
            {
                continue;
            }

            var coordenada = elemento;
            if (elemento.TryGetProperty("center", out var centro))
            {
                coordenada = centro;
            }

            if (!TentarLerDecimal(coordenada, "lat", out var latitude)
                || !TentarLerDecimal(coordenada, "lon", out var longitude)
                || !elemento.TryGetProperty("tags", out var tags))
            {
                continue;
            }

            var nome = LerTag(tags, "name") ?? LerTag(tags, "ref");
            var velocidade = LerTag(tags, "maxspeed");
            var endereco = LerTag(tags, "addr:street") ?? LerTag(tags, "ref") ?? "Trecho de via mapeado";
            var bairro = LerTag(tags, "addr:suburb") ?? LerTag(tags, "addr:district") ?? "Região de Tupã";
            var cidade = LerTag(tags, "addr:city") ?? "Tupã e região";
            var urlFonte = $"https://www.openstreetmap.org/{tipoElementoOsm}/{id}";

            radares.Add(new RadarOpenStreetMapItem(
                $"osm-{tipoElementoOsm}-{id}",
                "Radar mapeado no OpenStreetMap",
                $"OSM-{id}",
                bairro,
                cidade,
                "SP",
                endereco,
                nome is null ? velocidade : $"{nome}{(velocidade is null ? string.Empty : $" · Máx. {velocidade}")}",
                latitude,
                longitude,
                SituacaoEquipamento.Operacional,
                "OpenStreetMap",
                urlFonte,
                velocidade));
        }

        return radares;
    }

    private static bool TentarLerDecimal(JsonElement elemento, string propriedade, out decimal valor)
    {
        valor = 0;
        return elemento.TryGetProperty(propriedade, out var coordenada)
            && coordenada.ValueKind == JsonValueKind.Number
            && coordenada.TryGetDecimal(out valor);
    }

    private static string? LerTag(JsonElement tags, string nome) =>
        tags.ValueKind == JsonValueKind.Object
        && tags.TryGetProperty(nome, out var tag)
        && tag.ValueKind == JsonValueKind.String
            ? tag.GetString()
            : null;

    private static RadaresOpenStreetMapResposta Indisponivel() => new(
        false,
        "OpenStreetMap / Overpass API",
        "A consulta pública de radares está temporariamente indisponível. Os dados demonstrativos continuam visíveis.",
        []);
}

public sealed record RadarOpenStreetMapItem(
    string Id,
    string Tipo,
    string NumeroSerie,
    string Bairro,
    string Cidade,
    string Uf,
    string Rua,
    string? Referencia,
    decimal Latitude,
    decimal Longitude,
    SituacaoEquipamento Situacao,
    string Fonte,
    string UrlFonte,
    string? VelocidadeMaxima);

public sealed record RadaresOpenStreetMapResposta(
    bool Disponivel,
    string Fonte,
    string Aviso,
    IReadOnlyCollection<RadarOpenStreetMapItem> Itens);
