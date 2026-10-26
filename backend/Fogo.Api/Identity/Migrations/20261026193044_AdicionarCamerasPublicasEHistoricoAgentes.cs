using System;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fogo.Api.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCamerasPublicasEHistoricoAgentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cameras_publicas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cidade = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    nome = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    url_stream = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    endereco = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cameras_publicas", x => x.id);
                    table.CheckConstraint("CK_cameras_publicas_coordenadas_completas", "(latitude IS NULL AND longitude IS NULL) OR (latitude IS NOT NULL AND longitude IS NOT NULL)");
                    table.CheckConstraint("CK_cameras_publicas_latitude", "latitude IS NULL OR latitude BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_cameras_publicas_longitude", "longitude IS NULL OR longitude BETWEEN -180 AND 180");
                    table.CheckConstraint("CK_cameras_publicas_stream_https", "url_stream LIKE 'https://%'");
                });

            migrationBuilder.CreateTable(
                name: "historico_localizacoes_agentes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    registrada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historico_localizacoes_agentes", x => x.id);
                    table.CheckConstraint("CK_historico_localizacoes_agentes_latitude", "latitude BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_historico_localizacoes_agentes_longitude", "longitude BETWEEN -180 AND 180");
                    table.ForeignKey(
                        name: "FK_historico_localizacoes_agentes_agentes_agente_id",
                        column: x => x.agente_id,
                        principalTable: "agentes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cameras_publicas_cidade_nome",
                table: "cameras_publicas",
                columns: new[] { "cidade", "nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cameras_publicas_latitude_longitude",
                table: "cameras_publicas",
                columns: new[] { "latitude", "longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_historico_localizacoes_agentes_agente_id_registrada_em",
                table: "historico_localizacoes_agentes",
                columns: new[] { "agente_id", "registrada_em" });

            var catalogo = new[]
            {
                "Avaré|Mercadão Frente|hls/avare-mercadao-frente/index.m3u8||",
                "Avaré|Mercadão Fundos|hls/avare-mercadao-fundos/index.m3u8||",
                "Avaré|Portal Entrada Avaré|hls/avare-portal-entrada-avare/index.m3u8||",
                "Avaré|R.G do Sul x D.Santana|hls/avare-rg-do-sul-x-d-santana/index.m3u8||",
                "Avaré|R.G do Sul x Maranhão|hls/avare-r-g-do-sul-x-maranhao/index.m3u8||",
                "Bastos|Areninha|hls/bastos-areninha/index.m3u8||",
                "Bastos|Entrada Kawakami|hls/bastos-entrada-kawakami/index.m3u8||",
                "Bastos|Entrada Prefeitura|hls/bastos-entrada-prefeitura/index.m3u8||",
                "Bastos|Praça 500 anos|hls/bastos-preca-500-anos/index.m3u8||",
                "Bastos|Praça da Juventude|hls/bastos-praca-da-juventude/index.m3u8||",
                "Bastos|Praça do Ovo|hls/bastos-praca-do-ovo/index.m3u8||",
                "Bastos|Praça S.J.Operário|hls/bastos-praca-s-j-operario/index.m3u8||",
                "Bastos|Praça Senjiro Hatanaka|hls/bastos-praca-senjiro-hatanaka/index.m3u8||",
                "Bastos|Entrada Yabuta|hls/bastos-entrada-yabuta/index.m3u8||",
                "Cambará|Genaro Resende 1|hls/cambara-genaro-resende-01/index.m3u8||",
                "Cambará|Genaro Resende 2|hls/cambara-genaro-resende-2/index.m3u8||",
                "Cambará|Parque Ignez Panichi|hls/cambara-parque-ignez-panichi/index.m3u8||",
                "Cerqueira César|Areninha|hls/cerqueira-cesar-areninha/index.m3u8||",
                "Cerqueira César|Areninha 2|hls/cerqueira-cesar-areninha-2/index.m3u8||",
                "Cerqueira César|Patio Estação|hls/cerqueira-cesar-patio-estacao/index.m3u8||",
                "Cerqueira César|Patio Estação 2|hls/cerqueira-cesar-patio-estacao-2/index.m3u8||",
                "Cerqueira César|Praça Matriz|cmaf/cerqueira-matriz/index.m3u8||",
                "Cerqueira César|Praça Matriz 2|cmaf/cerqueira-matriz-2/index.m3u8||",
                "Cerqueira César|Rodoviária|cmaf/cerqueira-rodoviaria/index.m3u8||",
                "Cândido Mota|Parque Panarao|hls/candido-mota-parque-panarao/index.m3u8||",
                "Cândido Mota|Praça Igreja Popular|hls/candido-mota-praca-igreja-popular/index.m3u8||",
                "Cândido Mota|Praça Matriz 1|hls/candido-mota-praca-matriz-1/index.m3u8||",
                "Cândido Mota|Praça Matriz 2|hls/candido-mota-praca-matriz-2/index.m3u8||",
                "Cândido Mota|Rotátoria do Cristo|hls/candido-mota-rotatoria-do-cristo/index.m3u8||",
                "Espírito Santo do Turvo|Praça Matriz|hls/espirito-sant-turvo-matriz/index.m3u8||",
                "Espírito Santo do Turvo|Praça José Melo|hls/espirito-sant-turvo-jose-melo/index.m3u8||",
                "Jaú|Av. Anna Claudina|hls/jau-detecta-av-anna-claudina/index.m3u8||",
                "Jaú|Cruzamento Av. João Ferraz X Isaltino do Amaral Carvalho|hls/jau-cruzamento-joao--x-isaltino/index.m3u8||",
                "Jaú|Cruzamento Av. Zezinho Magalhães X Av. Anna Claudina|hls/jau-cruz-zezinho-x-anaclaudina/index.m3u8||",
                "Jaú|Lago do Silvério 1|cmaf/jau-lago-silverio-1/index.m3u8||",
                "Jaú|Lago do Silvério 2|cmaf/au-lago-silverio-2/index.m3u8||",
                "Jaú|Museu Municipal|hls/jau-museu-1/index.m3u8||",
                "Jaú|Parque do Rio Jaú 1|hls/jau-parque-rio-jau/index.m3u8||",
                "Jaú|Parque do Rio Jaú 2|hls/jau-parque-rio-jau2/index.m3u8||",
                "Jaú|Kartódromo 1|hls/jau-kartodromo1/index.m3u8||",
                "Jaú|Kartódromo 2|cmaf/jau-kartodromo-2/index.m3u8||",
                "Jaú|Praça da República 1|hls/jau-praca-republica01/index.m3u8||",
                "Jaú|Praça da República 2|hls/jau-praca-republica02/index.m3u8||",
                "Ourinhos|Calçadão|hls/ourinhos-calcadao/index.m3u8||",
                "Ourinhos|Calçadão 2|hls/ourinhos-calcadao-2/index.m3u8||",
                "Ourinhos|Praça Recanto dos Pássaros|hls/ourinhos-praca-recanto-passaros/index.m3u8||",
                "Ourinhos|Plaza Shopping|hls/ourinhos-plaza-shopping/index.m3u8||",
                "Ourinhos|Feira da lua Cohab|hls/ourinhos-feira-da-lua-cohab/index.m3u8||",
                "Ourinhos|Praça dos Skatistas|hls/ourinhos-praca-dos-shatistas/index.m3u8||",
                "Ourinhos|Praça Kennedy|hls/ourinhos-praca-kennedy/index.m3u8||",
                "Ourinhos|Praça dos Ferroviários|hls/ourinhos-praca-dos-ferroviarios/index.m3u8||",
                "Ourinhos|Praça São Pio X|hls/ourinhos-sao-pio-x/index.m3u8||",
                "Ourinhos|Praça Vila Perino|hls/ourinhos-praca-vila-peino/index.m3u8||",
                "Ourinhos|Rio de Janeiro x Antônio Carlos Mori|hls/ourinhos-rio-de-janeiro-x-antonio-mori/index.m3u8||",
                "Ourinhos|Altino Arantes x Cardoso Ribeiro|hls/ourinhos-altino-arantes-x-cardoso-ribeiro/index.m3u8||",
                "Paraguaçu Paulista|Avenida Brasil|hls/paraguacu-paulista-avenida-brasil/index.m3u8||",
                "Paraguaçu Paulista|Balneário Municipal|hls/paraguacu-paulista-balneario-municipal/index.m3u8||",
                "Paraguaçu Paulista|Centro de Convergência|hls/paraguacu-paulista-centro-de-convergencia/index.m3u8||",
                "Paraguaçu Paulista|Fonte Luminosa|hls/paraguacu-paulista-fonte-luminosa/index.m3u8||",
                "Paraguaçu Paulista|Maria Fumaça|hls/paraguacu-paulista-maria-fumaca/index.m3u8||",
                "Paraguaçu Paulista|Portal da Entrada|hls/paraguacu-paulista-portal-da-entrada/index.m3u8||",
                "Paraguaçu Paulista|Praça Matriz|hls/paraguacu-paulista-praca-matriz/index.m3u8||",
                "Paraguaçu Paulista|Praça Panambi|hls/paraguacu-paulista-praca-panambi/index.m3u8||",
                "Paraguaçu Paulista|Prefeitura Municipal|hls/paraguacu-paulista-prefeitura-municipal/index.m3u8||",
                "Quatá|Praça Matriz|hls/quata-matriz/index.m3u8||",
                "Quatá|Praça da Fonte|hls/quata-praca-fonte/index.m3u8||",
                "Quatá|Praça Valéria|hls/quata-praca-valeria/index.m3u8||",
                "Quatá|Praça do Campo Sintético|hls/quata-praca-campo-sintetico/index.m3u8||",
                "Quatá|Praça Jardim Tropical|hls/quata-praca-jardim-tropical/index.m3u8||",
                "Ribeirão do Sul|Entrada cristo|hls/ribeirao-sul-entrada-cristo/index.m3u8||",
                "Ribeirão do Sul|Matriz|hls/ribeirao-sul-matriz/index.m3u8||",
                "São José da Boa Vista|Entrada do Santo|hls/sao-jose-da-boa-vista-entrada-do-santo/index.m3u8||",
                "São José da Boa Vista|Praça da Igreja|hls/sao-jose-da-boa-vista-praca-igreja/index.m3u8||",
                "São José da Boa Vista|Praça do Estádio|hls/sao-jose-da-boa-vista-prado-do-estadio/index.m3u8||",
                "São José da Boa Vista|Praça José Maria Soares|hls/sao-jose-da-boa-vista-jose-maria/index.m3u8||",
                "Taguaí|Entrada do Cristo|hls/taguai-entrada-santo/index.m3u8||",
                "Taguaí|Entada e saida de Alunos -Pe. Giovanne|hls/taguai-entrada-saida-alunos-giovanne/index.m3u8||",
                "Taguaí|Lanchodromo|hls/taguai-lanchodromo/index.m3u8||",
                "Taguaí|Praça Angelo Globo|hls/taguai-praca-angelo-globo/index.m3u8||",
                "Taguaí|Praça Lanchodromo|hls/taguai-praca-lanchodromo/index.m3u8||",
                "Taguaí|Praça Rubens Alves|hls/taguai-praca-rubens-alves/index.m3u8||",
                "Taguaí|Praça São Gabriel|hls/taguai-praca-sao-gabriel/index.m3u8||",
                "Taguaí|Prefeitura Municipal|hls/taguai-prefeitura-municipal/index.m3u8||",
                "Taguaí|Quadra e Pista de Skate|hls/taguai-quadra-e-pista-de-skate/index.m3u8||",
                "Taquarituba|Praça São Roque|hls/taquarituba-praca-sao-roque/index.m3u8||",
                "Taquarituba|Trevo Cel. Macedo|hls/taquarituba-trevo-cel-macedo/index.m3u8||",
                "Taquarituba|Trevo Itai|hls/taquarituba-trevo-itai/index.m3u8||",
                "Taquarituba|Trevo Taguai|hls/taquarituba-trevo-taguai/index.m3u8||",
                "Tupã|Brasil x Caingangs|hls/tupa-brasil-x-caingangs/index.m3u8||",
                "Tupã|Praça da Bandeira|hls/tupa-praca-da-bandeira/index.m3u8|-21.934371|-50.513274",
                "Tupã|Praça do Estádio|hls/tupa-praca-estadio/index.m3u8||",
                "Tupã|Praça Dom Bosco|hls/tupa-praca-dom-bosco/index.m3u8|-21.935822|-50.518584",
                "Tupã|Tamoios x Aimorés|hls/tupa-tamoios-x-aimores/index.m3u8||",
                "Tupã|Tamois x Caingangs|hls/tupa-tamois-x-caingangs/index.m3u8||"
            };

            var indice = 0;
            foreach (var linha in catalogo)
            {
                var campos = linha.Split('|');
                var id = Guid.Parse($"d067b975-41f9-48fa-b020-{++indice:D12}");
                decimal? latitude = decimal.TryParse(campos[3], NumberStyles.Number, CultureInfo.InvariantCulture, out var lat) ? lat : null;
                decimal? longitude = decimal.TryParse(campos[4], NumberStyles.Number, CultureInfo.InvariantCulture, out var lon) ? lon : null;
                migrationBuilder.InsertData(
                    table: "cameras_publicas",
                    columns: new[] { "id", "cidade", "nome", "url_stream", "endereco", "latitude", "longitude" },
                    values: new object[] { id, campos[0], campos[1], $"https://ipservice.aovivonanet.com.br:4433/{campos[2]}", null!, (object)latitude, (object)longitude });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cameras_publicas");

            migrationBuilder.DropTable(
                name: "historico_localizacoes_agentes");
        }
    }
}
