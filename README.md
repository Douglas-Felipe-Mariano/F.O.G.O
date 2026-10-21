# F.O.G.O.

Ferramenta Onisciente de Gestão e Operações — etapa bônus do desafio da Dsin.

## Estado

O desafio está dividido em inventário inteligente, central de operações e cidade preditiva. Esta primeira fatia contém um endpoint técnico de saúde da API, uma página informativa e teste automatizado para a API. Inventário, autenticação, mapa, operações e predição ainda não foram implementados. As decisões e pendências estão em [docs/arquitetura.md](docs/arquitetura.md).

Os SDKs foram preparados em diretório local de usuário (fora do repositório): .NET SDK 10.0.402 e Node.js 24.20.0/npm 11.19.0. Os pacotes foram baixados de fontes oficiais e seus hashes conferidos. Para compilar no PowerShell desta máquina, foi necessário adicionar `FogoTools\dotnet` e `FogoTools\node-v24.20.0-win-x64` ao PATH da sessão; npm foi chamado via `npm.cmd` devido à política local que bloqueia scripts `.ps1`. PostgreSQL, Docker e `psql` não estão instalados; a fatia atual não depende deles.

## Direção técnica

- Backend: ASP.NET Core Web API com C#.
- Frontend: React, TypeScript e Vite.
- Persistência planejada: PostgreSQL com Entity Framework Core e migrations.
- Mapa planejado: Leaflet e OpenStreetMap.
- Autenticação/autorização são parte da fundação. Este esqueleto ainda não implementa login nem endpoints de negócio.

## Próximas etapas

1. Implementar autenticação segura e inventário protegido com campos derivados do desafio.
2. Configurar PostgreSQL e migrations somente após a fundação da persistência, sem resetar bancos existentes.
3. Exibir no mapa somente registros persistidos e derivar indicadores dos dados disponíveis.
4. Avaliar atualização em tempo real, integrações e predição conforme dados, autorização e prazo.

## Validação executada

- `dotnet test backend/Fogo.Tests/Fogo.Tests.csproj --no-restore`: passou (1 teste).
- `dotnet build backend/Fogo.Api/Fogo.Api.csproj --no-restore`: passou.
- `npm install` em `frontend/`: passou, 0 vulnerabilidades reportadas.
- `npm run build` em `frontend/`: passou (TypeScript e Vite).
- `dotnet list ... package --vulnerable --include-transitive`: nenhum pacote vulnerável nos projetos de teste.

Nenhuma integração ou previsão será apresentada como funcional sem validação. Dados demonstrativos, caso sejam adicionados, deverão estar claramente identificados como simulados.

## Uso de IA

GitHub Copilot foi utilizado como assistente de análise e implementação. O responsável pelo projeto deve revisar e compreender o código antes da entrega.
