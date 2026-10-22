using Fogo.Api.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Fogo.Tests;

public sealed class ConfiguracaoFogoWebApplicationFactory : WebApplicationFactory<Program>
{
    static ConfiguracaoFogoWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("FOGO_JWT_KEY", "teste-local-chave-jwt-com-mais-de-32-bytes");
        Environment.SetEnvironmentVariable("ConnectionStrings__PostgreSQL", "Host=localhost;Database=fogo_test;Username=test;Password=test");
        Environment.SetEnvironmentVariable("FOGO_ORIGINS", "http://localhost:5173");
        Environment.SetEnvironmentVariable("FOGO_HTTPS_DEV", "true");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}