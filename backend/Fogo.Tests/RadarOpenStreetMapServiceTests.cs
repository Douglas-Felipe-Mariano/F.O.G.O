using System.Net;
using System.Text;
using Fogo.Api.Application.Inventario;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Fogo.Tests;

public sealed class RadarOpenStreetMapServiceTests
{
    [Fact]
    public async Task ConsultarAsync_ConverteNodesEWaysEArmazenaResultadoEmCache()
    {
        const string json = """
            {
              "elements": [
                { "type": "node", "id": 101, "lat": -21.93, "lon": -50.51, "tags": { "highway": "speed_camera", "maxspeed": "60", "addr:city": "Tupã" } },
                { "type": "way", "id": 202, "center": { "lat": -22.00, "lon": -50.29 }, "tags": { "highway": "speed_camera", "name": "SP-294" } },
                { "type": "node", "id": 303, "lat": -21.90, "tags": { "highway": "speed_camera" } }
              ]
            }
            """;
        var handler = new HandlerFalso(HttpStatusCode.OK, json);
        using var cliente = new HttpClient(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var servico = new RadarOpenStreetMapService(cliente, cache);

        var primeiraConsulta = await servico.ConsultarAsync(CancellationToken.None);
        var segundaConsulta = await servico.ConsultarAsync(CancellationToken.None);

        Assert.True(primeiraConsulta.Disponivel);
        Assert.Equal("OpenStreetMap / Overpass API", primeiraConsulta.Fonte);
        Assert.Equal(2, primeiraConsulta.Itens.Count);
        Assert.Equal("osm-node-101", primeiraConsulta.Itens.First().Id);
        Assert.Equal(-21.93m, primeiraConsulta.Itens.First().Latitude);
        Assert.Equal("60", primeiraConsulta.Itens.First().VelocidadeMaxima);
        Assert.Equal("way", new Uri(primeiraConsulta.Itens.Last().UrlFonte).Segments[^2].Trim('/'));
        Assert.Equal(2, segundaConsulta.Itens.Count);
        Assert.Equal(1, handler.QuantidadeRequisicoes);
    }

    [Fact]
    public async Task ConsultarAsync_QuandoFonteFalha_RetornaAvisoSemLancarExcecao()
    {
        var handler = new HandlerFalso(HttpStatusCode.ServiceUnavailable, "");
        using var cliente = new HttpClient(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var servico = new RadarOpenStreetMapService(cliente, cache);

        var resposta = await servico.ConsultarAsync(CancellationToken.None);

        Assert.False(resposta.Disponivel);
        Assert.Empty(resposta.Itens);
        Assert.Contains("temporariamente indisponível", resposta.Aviso);
    }

    private sealed class HandlerFalso(HttpStatusCode statusCode, string conteudo) : HttpMessageHandler
    {
        public int QuantidadeRequisicoes { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            QuantidadeRequisicoes++;
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://overpass-api.de/api/interpreter", request.RequestUri!.ToString());
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(conteudo, Encoding.UTF8, "application/json")
            });
        }
    }
}
