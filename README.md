# F.O.G.O.

Ferramenta Onisciente de Gestão e Operações — etapa bônus do desafio da Dsin.

## Estado

O desafio está dividido em inventário inteligente, central de operações e cidade preditiva. A base contém endpoint técnico de saúde, interface inicial e a fundação de autenticação/persistência. Inventário, mapa operacional, indicadores e predição ainda não foram implementados. As decisões e pendências estão em [docs/arquitetura.md](docs/arquitetura.md).

Os SDKs foram preparados em diretório local de usuário (fora do repositório): .NET SDK 10.0.402 e Node.js 24.20.0/npm 11.19.0. Os pacotes foram baixados de fontes oficiais e seus hashes conferidos. Para compilar no PowerShell desta máquina, foi necessário adicionar `FogoTools\dotnet` e `FogoTools\node-v24.20.0-win-x64` ao PATH da sessão; npm foi chamado via `npm.cmd` devido à política local que bloqueia scripts `.ps1`. PostgreSQL, Docker e `psql` não estão instalados; a fatia atual não depende deles.

## Direção técnica

- Backend: ASP.NET Core Web API com C#.
- Frontend: React, TypeScript e Vite.
- Persistência: PostgreSQL com Entity Framework Core e migrations; PostgreSQL ainda precisa ser provisionado no ambiente.
- Mapa planejado: Leaflet e OpenStreetMap.
- Autenticação: ASP.NET Core Identity, JWT de acesso, refresh token rotativo persistido somente como hash, cookies e proteção CSRF. Ainda não há provisionamento do primeiro usuário.

## Configuração local da API

Defina `FOGO_JWT_KEY` com pelo menos 32 bytes aleatórios usando um gerenciador de segredos ou variável de ambiente; nunca versione o valor. Configure `ConnectionStrings__PostgreSQL` com uma base PostgreSQL criada para este ambiente e, opcionalmente, `FOGO_ORIGINS` como lista de origens separadas por ponto e vírgula. Em desenvolvimento HTTP local, `FOGO_HTTPS_DEV=true` ativa cookies Secure; prefira HTTPS. A API falha ao iniciar se a chave ou conexão não estiver configurada.

O login não está aberto para cadastro público. Ainda falta implementar/documentar provisionamento administrativo seguro; não há usuário nem senha padrão.

## Próximas etapas

1. Provisionar PostgreSQL e aplicar migrations somente a uma base alvo revisada; nunca resetar bancos existentes.
2. Implementar provisionamento administrativo seguro e inventário protegido com campos derivados do desafio.
3. Exibir no mapa somente registros persistidos e derivar indicadores dos dados disponíveis.
4. Avaliar atualização em tempo real, integrações e predição conforme dados, autorização e prazo.

## Validação executada

- `dotnet test backend/Fogo.Tests/Fogo.Tests.csproj --no-restore`: passou (5 testes HTTP; a cobertura ainda não exercita persistência nem os fluxos completos de sessão).
- `dotnet build backend/Fogo.Api/Fogo.Api.csproj --no-restore`: passou.
- Migration inicial do Identity e das sessões gerada em `backend/Fogo.Api/Identity/Migrations`; não foi aplicada porque PostgreSQL ainda não está instalado.
- `npm install` em `frontend/`: passou, 0 vulnerabilidades reportadas.
- `npm run build` em `frontend/`: passou (TypeScript e Vite).
- `dotnet list ... package --vulnerable --include-transitive`: nenhum pacote vulnerável nos projetos de teste.

Nenhuma integração ou previsão será apresentada como funcional sem validação. Dados demonstrativos, caso sejam adicionados, deverão estar claramente identificados como simulados.

## Uso de IA

GitHub Copilot foi utilizado como assistente de análise e implementação. O responsável pelo projeto deve revisar e compreender o código antes da entrega.
