using System.Text;
using System.Threading.RateLimiting;
using Fogo.Api.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Fogo.Api;

public static class ConfiguracaoAutenticacao
{
    public static WebApplicationBuilder AdicionarAutenticacaoFogo(this WebApplicationBuilder builder)
    {
        var chaveJwt = builder.Configuration["FOGO_JWT_KEY"];
        if (string.IsNullOrWhiteSpace(chaveJwt) || Encoding.UTF8.GetByteCount(chaveJwt) < 32)
        {
            throw new InvalidOperationException("Configure FOGO_JWT_KEY com pelo menos 32 bytes por uma fonte segura de configuração.");
        }

        var conexaoBanco = builder.Configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:PostgreSQL antes de iniciar a API.");
        var opcoesToken = new OpcoesToken
        {
            Emissor = builder.Configuration["FOGO_JWT_ISSUER"] ?? "Fogo.Api",
            Audiencia = builder.Configuration["FOGO_JWT_AUDIENCE"] ?? "Fogo.Frontend",
            ChaveAssinatura = chaveJwt,
            CookiesSeguros = !builder.Environment.IsDevelopment()
                || builder.Configuration.GetValue<bool>("FOGO_HTTPS_DEV")
        };
        var origensConfiguradas = builder.Configuration["FOGO_ORIGINS"];
        if (string.IsNullOrWhiteSpace(origensConfiguradas))
        {
            if (!builder.Environment.IsDevelopment())
            {
                throw new InvalidOperationException("Configure FOGO_ORIGINS com as origens permitidas fora do ambiente de desenvolvimento.");
            }

            origensConfiguradas = "http://localhost:5173";
        }

        var origensInformadas = origensConfiguradas.Split(';', StringSplitOptions.TrimEntries);
        if (origensInformadas.Any(origem => !TentarNormalizarOrigemHttp(origem, out _)))
        {
            throw new InvalidOperationException("FOGO_ORIGINS deve conter somente origens HTTP/HTTPS válidas, sem caminho, consulta ou fragmento, separadas por ponto e vírgula.");
        }

        var origensPermitidas = origensInformadas
            .Select(origem => TentarNormalizarOrigemHttp(origem, out var normalizada) ? normalizada : origem)
            .ToArray();

        builder.Services.AddSingleton(opcoesToken);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IReadOnlyCollection<string>>(origensPermitidas);
        builder.Services.AddDbContext<FogoDbContext>(opcoes => opcoes.UseNpgsql(conexaoBanco));
        builder.Services.AddIdentity<UsuarioFogo, IdentityRole>(opcoes =>
            {
                opcoes.SignIn.RequireConfirmedEmail = true;
                opcoes.Password.RequiredLength = 12;
                opcoes.Password.RequireDigit = true;
                opcoes.Password.RequireLowercase = true;
                opcoes.Password.RequireUppercase = true;
                opcoes.Password.RequireNonAlphanumeric = true;
                opcoes.Lockout.AllowedForNewUsers = true;
                opcoes.Lockout.MaxFailedAccessAttempts = 5;
                opcoes.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                opcoes.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<FogoDbContext>()
            .AddDefaultTokenProviders();
        var cookiesSeguros = opcoesToken.CookiesSeguros;
        builder.Services.ConfigureApplicationCookie(opcoes =>
        {
            opcoes.Cookie.Name = "fogo.identidade";
            opcoes.Cookie.HttpOnly = true;
            opcoes.Cookie.SecurePolicy = cookiesSeguros
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            opcoes.Cookie.SameSite = SameSiteMode.Strict;
        });
        builder.Services.AddAuthentication(opcoes =>
            {
                opcoes.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opcoes.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                opcoes.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(opcoes =>
            {
                opcoes.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = opcoesToken.Emissor,
                    ValidateAudience = true,
                    ValidAudience = opcoesToken.Audiencia,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chaveJwt)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                opcoes.Events = new JwtBearerEvents
                {
                    OnMessageReceived = contexto =>
                    {
                        var tokenAcesso = contexto.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(tokenAcesso)
                            && contexto.HttpContext.Request.Path.StartsWithSegments("/hubs/equipamentos"))
                        {
                            contexto.Token = tokenAcesso;
                        }

                        return Task.CompletedTask;
                    }
                };
            });
        builder.Services.AddAuthorization();
        builder.Services.AddScoped<ServicoTokens>();
        builder.Services.AddCors(opcoes => opcoes.AddDefaultPolicy(politica =>
            politica.WithOrigins(origensPermitidas)
                .AllowCredentials()
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                .WithHeaders("Content-Type", "Authorization", ProtecaoRequisicao.CabecalhoCsrf)));

        return builder;
    }

    private static bool TentarNormalizarOrigemHttp(string origem, out string normalizada)
    {
        if (Uri.TryCreate(origem, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrEmpty(uri.Host)
            && string.IsNullOrEmpty(uri.UserInfo)
            && uri.AbsolutePath == "/"
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment))
        {
            normalizada = uri.GetLeftPart(UriPartial.Authority);
            return true;
        }

        normalizada = string.Empty;
        return false;
    }

    public static IServiceCollection AdicionarProtecaoLogin(this IServiceCollection servicos)
    {
        servicos.AddRateLimiter(opcoes =>
        {
            opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opcoes.AddPolicy("login", contexto => RateLimitPartition.GetFixedWindowLimiter(
                contexto.Connection.RemoteIpAddress?.ToString() ?? "origem-desconhecida",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
        });
        return servicos;
    }
}