using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Fogo.Api.Identity;

public sealed class FogoDbContext(DbContextOptions<FogoDbContext> options)
    : IdentityDbContext<UsuarioFogo, IdentityRole, string>(options)
{
    public DbSet<SessaoAtualizacao> SessoesAtualizacao => Set<SessaoAtualizacao>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UsuarioFogo>(entidade =>
        {
            entidade.ToTable("usuarios");
            entidade.Property(usuario => usuario.Id).HasColumnName("id");
            entidade.Property(usuario => usuario.UserName).HasColumnName("nome_usuario");
            entidade.Property(usuario => usuario.NormalizedUserName).HasColumnName("nome_usuario_normalizado");
            entidade.Property(usuario => usuario.Email).HasColumnName("email");
            entidade.Property(usuario => usuario.NormalizedEmail).HasColumnName("email_normalizado");
            entidade.Property(usuario => usuario.EmailConfirmed).HasColumnName("email_confirmado");
            entidade.Property(usuario => usuario.PasswordHash).HasColumnName("hash_senha");
            entidade.Property(usuario => usuario.SecurityStamp).HasColumnName("carimbo_seguranca");
            entidade.Property(usuario => usuario.ConcurrencyStamp).HasColumnName("carimbo_concorrencia");
            entidade.Property(usuario => usuario.PhoneNumber).HasColumnName("telefone");
            entidade.Property(usuario => usuario.PhoneNumberConfirmed).HasColumnName("telefone_confirmado");
            entidade.Property(usuario => usuario.TwoFactorEnabled).HasColumnName("autenticacao_dois_fatores");
            entidade.Property(usuario => usuario.LockoutEnd).HasColumnName("bloqueio_ate");
            entidade.Property(usuario => usuario.LockoutEnabled).HasColumnName("bloqueio_habilitado");
            entidade.Property(usuario => usuario.AccessFailedCount).HasColumnName("tentativas_falhas");
        });

        builder.Entity<IdentityRole>(entidade =>
        {
            entidade.ToTable("perfis_acesso");
            entidade.Property(perfil => perfil.Id).HasColumnName("id");
            entidade.Property(perfil => perfil.Name).HasColumnName("nome");
            entidade.Property(perfil => perfil.NormalizedName).HasColumnName("nome_normalizado");
            entidade.Property(perfil => perfil.ConcurrencyStamp).HasColumnName("carimbo_concorrencia");
        });

        builder.Entity<IdentityUserRole<string>>(entidade =>
        {
            entidade.ToTable("usuarios_perfis_acesso");
            entidade.Property(relacao => relacao.UserId).HasColumnName("usuario_id");
            entidade.Property(relacao => relacao.RoleId).HasColumnName("perfil_acesso_id");
        });

        builder.Entity<IdentityUserClaim<string>>(entidade =>
        {
            entidade.ToTable("reivindicacoes_usuarios");
            entidade.Property(reivindicacao => reivindicacao.Id).HasColumnName("id");
            entidade.Property(reivindicacao => reivindicacao.UserId).HasColumnName("usuario_id");
            entidade.Property(reivindicacao => reivindicacao.ClaimType).HasColumnName("tipo_reivindicacao");
            entidade.Property(reivindicacao => reivindicacao.ClaimValue).HasColumnName("valor_reivindicacao");
        });

        builder.Entity<IdentityUserLogin<string>>(entidade =>
        {
            entidade.ToTable("logins_externos_usuarios");
            entidade.Property(login => login.LoginProvider).HasColumnName("provedor_login");
            entidade.Property(login => login.ProviderKey).HasColumnName("chave_provedor");
            entidade.Property(login => login.ProviderDisplayName).HasColumnName("nome_provedor");
            entidade.Property(login => login.UserId).HasColumnName("usuario_id");
        });

        builder.Entity<IdentityUserToken<string>>(entidade =>
        {
            entidade.ToTable("tokens_usuarios");
            entidade.Property(token => token.UserId).HasColumnName("usuario_id");
            entidade.Property(token => token.LoginProvider).HasColumnName("provedor_login");
            entidade.Property(token => token.Name).HasColumnName("nome");
            entidade.Property(token => token.Value).HasColumnName("valor");
        });

        builder.Entity<IdentityRoleClaim<string>>(entidade =>
        {
            entidade.ToTable("reivindicacoes_perfis_acesso");
            entidade.Property(reivindicacao => reivindicacao.Id).HasColumnName("id");
            entidade.Property(reivindicacao => reivindicacao.RoleId).HasColumnName("perfil_acesso_id");
            entidade.Property(reivindicacao => reivindicacao.ClaimType).HasColumnName("tipo_reivindicacao");
            entidade.Property(reivindicacao => reivindicacao.ClaimValue).HasColumnName("valor_reivindicacao");
        });

        builder.Entity<SessaoAtualizacao>(entidade =>
        {
            entidade.ToTable("sessoes_atualizacao");
            entidade.HasKey(sessao => sessao.Id);
            entidade.Property(sessao => sessao.Id).HasColumnName("id");
            entidade.Property(sessao => sessao.UsuarioId).HasColumnName("usuario_id").IsRequired();
            entidade.Property(sessao => sessao.HashToken).HasColumnName("hash_token").HasMaxLength(64).IsRequired();
            entidade.Property(sessao => sessao.CriadaEm).HasColumnName("criada_em");
            entidade.Property(sessao => sessao.ExpiraEm).HasColumnName("expira_em");
            entidade.Property(sessao => sessao.RevogadaEm).HasColumnName("revogada_em");
            entidade.Property(sessao => sessao.SubstituidaPorHash).HasColumnName("substituida_por_hash").HasMaxLength(64);
            entidade.HasIndex(sessao => sessao.HashToken).IsUnique();
            entidade.HasIndex(sessao => sessao.UsuarioId);
            entidade.HasOne<UsuarioFogo>()
                .WithMany()
                .HasForeignKey(sessao => sessao.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
