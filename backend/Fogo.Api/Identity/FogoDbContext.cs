using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Fogo.Api.Inventario;
using Fogo.Api.Operacoes;

namespace Fogo.Api.Identity;

public sealed class FogoDbContext(DbContextOptions<FogoDbContext> options)
    : IdentityDbContext<UsuarioFogo, IdentityRole, string>(options)
{
    public DbSet<SessaoAtualizacao> SessoesAtualizacao => Set<SessaoAtualizacao>();
    public DbSet<Equipamento> Equipamentos => Set<Equipamento>();
    public DbSet<Agente> Agentes => Set<Agente>();
    public DbSet<HistoricoLocalizacaoAgente> HistoricoLocalizacoesAgentes => Set<HistoricoLocalizacaoAgente>();
    public DbSet<CameraPublica> CamerasPublicas => Set<CameraPublica>();
    public DbSet<SegmentoTransito> SegmentosTransito => Set<SegmentoTransito>();
    public DbSet<ObservacaoTransito> ObservacoesTransito => Set<ObservacaoTransito>();
    public DbSet<OcorrenciaAcidente> OcorrenciasAcidente => Set<OcorrenciaAcidente>();

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

        builder.Entity<Equipamento>(entidade =>
        {
            entidade.ToTable("equipamentos", tabela =>
            {
                tabela.HasCheckConstraint(
                    "CK_equipamentos_coordenadas_completas",
                    "(latitude IS NULL AND longitude IS NULL) OR (latitude IS NOT NULL AND longitude IS NOT NULL)");
                tabela.HasCheckConstraint("CK_equipamentos_latitude", "latitude IS NULL OR latitude BETWEEN -90 AND 90");
                tabela.HasCheckConstraint("CK_equipamentos_longitude", "longitude IS NULL OR longitude BETWEEN -180 AND 180");
                tabela.HasCheckConstraint("CK_equipamentos_manutencao", "(necessita_manutencao = FALSE AND tipo_manutencao IS NULL AND proxima_manutencao IS NULL) OR (necessita_manutencao = TRUE AND ((tipo_manutencao = 'Imediata' AND proxima_manutencao IS NULL) OR (tipo_manutencao IN ('Agendada', 'Periodica') AND proxima_manutencao IS NOT NULL)))");
            });
            entidade.HasKey(equipamento => equipamento.Id);
            entidade.Property(equipamento => equipamento.Id).HasColumnName("id");
            entidade.Property(equipamento => equipamento.Tipo).HasColumnName("tipo").HasMaxLength(60).IsRequired();
            entidade.Property(equipamento => equipamento.NumeroSerie).HasColumnName("numero_serie").HasMaxLength(100).IsRequired();
            entidade.Property(equipamento => equipamento.Marca).HasColumnName("marca").HasMaxLength(100);
            entidade.Property(equipamento => equipamento.Modelo).HasColumnName("modelo").HasMaxLength(120);
            var conversorCaracteristicas = new ValueConverter<Dictionary<string, string>, string>(
                valor => SerializarCaracteristicas(valor),
                json => DesserializarCaracteristicas(json));
            var comparadorCaracteristicas = new ValueComparer<Dictionary<string, string>>(
                (esquerda, direita) => CaracteristicasIguais(esquerda, direita),
                valor => HashCaracteristicas(valor),
                valor => CopiarCaracteristicas(valor));
            entidade.Property(equipamento => equipamento.Caracteristicas)
                .HasColumnName("caracteristicas")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb")
                .HasConversion(conversorCaracteristicas)
                .Metadata.SetValueComparer(comparadorCaracteristicas);
            entidade.Property(equipamento => equipamento.Cep).HasColumnName("cep").HasMaxLength(20).IsRequired();
            entidade.Property(equipamento => equipamento.Bairro).HasColumnName("bairro").HasMaxLength(120).IsRequired();
            entidade.Property(equipamento => equipamento.Cidade).HasColumnName("cidade").HasMaxLength(120).IsRequired();
            entidade.Property(equipamento => equipamento.Uf).HasColumnName("uf").HasMaxLength(2).IsRequired();
            entidade.Property(equipamento => equipamento.Rua).HasColumnName("rua").HasMaxLength(180).IsRequired();
            entidade.Property(equipamento => equipamento.Referencia).HasColumnName("referencia").HasMaxLength(240);
            entidade.Property(equipamento => equipamento.Latitude).HasColumnName("latitude").HasPrecision(9, 6);
            entidade.Property(equipamento => equipamento.Longitude).HasColumnName("longitude").HasPrecision(9, 6);
            entidade.Property(equipamento => equipamento.Situacao).HasColumnName("situacao").HasConversion<string>().HasMaxLength(40).IsRequired();
            entidade.Property(equipamento => equipamento.NecessitaManutencao).HasColumnName("necessita_manutencao").IsRequired();
            entidade.Property(equipamento => equipamento.TipoManutencao).HasColumnName("tipo_manutencao").HasConversion<string>().HasMaxLength(30);
            entidade.Property(equipamento => equipamento.ProximaManutencao).HasColumnName("proxima_manutencao");
            entidade.Property(equipamento => equipamento.OrigemDados).HasColumnName("origem_dados").HasConversion<string>().HasMaxLength(20).IsRequired();
            entidade.Property(equipamento => equipamento.CriadoEm).HasColumnName("criado_em").IsRequired();
            entidade.Property(equipamento => equipamento.AtualizadoEm).HasColumnName("atualizado_em").IsRequired();
            entidade.HasIndex(equipamento => equipamento.NumeroSerie);
            entidade.HasIndex(equipamento => equipamento.Tipo);
            entidade.HasIndex(equipamento => equipamento.Situacao);
            entidade.HasIndex(equipamento => equipamento.Bairro);
        });

        builder.Entity<Agente>(entidade =>
        {
            entidade.ToTable("agentes", tabela =>
            {
                tabela.HasCheckConstraint(
                    "CK_agentes_coordenadas_completas",
                    "(latitude IS NULL AND longitude IS NULL) OR (latitude IS NOT NULL AND longitude IS NOT NULL)");
                tabela.HasCheckConstraint("CK_agentes_latitude", "latitude IS NULL OR latitude BETWEEN -90 AND 90");
                tabela.HasCheckConstraint("CK_agentes_longitude", "longitude IS NULL OR longitude BETWEEN -180 AND 180");
            });
            entidade.HasKey(agente => agente.Id);
            entidade.Property(agente => agente.Id).HasColumnName("id");
            entidade.Property(agente => agente.Nome).HasColumnName("nome").HasMaxLength(160).IsRequired();
            entidade.Property(agente => agente.CategoriaProfissional).HasColumnName("categoria_profissional").HasMaxLength(120).IsRequired();
            entidade.Property(agente => agente.BaseOperacional).HasColumnName("base_operacional").HasMaxLength(240).IsRequired();
            entidade.Property(agente => agente.Latitude).HasColumnName("latitude").HasPrecision(9, 6);
            entidade.Property(agente => agente.Longitude).HasColumnName("longitude").HasPrecision(9, 6);
            entidade.Property(agente => agente.Situacao).HasColumnName("situacao").HasConversion<string>().HasMaxLength(30).IsRequired();
            entidade.Property(agente => agente.OrigemDados).HasColumnName("origem_dados").HasConversion<string>().HasMaxLength(20).IsRequired();
            entidade.Property(agente => agente.CriadoEm).HasColumnName("criado_em").IsRequired();
            entidade.Property(agente => agente.AtualizadoEm).HasColumnName("atualizado_em").IsRequired();
            entidade.HasIndex(agente => agente.CategoriaProfissional);
            entidade.HasIndex(agente => agente.Situacao);
            entidade.HasIndex(agente => agente.Nome);
        });

        builder.Entity<HistoricoLocalizacaoAgente>(entidade =>
        {
            entidade.ToTable("historico_localizacoes_agentes", tabela =>
            {
                tabela.HasCheckConstraint("CK_historico_localizacoes_agentes_latitude", "latitude BETWEEN -90 AND 90");
                tabela.HasCheckConstraint("CK_historico_localizacoes_agentes_longitude", "longitude BETWEEN -180 AND 180");
            });
            entidade.HasKey(localizacao => localizacao.Id);
            entidade.Property(localizacao => localizacao.Id).HasColumnName("id");
            entidade.Property(localizacao => localizacao.AgenteId).HasColumnName("agente_id").IsRequired();
            entidade.Property(localizacao => localizacao.Latitude).HasColumnName("latitude").HasPrecision(9, 6).IsRequired();
            entidade.Property(localizacao => localizacao.Longitude).HasColumnName("longitude").HasPrecision(9, 6).IsRequired();
            entidade.Property(localizacao => localizacao.RegistradaEm).HasColumnName("registrada_em").IsRequired();
            entidade.HasIndex(localizacao => new { localizacao.AgenteId, localizacao.RegistradaEm });
            entidade.HasOne<Agente>().WithMany().HasForeignKey(localizacao => localizacao.AgenteId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CameraPublica>(entidade =>
        {
            entidade.ToTable("cameras_publicas", tabela =>
            {
                tabela.HasCheckConstraint("CK_cameras_publicas_coordenadas_completas", "(latitude IS NULL AND longitude IS NULL) OR (latitude IS NOT NULL AND longitude IS NOT NULL)");
                tabela.HasCheckConstraint("CK_cameras_publicas_latitude", "latitude IS NULL OR latitude BETWEEN -90 AND 90");
                tabela.HasCheckConstraint("CK_cameras_publicas_longitude", "longitude IS NULL OR longitude BETWEEN -180 AND 180");
                tabela.HasCheckConstraint("CK_cameras_publicas_stream_https", "url_stream LIKE 'https://%'");
            });
            entidade.HasKey(camera => camera.Id);
            entidade.Property(camera => camera.Id).HasColumnName("id");
            entidade.Property(camera => camera.Cidade).HasColumnName("cidade").HasMaxLength(120).IsRequired();
            entidade.Property(camera => camera.Nome).HasColumnName("nome").HasMaxLength(180).IsRequired();
            entidade.Property(camera => camera.UrlStream).HasColumnName("url_stream").HasMaxLength(600).IsRequired();
            entidade.Property(camera => camera.Endereco).HasColumnName("endereco").HasMaxLength(240);
            entidade.Property(camera => camera.Latitude).HasColumnName("latitude").HasPrecision(9, 6);
            entidade.Property(camera => camera.Longitude).HasColumnName("longitude").HasPrecision(9, 6);
            entidade.HasIndex(camera => new { camera.Cidade, camera.Nome }).IsUnique();
            entidade.HasIndex(camera => new { camera.Latitude, camera.Longitude });
        });

        builder.Entity<SegmentoTransito>(entidade =>
        {
            entidade.ToTable("segmentos_transito", tabela =>
            {
                tabela.HasCheckConstraint("CK_segmentos_transito_latitude", "latitude BETWEEN -90 AND 90");
                tabela.HasCheckConstraint("CK_segmentos_transito_longitude", "longitude BETWEEN -180 AND 180");
            });
            entidade.HasKey(segmento => segmento.Id);
            entidade.Property(segmento => segmento.Id).HasColumnName("id");
            entidade.Property(segmento => segmento.Nome).HasColumnName("nome").HasMaxLength(180).IsRequired();
            entidade.Property(segmento => segmento.Cidade).HasColumnName("cidade").HasMaxLength(120).IsRequired();
            entidade.Property(segmento => segmento.ReferenciaMapa).HasColumnName("referencia_mapa").HasMaxLength(240).IsRequired();
            entidade.Property(segmento => segmento.Latitude).HasColumnName("latitude").HasPrecision(9, 6).IsRequired();
            entidade.Property(segmento => segmento.Longitude).HasColumnName("longitude").HasPrecision(9, 6).IsRequired();
            entidade.Property(segmento => segmento.OrigemDados).HasColumnName("origem_dados").HasConversion<string>().HasMaxLength(20).IsRequired();
            entidade.HasIndex(segmento => segmento.Cidade);
        });

        builder.Entity<ObservacaoTransito>(entidade =>
        {
            entidade.ToTable("observacoes_transito", tabela =>
            {
                tabela.HasCheckConstraint("CK_observacoes_transito_volume", "veiculos_ultima_hora >= 0");
                tabela.HasCheckConstraint("CK_observacoes_transito_velocidade", "velocidade_media_kmh >= 0");
                tabela.HasCheckConstraint("CK_observacoes_transito_congestionamento", "congestionamento_percentual BETWEEN 0 AND 100");
            });
            entidade.HasKey(observacao => observacao.Id);
            entidade.Property(observacao => observacao.Id).HasColumnName("id");
            entidade.Property(observacao => observacao.SegmentoId).HasColumnName("segmento_id").IsRequired();
            entidade.Property(observacao => observacao.RegistradaEm).HasColumnName("registrada_em").IsRequired();
            entidade.Property(observacao => observacao.VeiculosUltimaHora).HasColumnName("veiculos_ultima_hora").IsRequired();
            entidade.Property(observacao => observacao.VelocidadeMediaKmh).HasColumnName("velocidade_media_kmh").HasPrecision(6, 2).IsRequired();
            entidade.Property(observacao => observacao.CongestionamentoPercentual).HasColumnName("congestionamento_percentual").IsRequired();
            entidade.Property(observacao => observacao.OrigemDados).HasColumnName("origem_dados").HasConversion<string>().HasMaxLength(20).IsRequired();
            entidade.HasIndex(observacao => new { observacao.SegmentoId, observacao.RegistradaEm }).IsUnique();
            entidade.HasOne<SegmentoTransito>().WithMany().HasForeignKey(observacao => observacao.SegmentoId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OcorrenciaAcidente>(entidade =>
        {
            entidade.ToTable("ocorrencias_acidentes", tabela =>
            {
                tabela.HasCheckConstraint("CK_ocorrencias_acidentes_latitude", "latitude BETWEEN -90 AND 90");
                tabela.HasCheckConstraint("CK_ocorrencias_acidentes_longitude", "longitude BETWEEN -180 AND 180");
            });
            entidade.HasKey(acidente => acidente.Id);
            entidade.Property(acidente => acidente.Id).HasColumnName("id");
            entidade.Property(acidente => acidente.LocalDescricao).HasColumnName("local_descricao").HasMaxLength(240).IsRequired();
            entidade.Property(acidente => acidente.Cidade).HasColumnName("cidade").HasMaxLength(120).IsRequired();
            entidade.Property(acidente => acidente.Latitude).HasColumnName("latitude").HasPrecision(9, 6).IsRequired();
            entidade.Property(acidente => acidente.Longitude).HasColumnName("longitude").HasPrecision(9, 6).IsRequired();
            entidade.Property(acidente => acidente.Grau).HasColumnName("grau").HasConversion<string>().HasMaxLength(20).IsRequired();
            entidade.Property(acidente => acidente.ImpactoTransito).HasColumnName("impacto_transito").HasConversion<string>().HasMaxLength(30).IsRequired();
            entidade.Property(acidente => acidente.HaFeridos).HasColumnName("ha_feridos").IsRequired();
            entidade.Property(acidente => acidente.Situacao).HasColumnName("situacao").HasConversion<string>().HasMaxLength(30).IsRequired();
            entidade.Property(acidente => acidente.RegistradoEm).HasColumnName("registrado_em").IsRequired();
            entidade.Property(acidente => acidente.DespachoSolicitadoEm).HasColumnName("despacho_solicitado_em");
            entidade.Property(acidente => acidente.AgenteDesignadoId).HasColumnName("agente_designado_id");
            entidade.Property(acidente => acidente.SocorroSolicitadoEm).HasColumnName("socorro_solicitado_em");
            entidade.HasIndex(acidente => new { acidente.Situacao, acidente.RegistradoEm });
            entidade.HasOne<Agente>().WithMany().HasForeignKey(acidente => acidente.AgenteDesignadoId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static string SerializarCaracteristicas(Dictionary<string, string> valor) =>
        System.Text.Json.JsonSerializer.Serialize(valor);

    private static Dictionary<string, string> DesserializarCaracteristicas(string json) =>
        System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();

    private static bool CaracteristicasIguais(
        Dictionary<string, string>? esquerda,
        Dictionary<string, string>? direita) =>
        esquerda is not null
        && direita is not null
        && esquerda.Count == direita.Count
        && esquerda.All(item => direita.TryGetValue(item.Key, out var valor) && valor == item.Value);

    private static int HashCaracteristicas(Dictionary<string, string> valor) =>
        valor.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.Key, item.Value));

    private static Dictionary<string, string> CopiarCaracteristicas(Dictionary<string, string> valor) =>
        new(valor);
}
