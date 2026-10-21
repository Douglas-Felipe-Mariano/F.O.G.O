var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/saude", () => Results.Ok(new { estado = "disponivel" }))
    .WithName("ConsultarSaude")
    .WithSummary("Verifica se a API está respondendo.");

app.Run();

public partial class Program { }