using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fogo.Api.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarTransitoEAcidentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ocorrencias_acidentes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    local_descricao = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    cidade = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    grau = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    impacto_transito = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ha_feridos = table.Column<bool>(type: "boolean", nullable: false),
                    situacao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    registrado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    despacho_solicitado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    agente_designado_id = table.Column<Guid>(type: "uuid", nullable: true),
                    socorro_solicitado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ocorrencias_acidentes", x => x.id);
                    table.CheckConstraint("CK_ocorrencias_acidentes_latitude", "latitude BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_ocorrencias_acidentes_longitude", "longitude BETWEEN -180 AND 180");
                    table.ForeignKey(
                        name: "FK_ocorrencias_acidentes_agentes_agente_designado_id",
                        column: x => x.agente_designado_id,
                        principalTable: "agentes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "segmentos_transito",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    cidade = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    referencia_mapa = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    origem_dados = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_segmentos_transito", x => x.id);
                    table.CheckConstraint("CK_segmentos_transito_latitude", "latitude BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_segmentos_transito_longitude", "longitude BETWEEN -180 AND 180");
                });

            migrationBuilder.CreateTable(
                name: "observacoes_transito",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    segmento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registrada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    veiculos_ultima_hora = table.Column<int>(type: "integer", nullable: false),
                    velocidade_media_kmh = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    congestionamento_percentual = table.Column<int>(type: "integer", nullable: false),
                    origem_dados = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_observacoes_transito", x => x.id);
                    table.CheckConstraint("CK_observacoes_transito_congestionamento", "congestionamento_percentual BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_observacoes_transito_velocidade", "velocidade_media_kmh >= 0");
                    table.CheckConstraint("CK_observacoes_transito_volume", "veiculos_ultima_hora >= 0");
                    table.ForeignKey(
                        name: "FK_observacoes_transito_segmentos_transito_segmento_id",
                        column: x => x.segmento_id,
                        principalTable: "segmentos_transito",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_observacoes_transito_segmento_id_registrada_em",
                table: "observacoes_transito",
                columns: new[] { "segmento_id", "registrada_em" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencias_acidentes_agente_designado_id",
                table: "ocorrencias_acidentes",
                column: "agente_designado_id");

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencias_acidentes_situacao_registrado_em",
                table: "ocorrencias_acidentes",
                columns: new[] { "situacao", "registrado_em" });

            migrationBuilder.CreateIndex(
                name: "IX_segmentos_transito_cidade",
                table: "segmentos_transito",
                column: "cidade");

            // Histórico sintético para demonstrar a visualização e a heurística preditiva.
            // As coordenadas são referências de centro municipal (OSM), não sensores ou trechos monitorados.
            migrationBuilder.Sql(
                """
                INSERT INTO segmentos_transito (id, nome, cidade, referencia_mapa, latitude, longitude, origem_dados)
                VALUES
                    ('cafe0000-0000-4000-8000-000000000001', 'Referência demonstrativa · Tupã', 'Tupã', 'Centro municipal aproximado · não representa um sensor ou trecho viário', -21.934913, -50.514126, 'Simulado'),
                    ('cafe0000-0000-4000-8000-000000000002', 'Referência demonstrativa · Bastos', 'Bastos', 'Centro municipal aproximado · não representa um sensor ou trecho viário', -21.922247, -50.731773, 'Simulado'),
                    ('cafe0000-0000-4000-8000-000000000003', 'Referência demonstrativa · Herculândia', 'Herculândia', 'Centro municipal aproximado · não representa um sensor ou trecho viário', -22.004508, -50.386275, 'Simulado'),
                    ('cafe0000-0000-4000-8000-000000000004', 'Referência demonstrativa · Quatá', 'Quatá', 'Centro municipal aproximado · não representa um sensor ou trecho viário', -22.247098, -50.700362, 'Simulado'),
                    ('cafe0000-0000-4000-8000-000000000005', 'Referência demonstrativa · Paraguaçu Paulista', 'Paraguaçu Paulista', 'Centro municipal aproximado · não representa um sensor ou trecho viário', -22.420350, -50.579210, 'Simulado');

                WITH faixa_horaria AS (
                    SELECT instante,
                           (instante AT TIME ZONE 'America/Sao_Paulo') AS horario_local,
                           EXTRACT(DOW FROM instante AT TIME ZONE 'America/Sao_Paulo')::integer AS dia_semana
                    FROM generate_series(
                        date_trunc('hour', now() - interval '28 days'),
                        date_trunc('hour', now()),
                        interval '1 hour') AS serie(instante)
                ),
                segmentos AS (
                    SELECT id, cidade,
                           row_number() OVER (ORDER BY id)::integer AS variacao
                    FROM segmentos_transito
                    WHERE id BETWEEN 'cafe0000-0000-4000-8000-000000000001'::uuid
                                 AND 'cafe0000-0000-4000-8000-000000000005'::uuid
                ),
                valores AS (
                    SELECT segmento.id AS segmento_id,
                           faixa.instante,
                           faixa.horario_local,
                           faixa.dia_semana,
                           segmento.variacao,
                           CASE
                               WHEN EXTRACT(HOUR FROM faixa.horario_local)::integer BETWEEN 7 AND 9
                                 OR EXTRACT(HOUR FROM faixa.horario_local)::integer BETWEEN 16 AND 19
                               THEN LEAST(96, 58 + segmento.variacao * 4 + ((faixa.dia_semana + EXTRACT(HOUR FROM faixa.horario_local)::integer) % 17))
                               ELSE 12 + ((EXTRACT(HOUR FROM faixa.horario_local)::integer * 7 + faixa.dia_semana * 11 + segmento.variacao * 13) % 38)
                           END::integer AS congestionamento
                    FROM segmentos AS segmento
                    CROSS JOIN faixa_horaria AS faixa
                )
                INSERT INTO observacoes_transito
                    (id, segmento_id, registrada_em, veiculos_ultima_hora, velocidade_media_kmh, congestionamento_percentual, origem_dados)
                SELECT md5(valores.segmento_id::text || '|' || valores.instante::text)::uuid,
                       valores.segmento_id,
                       valores.instante,
                       65 + valores.variacao * 18
                           + CASE
                               WHEN EXTRACT(HOUR FROM valores.horario_local)::integer BETWEEN 7 AND 9 THEN 145
                               WHEN EXTRACT(HOUR FROM valores.horario_local)::integer BETWEEN 16 AND 19 THEN 185
                               ELSE ((EXTRACT(HOUR FROM valores.horario_local)::integer * 13 + valores.dia_semana * 17 + valores.variacao * 23) % 100)::integer
                             END,
                       GREATEST(12, 72 - valores.congestionamento * 0.55)::numeric(6,2),
                       valores.congestionamento,
                       'Simulado'
                FROM valores;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "observacoes_transito");

            migrationBuilder.DropTable(
                name: "ocorrencias_acidentes");

            migrationBuilder.DropTable(
                name: "segmentos_transito");
        }
    }
}
