using System.Text.Json;
using System.Text.Json.Serialization;
using Fogo.Api;
using Fogo.Api.Application.Inventario;
using Fogo.Api.Identity;
using Fogo.Api.Repositories;
using Microsoft.EntityFrameworkCore;

const string argumentoProvisionarPrimeiroUsuario = "--provisionar-primeiro-usuario";
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
var argumentosHospedeiro = args
    .Where(argumento => argumento != argumentoProvisionarPrimeiroUsuario)
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
builder.Services.AddScoped<IEquipamentoRepository, EquipamentoRepository>();
builder.Services.AddScoped<EquipamentoService>();
builder.Services.AddScoped<ProvisionadorPrimeiroUsuario>();

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

if (provisionarPrimeiroUsuario)
{
    await using var escopo = app.Services.CreateAsyncScope();
    var provisionador = escopo.ServiceProvider.GetRequiredService<ProvisionadorPrimeiroUsuario>();
    Environment.ExitCode = await provisionador.ExecutarAsync(CancellationToken.None) ? 0 : 1;
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

app.MapGet("/api/autenticacao/eu", (System.Security.Claims.ClaimsPrincipal usuario) =>
        Results.Ok(new { id = usuario.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value }))
    .RequireAuthorization()
    .WithSummary("Retorna a identidade da sessão autenticada.");

app.MapearAutenticacao();
app.MapControllers();

app.Run();

public partial class Program { }