using Fogo.Api.Identity;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fogo.Api.Identity.Migrations;

[DbContext(typeof(FogoDbContext))]
[Migration("20261027180000_CorrigirCoordenadasCamerasPublicas")]
public sealed class CorrigirCoordenadasCamerasPublicas : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
                    RAISE EXCEPTION 'Esperadas 94 câmeras do catálogo (IDs determinísticos 1–94); encontradas %.', quantidade;
                END IF;
            END $$;

            UPDATE cameras_publicas
            SET latitude = NULL, longitude = NULL
            WHERE id BETWEEN 'd067b975-41f9-48fa-b020-000000000001'::uuid
                         AND 'd067b975-41f9-48fa-b020-000000000094'::uuid;

            WITH coordenadas(id, latitude, longitude) AS (
                VALUES
                    ('d067b975-41f9-48fa-b020-000000000001'::uuid, -23.1048, -48.9397),
                    ('d067b975-41f9-48fa-b020-000000000002'::uuid, -23.1050, -48.9395),
                    ('d067b975-41f9-48fa-b020-000000000003'::uuid, -23.0865, -48.9510),
                    ('d067b975-41f9-48fa-b020-000000000004'::uuid, -23.1052, -48.9278),
                    ('d067b975-41f9-48fa-b020-000000000005'::uuid, -23.1055, -48.9261),
                    ('d067b975-41f9-48fa-b020-000000000006'::uuid, -21.9185, -50.7318),
                    ('d067b975-41f9-48fa-b020-000000000007'::uuid, -21.9120, -50.7390),
                    ('d067b975-41f9-48fa-b020-000000000008'::uuid, -21.9222, -50.7321),
                    ('d067b975-41f9-48fa-b020-000000000009'::uuid, -21.9212, -50.7330),
                    ('d067b975-41f9-48fa-b020-000000000010'::uuid, -21.9200, -50.7310),
                    ('d067b975-41f9-48fa-b020-000000000015'::uuid, -23.0430, -50.0745),
                    ('d067b975-41f9-48fa-b020-000000000016'::uuid, -23.0445, -50.0750),
                    ('d067b975-41f9-48fa-b020-000000000017'::uuid, -23.0460, -50.0730),
                    ('d067b975-41f9-48fa-b020-000000000018'::uuid, -23.0340, -49.1668),
                    ('d067b975-41f9-48fa-b020-000000000019'::uuid, -23.0345, -49.1672),
                    ('d067b975-41f9-48fa-b020-000000000020'::uuid, -23.0380, -49.1640),
                    ('d067b975-41f9-48fa-b020-000000000021'::uuid, -23.0382, -49.1638),
                    ('d067b975-41f9-48fa-b020-000000000022'::uuid, -23.0353, -49.1652),
                    ('d067b975-41f9-48fa-b020-000000000025'::uuid, -22.7445, -50.3880),
                    ('d067b975-41f9-48fa-b020-000000000026'::uuid, -22.7432, -50.3895),
                    ('d067b975-41f9-48fa-b020-000000000027'::uuid, -22.7428, -50.3902),
                    ('d067b975-41f9-48fa-b020-000000000028'::uuid, -22.7430, -50.3905),
                    ('d067b975-41f9-48fa-b020-000000000029'::uuid, -22.7520, -50.3820),
                    ('d067b975-41f9-48fa-b020-000000000030'::uuid, -22.6940, -49.4285),
                    ('d067b975-41f9-48fa-b020-000000000031'::uuid, -22.6932, -49.4295),
                    ('d067b975-41f9-48fa-b020-000000000032'::uuid, -22.2905, -48.5612),
                    ('d067b975-41f9-48fa-b020-000000000033'::uuid, -22.2960, -48.5580),
                    ('d067b975-41f9-48fa-b020-000000000034'::uuid, -22.2920, -48.5608),
                    ('d067b975-41f9-48fa-b020-000000000035'::uuid, -22.2760, -48.5510),
                    ('d067b975-41f9-48fa-b020-000000000036'::uuid, -22.2755, -48.5515),
                    ('d067b975-41f9-48fa-b020-000000000037'::uuid, -22.2942, -48.5588),
                    ('d067b975-41f9-48fa-b020-000000000038'::uuid, -22.2870, -48.5490),
                    ('d067b975-41f9-48fa-b020-000000000044'::uuid, -22.9778, -49.8688),
                    ('d067b975-41f9-48fa-b020-000000000045'::uuid, -22.9782, -49.8692),
                    ('d067b975-41f9-48fa-b020-000000000046'::uuid, -22.9815, -49.8720),
                    ('d067b975-41f9-48fa-b020-000000000047'::uuid, -22.9730, -49.8650),
                    ('d067b975-41f9-48fa-b020-000000000048'::uuid, -22.9840, -49.8560),
                    ('d067b975-41f9-48fa-b020-000000000049'::uuid, -22.9760, -49.8710),
                    ('d067b975-41f9-48fa-b020-000000000050'::uuid, -22.9800, -49.8700),
                    ('d067b975-41f9-48fa-b020-000000000056'::uuid, -22.4195, -50.5798),
                    ('d067b975-41f9-48fa-b020-000000000057'::uuid, -22.4210, -50.5770),
                    ('d067b975-41f9-48fa-b020-000000000058'::uuid, -22.4200, -50.5810),
                    ('d067b975-41f9-48fa-b020-000000000059'::uuid, -22.4180, -50.5805),
                    ('d067b975-41f9-48fa-b020-000000000065'::uuid, -22.2478, -50.6990),
                    ('d067b975-41f9-48fa-b020-000000000066'::uuid, -22.2465, -50.7005),
                    ('d067b975-41f9-48fa-b020-000000000067'::uuid, -22.2490, -50.6985),
                    ('d067b975-41f9-48fa-b020-000000000068'::uuid, -22.2510, -50.7015),
                    ('d067b975-41f9-48fa-b020-000000000069'::uuid, -22.2455, -50.7020),
                    ('d067b975-41f9-48fa-b020-000000000070'::uuid, -22.7890, -49.9280),
                    ('d067b975-41f9-48fa-b020-000000000071'::uuid, -22.7862, -49.9285),
                    ('d067b975-41f9-48fa-b020-000000000072'::uuid, -23.9120, -49.6530),
                    ('d067b975-41f9-48fa-b020-000000000073'::uuid, -23.9173, -49.6506),
                    ('d067b975-41f9-48fa-b020-000000000074'::uuid, -23.9195, -49.6490),
                    ('d067b975-41f9-48fa-b020-000000000075'::uuid, -23.9180, -49.6510),
                    ('d067b975-41f9-48fa-b020-000000000076'::uuid, -23.4480, -49.4020),
                    ('d067b975-41f9-48fa-b020-000000000077'::uuid, -23.4535, -49.4060),
                    ('d067b975-41f9-48fa-b020-000000000078'::uuid, -23.4545, -49.4055),
                    ('d067b975-41f9-48fa-b020-000000000079'::uuid, -23.4530, -49.4040),
                    ('d067b975-41f9-48fa-b020-000000000080'::uuid, -23.4550, -49.4050),
                    ('d067b975-41f9-48fa-b020-000000000081'::uuid, -23.4525, -49.4045),
                    ('d067b975-41f9-48fa-b020-000000000085'::uuid, -23.5350, -49.2468),
                    ('d067b975-41f9-48fa-b020-000000000086'::uuid, -23.5290, -49.2400),
                    ('d067b975-41f9-48fa-b020-000000000087'::uuid, -23.5265, -49.2380),
                    ('d067b975-41f9-48fa-b020-000000000088'::uuid, -23.5310, -49.2420),
                    ('d067b975-41f9-48fa-b020-000000000089'::uuid, -21.9330, -50.5148),
                    ('d067b975-41f9-48fa-b020-000000000090'::uuid, -21.9352, -50.5142),
                    ('d067b975-41f9-48fa-b020-000000000091'::uuid, -21.9380, -50.5120),
                    ('d067b975-41f9-48fa-b020-000000000092'::uuid, -21.9340, -50.5160),
                    ('d067b975-41f9-48fa-b020-000000000093'::uuid, -21.9345, -50.5155)
            )
            UPDATE cameras_publicas AS camera
            SET latitude = coordenadas.latitude,
                longitude = coordenadas.longitude
            FROM coordenadas
            WHERE camera.id = coordenadas.id;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reproduz o estado armazenado pela migration anterior, antes da correção individual.
        migrationBuilder.Sql(
            """
            UPDATE cameras_publicas
            SET latitude = NULL, longitude = NULL
            WHERE id BETWEEN 'd067b975-41f9-48fa-b020-000000000001'::uuid
                         AND 'd067b975-41f9-48fa-b020-000000000094'::uuid;

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
            UPDATE cameras_publicas SET latitude = -21.934371, longitude = -50.513274
            WHERE id = 'd067b975-41f9-48fa-b020-000000000090'::uuid;
            UPDATE cameras_publicas SET latitude = -21.935822, longitude = -50.518584
            WHERE id = 'd067b975-41f9-48fa-b020-000000000092'::uuid;
            """);
    }
}
