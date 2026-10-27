using Fogo.Api.Identity;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fogo.Api.Identity.Migrations;

[DbContext(typeof(FogoDbContext))]
[Migration("20261027050000_GeocodificarCamerasPublicas")]
public sealed class GeocodificarCamerasPublicas : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Valida o catálogo completo atual; esta migration histórica limpa as primeiras 80 posições sem reinserir registros.
        migrationBuilder.Sql(
            """
            DO $$
            DECLARE
                quantidade integer;
            BEGIN
                SELECT count(*) INTO quantidade
                FROM cameras_publicas
                WHERE id BETWEEN 'd067b975-41f9-48fa-b020-000000000001'::uuid
                             AND 'd067b975-41f9-48fa-b020-000000000094'::uuid;

                IF quantidade <> 94 THEN
                    RAISE EXCEPTION 'Esperadas 94 câmeras existentes (IDs determinísticos 1–94); encontradas %.', quantidade;
                END IF;
            END $$;

            UPDATE cameras_publicas
            SET latitude = NULL, longitude = NULL
            WHERE id BETWEEN 'd067b975-41f9-48fa-b020-000000000001'::uuid
                         AND 'd067b975-41f9-48fa-b020-000000000080'::uuid;

            UPDATE cameras_publicas SET latitude = -21.922132, longitude = -50.731740
            WHERE id = 'd067b975-41f9-48fa-b020-000000000011'::uuid;

            UPDATE cameras_publicas SET latitude = -21.920797, longitude = -50.734317
            WHERE id = 'd067b975-41f9-48fa-b020-000000000013'::uuid;

            UPDATE cameras_publicas SET latitude = -22.979992, longitude = -49.863077
            WHERE id = 'd067b975-41f9-48fa-b020-000000000047'::uuid;

            UPDATE cameras_publicas SET latitude = -22.982451, longitude = -49.873824
            WHERE id = 'd067b975-41f9-48fa-b020-000000000049'::uuid;

            UPDATE cameras_publicas SET latitude = -22.421611, longitude = -50.581093
            WHERE id = 'd067b975-41f9-48fa-b020-000000000064'::uuid;

            UPDATE cameras_publicas SET latitude = -22.249936, longitude = -50.696541
            WHERE id = 'd067b975-41f9-48fa-b020-000000000066'::uuid;

                        -- Backfill one baseline snapshot per seeded demo agent from its existing stored position.
                        -- No interpolated or additional coordinates are generated.
                        WITH agentes_demo(id_localizacao, agente_id) AS (
                                VALUES
                                        ('9b1d7071-9af7-4e8d-a0aa-000000000001'::uuid, '6b8b75cf-7b7f-42fc-b2d9-000000000001'::uuid),
                                        ('9b1d7071-9af7-4e8d-a0aa-000000000002'::uuid, '6b8b75cf-7b7f-42fc-b2d9-000000000002'::uuid),
                                        ('9b1d7071-9af7-4e8d-a0aa-000000000003'::uuid, '6b8b75cf-7b7f-42fc-b2d9-000000000003'::uuid),
                                        ('9b1d7071-9af7-4e8d-a0aa-000000000004'::uuid, '6b8b75cf-7b7f-42fc-b2d9-000000000004'::uuid)
                        )
                        INSERT INTO historico_localizacoes_agentes (id, agente_id, latitude, longitude, registrada_em)
                        SELECT demo.id_localizacao, agente.id, agente.latitude, agente.longitude, agente.atualizado_em
                        FROM agentes_demo AS demo
                        INNER JOIN agentes AS agente ON agente.id = demo.agente_id
                        WHERE agente.origem_dados = 'Simulado'
                            AND agente.latitude IS NOT NULL
                            AND agente.longitude IS NOT NULL
                        ON CONFLICT (id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reverte apenas as coordenadas dos 80 registros; restaura os seis valores anteriores conhecidos.
        migrationBuilder.Sql(
            """
            DELETE FROM historico_localizacoes_agentes
            WHERE id IN (
                '9b1d7071-9af7-4e8d-a0aa-000000000001'::uuid,
                '9b1d7071-9af7-4e8d-a0aa-000000000002'::uuid,
                '9b1d7071-9af7-4e8d-a0aa-000000000003'::uuid,
                '9b1d7071-9af7-4e8d-a0aa-000000000004'::uuid
            );

            UPDATE cameras_publicas
            SET latitude = NULL, longitude = NULL
            WHERE id BETWEEN 'd067b975-41f9-48fa-b020-000000000001'::uuid
                         AND 'd067b975-41f9-48fa-b020-000000000080'::uuid;

            UPDATE cameras_publicas SET latitude = -23.104250, longitude = -48.923810
            WHERE id = 'd067b975-41f9-48fa-b020-000000000004'::uuid;

            UPDATE cameras_publicas SET latitude = -23.099820, longitude = -48.925140
            WHERE id = 'd067b975-41f9-48fa-b020-000000000005'::uuid;

            UPDATE cameras_publicas SET latitude = -22.301420, longitude = -48.558910
            WHERE id = 'd067b975-41f9-48fa-b020-000000000033'::uuid;

            UPDATE cameras_publicas SET latitude = -22.299530, longitude = -48.563820
            WHERE id = 'd067b975-41f9-48fa-b020-000000000034'::uuid;

            UPDATE cameras_publicas SET latitude = -22.978510, longitude = -49.869820
            WHERE id = 'd067b975-41f9-48fa-b020-000000000054'::uuid;

            UPDATE cameras_publicas SET latitude = -22.975420, longitude = -49.867110
            WHERE id = 'd067b975-41f9-48fa-b020-000000000055'::uuid;
            """);
    }
}
