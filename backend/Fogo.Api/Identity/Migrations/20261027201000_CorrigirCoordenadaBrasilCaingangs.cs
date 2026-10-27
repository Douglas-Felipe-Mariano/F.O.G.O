using Fogo.Api.Identity;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fogo.Api.Identity.Migrations;

[DbContext(typeof(FogoDbContext))]
[Migration("20261027201000_CorrigirCoordenadaBrasilCaingangs")]
public sealed class CorrigirCoordenadaBrasilCaingangs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM cameras_publicas
                    WHERE id = 'd067b975-41f9-48fa-b020-000000000089'::uuid
                      AND cidade = 'Tupã'
                      AND nome = 'Brasil x Caingangs') THEN
                    RAISE EXCEPTION 'A câmera Brasil x Caingangs de Tupã não foi encontrada no ID esperado.';
                END IF;
            END $$;

            UPDATE cameras_publicas
            SET latitude = -21.9254581, longitude = -50.5225641
            WHERE id = 'd067b975-41f9-48fa-b020-000000000089'::uuid;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE cameras_publicas
            SET latitude = -21.9335005, longitude = -50.4845625
            WHERE id = 'd067b975-41f9-48fa-b020-000000000089'::uuid;
            """);
    }
}
