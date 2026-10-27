using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fogo.Api.Identity.Migrations
{
    /// <inheritdoc />
    public partial class GeorreferenciarCamerasValidadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "cameras_publicas",
                keyColumn: "id",
                keyValue: Guid.Parse("d067b975-41f9-48fa-b020-000000000004"),
                columns: new[] { "latitude", "longitude" },
                values: new object[] { -23.104250m, -48.923810m });

            migrationBuilder.UpdateData(
                table: "cameras_publicas",
                keyColumn: "id",
                keyValue: Guid.Parse("d067b975-41f9-48fa-b020-000000000005"),
                columns: new[] { "latitude", "longitude" },
                values: new object[] { -23.099820m, -48.925140m });

            migrationBuilder.UpdateData(
                table: "cameras_publicas",
                keyColumn: "id",
                keyValue: Guid.Parse("d067b975-41f9-48fa-b020-000000000033"),
                columns: new[] { "latitude", "longitude" },
                values: new object[] { -22.301420m, -48.558910m });

            migrationBuilder.UpdateData(
                table: "cameras_publicas",
                keyColumn: "id",
                keyValue: Guid.Parse("d067b975-41f9-48fa-b020-000000000034"),
                columns: new[] { "latitude", "longitude" },
                values: new object[] { -22.299530m, -48.563820m });

            migrationBuilder.UpdateData(
                table: "cameras_publicas",
                keyColumn: "id",
                keyValue: Guid.Parse("d067b975-41f9-48fa-b020-000000000054"),
                columns: new[] { "latitude", "longitude" },
                values: new object[] { -22.978510m, -49.869820m });

            migrationBuilder.UpdateData(
                table: "cameras_publicas",
                keyColumn: "id",
                keyValue: Guid.Parse("d067b975-41f9-48fa-b020-000000000055"),
                columns: new[] { "latitude", "longitude" },
                values: new object[] { -22.975420m, -49.867110m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var id in new[] { 4, 5, 33, 34, 54, 55 })
            {
                migrationBuilder.UpdateData(
                    table: "cameras_publicas",
                    keyColumn: "id",
                    keyValue: Guid.Parse($"d067b975-41f9-48fa-b020-{id:D12}"),
                    columns: new[] { "latitude", "longitude" },
                    values: new object[] { null, null });
            }
        }
    }
}
