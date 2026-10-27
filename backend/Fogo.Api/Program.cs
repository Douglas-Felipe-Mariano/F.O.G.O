using System.Text.Json;
using System.Text.Json.Serialization;
using Fogo.Api;
using Fogo.Api.Application.Inventario;
using Fogo.Api.Identity;
using Fogo.Api.Repositories;
using Microsoft.EntityFrameworkCore;

const string argumentoProvisionarPrimeiroUsuario = "--provisionar-primeiro-usuario";
const string argumentoProvisionarUsuario = "--provisionar-usuario";
if (args.Length == 1 && args[0] == "--healthcheck")
{
    try
    {
        using var clienteHttp = new HttpClient();
        using var resposta = await clienteHttp.GetAsync("http://127.0.0.1:8080/api/saude");
        Environment.ExitCode = resposta.IsSuccessStatusCode ? 0 : 1;
    }
    catch (HttpRequestException)
    {
        Environment.ExitCode = 1;
    }

    return;
}

var provisionarPrimeiroUsuario = args.Contains(argumentoProvisionarPrimeiroUsuario, StringComparer.Ordinal);
var provisionarUsuario = args.Contains(argumentoProvisionarUsuario, StringComparer.Ordinal);
if (provisionarPrimeiroUsuario && provisionarUsuario)
{
    Console.Error.WriteLine("Use somente um dos comandos de provisionamento por execução.");
    Environment.ExitCode = 2;
    return;
}

var argumentosHospedeiro = args
    .Where(argumento => argumento != argumentoProvisionarPrimeiroUsuario && argumento != argumentoProvisionarUsuario)
    .ToArray();
var builder = WebApplication.CreateBuilder(argumentosHospedeiro);

builder.AdicionarAutenticacaoFogo();
builder.Services.AdicionarProtecaoLogin();
builder.Services.ConfigureHttpJsonOptions(opcoes =>
{
    opcoes.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
    opcoes.SerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.Services.AddAuthorizationBuilder().SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build());

builder.Services.AddOpenApi();
builder.Services.AddControllers().AddJsonOptions(opcoes =>
{
    opcoes.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
    opcoes.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IEmailSenderFogo, SmtpEmailSender>();
builder.Services.AddHttpClient<RadarOpenStreetMapService>(cliente =>
{
    cliente.Timeout = TimeSpan.FromSeconds(15);
    cliente.DefaultRequestHeaders.UserAgent.ParseAdd("FOGO-Operations-Map/1.0");
});
builder.Services.AddScoped<IEquipamentoRepository, EquipamentoRepository>();
builder.Services.AddScoped<EquipamentoService>();
builder.Services.AddScoped<IAgenteRepository, AgenteRepository>();
builder.Services.AddScoped<AgenteService>();
builder.Services.AddScoped<HistoricoAgentesService>();
builder.Services.AddScoped<CameraPublicaService>();
builder.Services.AddScoped<ProvisionadorPrimeiroUsuario>();
builder.Services.AddScoped<IPublicadorEventosEquipamentos, PublicadorEventosEquipamentos>();
builder.Services.AddScoped<IPublicadorEventosAgentes, PublicadorEventosAgentes>();

var app = builder.Build();

if (builder.Configuration.GetValue("FOGO_HTTPS_REDIRECIONAR", !app.Environment.IsEnvironment("Testing")))
{
    app.UseHttpsRedirection();
}

if (builder.Configuration.GetValue<bool>("FOGO_APLICAR_MIGRATIONS"))
{
    await using var escopo = app.Services.CreateAsyncScope();
    var banco = escopo.ServiceProvider.GetRequiredService<FogoDbContext>();
    await banco.Database.MigrateAsync();
}

if (provisionarPrimeiroUsuario || provisionarUsuario)
{
    await using var escopo = app.Services.CreateAsyncScope();
    var provisionador = escopo.ServiceProvider.GetRequiredService<ProvisionadorPrimeiroUsuario>();
    Environment.ExitCode = await provisionador.ExecutarAsync(
        CancellationToken.None,
        somentePrimeiroUsuario: provisionarPrimeiroUsuario) ? 0 : 1;
    await app.DisposeAsync();
    return;
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/saude", () => Results.Ok(new { estado = "disponivel" }))
    .AllowAnonymous()
    .WithName("ConsultarSaude")
    .WithSummary("Verifica se a API está respondendo.");

app.MapGet("/api/radares/openstreetmap", async (
        RadarOpenStreetMapService radares,
        CancellationToken cancelamento) =>
        Results.Ok(await radares.ConsultarAsync(cancelamento)))
    .WithName("ConsultarRadaresOpenStreetMap")
    .WithSummary("Consulta radares mapeados pelo OpenStreetMap na região de Tupã; os registros importados não são gravados no banco.");

app.MapGet("/api/cameras-publicas", async (
        string? cidade,
        CameraPublicaService cameras,
        CancellationToken cancelamento) =>
        Results.Ok(await cameras.ListarAsync(cidade, cancelamento)))
    .WithName("ListarCamerasPublicas")
    .WithSummary("Lista câmeras públicas com localização geográfica verificada e streams disponibilizados publicamente.");

app.MapGet("/api/agentes/{id:guid}/trajeto", async (
        Guid id,
        DateOnly? data,
        HistoricoAgentesService historico,
        CancellationToken cancelamento) =>
    {
        var resultado = await historico.ConsultarTrajetoDiarioAsync(
            id,
            data ?? DateOnly.FromDateTime(DateTime.UtcNow),
            cancelamento);
        return resultado is null ? Results.NotFound() : Results.Ok(resultado);
    })
    .WithName("ConsultarTrajetoDiarioAgente")
    .WithSummary("Consulta os registros de posição de um agente na data solicitada; não representa telemetria em tempo real.");

app.MapGet("/api/autenticacao/eu", (System.Security.Claims.ClaimsPrincipal usuario) =>
        Results.Ok(new { id = usuario.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value }))
    .RequireAuthorization()
    .WithSummary("Retorna a identidade da sessão autenticada.");

app.MapearAutenticacao();
app.MapControllers();
app.MapHub<EquipamentosHub>("/hubs/equipamentos").RequireAuthorization();

app.Run();

public partial class Program { }