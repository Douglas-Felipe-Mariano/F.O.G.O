using Fogo.Api.Identity;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fogo.Api.Identity.Migrations;

[DbContext(typeof(FogoDbContext))]
[Migration("20261027200000_PreservarPrecisaoCoordenadasCameras")]
public sealed class PreservarPrecisaoCoordenadasCameras : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE cameras_publicas
                ALTER COLUMN latitude TYPE numeric(10,7),
                ALTER COLUMN longitude TYPE numeric(10,7);

            DO $$
            DECLARE
                quantidade integer;
            BEGIN
                SELECT count(*) INTO quantidade
                FROM cameras_publicas
                WHERE (id = 'd067b975-41f9-48fa-b020-000000000089'::uuid AND cidade = 'Tupã' AND nome = 'Brasil x Caingangs')
                   OR (id = 'd067b975-41f9-48fa-b020-000000000091'::uuid AND cidade = 'Tupã' AND nome = 'Praça do Estádio')
                   OR (id = 'd067b975-41f9-48fa-b020-000000000092'::uuid AND cidade = 'Tupã' AND nome = 'Praça Dom Bosco')
                   OR (id = 'd067b975-41f9-48fa-b020-000000000093'::uuid AND cidade = 'Tupã' AND nome = 'Tamoios x Aimorés')
                   OR (id = 'd067b975-41f9-48fa-b020-000000000094'::uuid AND cidade = 'Tupã' AND nome = 'Tamois x Caingangs');

                IF quantidade <> 5 THEN
                    RAISE EXCEPTION 'Esperadas 5 câmeras de Tupã para correção; encontradas %.', quantidade;
                END IF;
            END $$;

            UPDATE cameras_publicas SET latitude = -21.9335005, longitude = -50.4845625
            WHERE id = 'd067b975-41f9-48fa-b020-000000000089'::uuid;

            UPDATE cameras_publicas SET latitude = -21.9340313, longitude = -50.5189944
            WHERE id = 'd067b975-41f9-48fa-b020-000000000091'::uuid;

            UPDATE cameras_publicas SET latitude = -21.9358080, longitude = -50.5185844
            WHERE id = 'd067b975-41f9-48fa-b020-000000000092'::uuid;

            UPDATE cameras_publicas SET latitude = -21.9284604, longitude = -50.5120535
            WHERE id = 'd067b975-41f9-48fa-b020-000000000093'::uuid;

            UPDATE cameras_publicas SET latitude = -21.9319624, longitude = -50.5161392
            WHERE id = 'd067b975-41f9-48fa-b020-000000000094'::uuid;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE cameras_publicas SET latitude = -21.933000, longitude = -50.514800
            WHERE id = 'd067b975-41f9-48fa-b020-000000000089'::uuid;

            UPDATE cameras_publicas SET latitude = -21.938000, longitude = -50.512000
            WHERE id = 'd067b975-41f9-48fa-b020-000000000091'::uuid;

            UPDATE cameras_publicas SET latitude = -21.934000, longitude = -50.516000
            WHERE id = 'd067b975-41f9-48fa-b020-000000000092'::uuid;

            UPDATE cameras_publicas SET latitude = -21.934500, longitude = -50.515500
            WHERE id = 'd067b975-41f9-48fa-b020-000000000093'::uuid;

            UPDATE cameras_publicas SET latitude = NULL, longitude = NULL
            WHERE id = 'd067b975-41f9-48fa-b020-000000000094'::uuid;

            ALTER TABLE cameras_publicas
                ALTER COLUMN latitude TYPE numeric(9,6),
                ALTER COLUMN longitude TYPE numeric(9,6);
            """);
    }
}
