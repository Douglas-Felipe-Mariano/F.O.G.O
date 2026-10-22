using Fogo.Api;
using Fogo.Api.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.AdicionarAutenticacaoFogo();
builder.Services.AdicionarProtecaoLogin();

builder.Services.AddOpenApi();

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
    .WithName("ConsultarSaude")
    .WithSummary("Verifica se a API está respondendo.");

app.MapGet("/api/autenticacao/eu", (System.Security.Claims.ClaimsPrincipal usuario) =>
        Results.Ok(new { id = usuario.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value }))
    .RequireAuthorization()
    .WithSummary("Retorna a identidade da sessão autenticada.");

app.MapearAutenticacao();

app.Run();

public partial class Program { }