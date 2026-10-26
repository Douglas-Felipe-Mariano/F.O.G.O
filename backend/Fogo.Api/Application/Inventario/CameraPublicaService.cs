using Fogo.Api.Identity;
using Fogo.Api.Inventario;
using Microsoft.EntityFrameworkCore;

namespace Fogo.Api.Application.Inventario;

public sealed class CameraPublicaService(FogoDbContext banco)
{
    public async Task<PaginaCamerasResposta> ListarAsync(string? cidade, CancellationToken cancelamento)
    {
        var consulta = banco.CamerasPublicas.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(cidade))
        {
            consulta = consulta.Where(camera => camera.Cidade == cidade.Trim());
        }

        var itens = await consulta
            .OrderBy(camera => camera.Cidade)
            .ThenBy(camera => camera.Nome)
            .Select(camera => new CameraPublicaResposta(
                camera.Id,
                camera.Cidade,
                camera.Nome,
                camera.Endereco,
                camera.Latitude,
                camera.Longitude,
                camera.UrlStream))
            .ToArrayAsync(cancelamento);
        return new PaginaCamerasResposta(itens.Length, itens);
    }
}
