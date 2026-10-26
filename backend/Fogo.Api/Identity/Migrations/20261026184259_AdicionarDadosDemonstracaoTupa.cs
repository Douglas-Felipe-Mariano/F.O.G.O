using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fogo.Api.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarDadosDemonstracaoTupa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var instanteDemonstracao = new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

            migrationBuilder.InsertData(
                table: "equipamentos",
                columns: new[]
                {
                    "id", "tipo", "numero_serie", "marca", "modelo", "cep", "bairro", "cidade", "uf", "rua",
                    "referencia", "latitude", "longitude", "situacao", "necessita_manutencao", "tipo_manutencao",
                    "proxima_manutencao", "origem_dados", "criado_em", "atualizado_em"
                },
                values: new object[,]
                {
                    { Guid.Parse("c36a64dd-e932-4899-b5c7-000000000001"), "Radar demonstrativo", "SIM-RADAR-TUPA-001", "Demonstração", "Ponto ilustrativo", "17600-000", "Trecho demonstrativo", "Tupã", "SP", "SP-294 · ponto demonstrativo", "DADO SIMULADO; não representa radar confirmado.", -21.934200m, -50.513400m, "Operacional", false, null, null, "Simulado", instanteDemonstracao, instanteDemonstracao },
                    { Guid.Parse("c36a64dd-e932-4899-b5c7-000000000002"), "Radar demonstrativo", "SIM-RADAR-TUPA-002", "Demonstração", "Ponto ilustrativo", "17690-000", "Trecho demonstrativo", "Bastos", "SP", "SP-317 · ponto demonstrativo", "DADO SIMULADO; não representa radar confirmado.", -21.921700m, -50.733800m, "Operacional", false, null, null, "Simulado", instanteDemonstracao, instanteDemonstracao },
                    { Guid.Parse("c36a64dd-e932-4899-b5c7-000000000003"), "Radar demonstrativo", "SIM-RADAR-TUPA-003", "Demonstração", "Ponto ilustrativo", "17650-000", "Trecho demonstrativo", "Herculândia", "SP", "SP-294 · ponto demonstrativo", "DADO SIMULADO; não representa radar confirmado.", -22.003500m, -50.292300m, "Operacional", false, null, null, "Simulado", instanteDemonstracao, instanteDemonstracao }
                });

            migrationBuilder.InsertData(
                table: "agentes",
                columns: new[]
                {
                    "id", "nome", "categoria_profissional", "base_operacional", "latitude", "longitude",
                    "situacao", "origem_dados", "criado_em", "atualizado_em"
                },
                values: new object[,]
                {
                    { Guid.Parse("6b8b75cf-7b7f-42fc-b2d9-000000000001"), "Agente demonstrativo 01", "Equipe de campo demonstrativa", "Tupã · ponto ilustrativo", -21.933500m, -50.519000m, "Disponivel", "Simulado", instanteDemonstracao, instanteDemonstracao },
                    { Guid.Parse("6b8b75cf-7b7f-42fc-b2d9-000000000002"), "Agente demonstrativo 02", "Equipe de campo demonstrativa", "Bastos · ponto ilustrativo", -21.921600m, -50.736600m, "Disponivel", "Simulado", instanteDemonstracao, instanteDemonstracao },
                    { Guid.Parse("6b8b75cf-7b7f-42fc-b2d9-000000000003"), "Agente demonstrativo 03", "Equipe de campo demonstrativa", "Herculândia · ponto ilustrativo", -22.003500m, -50.292400m, "EmAtendimento", "Simulado", instanteDemonstracao, instanteDemonstracao },
                    { Guid.Parse("6b8b75cf-7b7f-42fc-b2d9-000000000004"), "Agente demonstrativo 04", "Equipe de campo demonstrativa", "Iacri · ponto ilustrativo", -21.857900m, -50.691300m, "Indisponivel", "Simulado", instanteDemonstracao, instanteDemonstracao }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "equipamentos",
                keyColumn: "id",
                keyValues: new object[]
                {
                    Guid.Parse("c36a64dd-e932-4899-b5c7-000000000001"),
                    Guid.Parse("c36a64dd-e932-4899-b5c7-000000000002"),
                    Guid.Parse("c36a64dd-e932-4899-b5c7-000000000003")
                });

            migrationBuilder.DeleteData(
                table: "agentes",
                keyColumn: "id",
                keyValues: new object[]
                {
                    Guid.Parse("6b8b75cf-7b7f-42fc-b2d9-000000000001"),
                    Guid.Parse("6b8b75cf-7b7f-42fc-b2d9-000000000002"),
                    Guid.Parse("6b8b75cf-7b7f-42fc-b2d9-000000000003"),
                    Guid.Parse("6b8b75cf-7b7f-42fc-b2d9-000000000004")
                });
        }
    }
}
