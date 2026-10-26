using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fogo.Api.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCadastroAgentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agentes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    categoria_profissional = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    base_operacional = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    situacao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    origem_dados = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agentes", x => x.id);
                    table.CheckConstraint("CK_agentes_coordenadas_completas", "(latitude IS NULL AND longitude IS NULL) OR (latitude IS NOT NULL AND longitude IS NOT NULL)");
                    table.CheckConstraint("CK_agentes_latitude", "latitude IS NULL OR latitude BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_agentes_longitude", "longitude IS NULL OR longitude BETWEEN -180 AND 180");
                });

            migrationBuilder.CreateIndex(
                name: "IX_agentes_categoria_profissional",
                table: "agentes",
                column: "categoria_profissional");

            migrationBuilder.CreateIndex(
                name: "IX_agentes_nome",
                table: "agentes",
                column: "nome");

            migrationBuilder.CreateIndex(
                name: "IX_agentes_situacao",
                table: "agentes",
                column: "situacao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agentes");
        }
    }
}
