using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fogo.Api.Identity.Migrations
{
    /// <inheritdoc />
    public partial class CriarInventarioEquipamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "equipamentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    numero_serie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    marca = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    modelo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    cep = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    bairro = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    cidade = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    rua = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    referencia = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    situacao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    necessita_manutencao = table.Column<bool>(type: "boolean", nullable: false),
                    tipo_manutencao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    proxima_manutencao = table.Column<DateOnly>(type: "date", nullable: true),
                    origem_dados = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipamentos", x => x.id);
                    table.CheckConstraint("CK_equipamentos_coordenadas_completas", "(latitude IS NULL AND longitude IS NULL) OR (latitude IS NOT NULL AND longitude IS NOT NULL)");
                    table.CheckConstraint("CK_equipamentos_latitude", "latitude IS NULL OR latitude BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_equipamentos_longitude", "longitude IS NULL OR longitude BETWEEN -180 AND 180");
                    table.CheckConstraint("CK_equipamentos_manutencao", "(necessita_manutencao = FALSE AND tipo_manutencao IS NULL AND proxima_manutencao IS NULL) OR (necessita_manutencao = TRUE AND ((tipo_manutencao = 'Imediata' AND proxima_manutencao IS NULL) OR (tipo_manutencao IN ('Agendada', 'Periodica') AND proxima_manutencao IS NOT NULL)))");
                });

            migrationBuilder.CreateIndex(
                name: "IX_equipamentos_bairro",
                table: "equipamentos",
                column: "bairro");

            migrationBuilder.CreateIndex(
                name: "IX_equipamentos_numero_serie",
                table: "equipamentos",
                column: "numero_serie");

            migrationBuilder.CreateIndex(
                name: "IX_equipamentos_situacao",
                table: "equipamentos",
                column: "situacao");

            migrationBuilder.CreateIndex(
                name: "IX_equipamentos_tipo",
                table: "equipamentos",
                column: "tipo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "equipamentos");
        }
    }
}
