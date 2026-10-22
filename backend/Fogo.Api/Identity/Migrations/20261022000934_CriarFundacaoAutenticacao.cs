using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fogo.Api.Identity.Migrations
{
    /// <inheritdoc />
    public partial class CriarFundacaoAutenticacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "perfis_acesso",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    nome = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    nome_normalizado = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    carimbo_concorrencia = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_perfis_acesso", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    nome_usuario = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    nome_usuario_normalizado = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_normalizado = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmado = table.Column<bool>(type: "boolean", nullable: false),
                    hash_senha = table.Column<string>(type: "text", nullable: true),
                    carimbo_seguranca = table.Column<string>(type: "text", nullable: true),
                    carimbo_concorrencia = table.Column<string>(type: "text", nullable: true),
                    telefone = table.Column<string>(type: "text", nullable: true),
                    telefone_confirmado = table.Column<bool>(type: "boolean", nullable: false),
                    autenticacao_dois_fatores = table.Column<bool>(type: "boolean", nullable: false),
                    bloqueio_ate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    bloqueio_habilitado = table.Column<bool>(type: "boolean", nullable: false),
                    tentativas_falhas = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reivindicacoes_perfis_acesso",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    perfil_acesso_id = table.Column<string>(type: "text", nullable: false),
                    tipo_reivindicacao = table.Column<string>(type: "text", nullable: true),
                    valor_reivindicacao = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reivindicacoes_perfis_acesso", x => x.id);
                    table.ForeignKey(
                        name: "FK_reivindicacoes_perfis_acesso_perfis_acesso_perfil_acesso_id",
                        column: x => x.perfil_acesso_id,
                        principalTable: "perfis_acesso",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "logins_externos_usuarios",
                columns: table => new
                {
                    provedor_login = table.Column<string>(type: "text", nullable: false),
                    chave_provedor = table.Column<string>(type: "text", nullable: false),
                    nome_provedor = table.Column<string>(type: "text", nullable: true),
                    usuario_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logins_externos_usuarios", x => new { x.provedor_login, x.chave_provedor });
                    table.ForeignKey(
                        name: "FK_logins_externos_usuarios_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reivindicacoes_usuarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<string>(type: "text", nullable: false),
                    tipo_reivindicacao = table.Column<string>(type: "text", nullable: true),
                    valor_reivindicacao = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reivindicacoes_usuarios", x => x.id);
                    table.ForeignKey(
                        name: "FK_reivindicacoes_usuarios_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sessoes_atualizacao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<string>(type: "text", nullable: false),
                    hash_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expira_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revogada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    substituida_por_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessoes_atualizacao", x => x.id);
                    table.ForeignKey(
                        name: "FK_sessoes_atualizacao_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tokens_usuarios",
                columns: table => new
                {
                    usuario_id = table.Column<string>(type: "text", nullable: false),
                    provedor_login = table.Column<string>(type: "text", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false),
                    valor = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tokens_usuarios", x => new { x.usuario_id, x.provedor_login, x.nome });
                    table.ForeignKey(
                        name: "FK_tokens_usuarios_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuarios_perfis_acesso",
                columns: table => new
                {
                    usuario_id = table.Column<string>(type: "text", nullable: false),
                    perfil_acesso_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios_perfis_acesso", x => new { x.usuario_id, x.perfil_acesso_id });
                    table.ForeignKey(
                        name: "FK_usuarios_perfis_acesso_perfis_acesso_perfil_acesso_id",
                        column: x => x.perfil_acesso_id,
                        principalTable: "perfis_acesso",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_usuarios_perfis_acesso_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_logins_externos_usuarios_usuario_id",
                table: "logins_externos_usuarios",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "perfis_acesso",
                column: "nome_normalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reivindicacoes_perfis_acesso_perfil_acesso_id",
                table: "reivindicacoes_perfis_acesso",
                column: "perfil_acesso_id");

            migrationBuilder.CreateIndex(
                name: "IX_reivindicacoes_usuarios_usuario_id",
                table: "reivindicacoes_usuarios",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_sessoes_atualizacao_hash_token",
                table: "sessoes_atualizacao",
                column: "hash_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sessoes_atualizacao_usuario_id",
                table: "sessoes_atualizacao",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "usuarios",
                column: "email_normalizado");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "usuarios",
                column: "nome_usuario_normalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_perfis_acesso_perfil_acesso_id",
                table: "usuarios_perfis_acesso",
                column: "perfil_acesso_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "logins_externos_usuarios");

            migrationBuilder.DropTable(
                name: "reivindicacoes_perfis_acesso");

            migrationBuilder.DropTable(
                name: "reivindicacoes_usuarios");

            migrationBuilder.DropTable(
                name: "sessoes_atualizacao");

            migrationBuilder.DropTable(
                name: "tokens_usuarios");

            migrationBuilder.DropTable(
                name: "usuarios_perfis_acesso");

            migrationBuilder.DropTable(
                name: "perfis_acesso");

            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}
