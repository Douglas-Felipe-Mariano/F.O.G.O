using System.Text.Json;
using System.Text.Json.Serialization;
using Fogo.Api;
using Fogo.Api.Application.Inventario;
using Fogo.Api.Identity;
using Fogo.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
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