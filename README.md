# F.O.G.O.

Ferramenta Onisciente de Gestão e Operações — etapa bônus do desafio da Dsin.

## Estado

O desafio está dividido em inventário inteligente, central de operações e cidade preditiva. A base contém endpoint técnico de saúde, autenticação fundacional, API protegida para cadastro, consulta, filtros, atualização e contagens agregadas de equipamentos, além de interface web de inventário/login com mapa dos equipamentos persistidos georreferenciados. Cadastro de agentes e predição ainda não foram implementados. As decisões e pendências estão em [docs/arquitetura.md](docs/arquitetura.md).

**Etapa atual (24/10):** núcleo da Fase 2 concluído; início da Fase 3 com endpoint e visualização de mapa. Falta validar o fluxo conectado a PostgreSQL e provisionar o primeiro usuário por procedimento seguro.

Os SDKs foram preparados em diretório local de usuário (fora do repositório): .NET SDK 10.0.402 e Node.js 24.20.0/npm 11.19.0. Os pacotes foram baixados de fontes oficiais e seus hashes conferidos. Para compilar no PowerShell desta máquina, foi necessário adicionar `FogoTools\dotnet` e `FogoTools\node-v24.20.0-win-x64` ao PATH da sessão; npm foi chamado via `npm.cmd` devido à política local que bloqueia scripts `.ps1`. PostgreSQL, Docker e `psql` não estão instalados; a fatia atual não depende deles.

No desenvolvimento, o Vite encaminha `/api` para `https://localhost:7001`. Se o perfil local da API usar outra porta, defina `FOGO_API_URL` antes de iniciar o frontend. O navegador precisa confiar no certificado HTTPS de desenvolvimento da API.

## Direção técnica

- Backend: ASP.NET Core Web API com C#.
- Frontend: React, TypeScript e Vite.
- Persistência: PostgreSQL com Entity Framework Core e migrations; PostgreSQL ainda precisa ser provisionado no ambiente.
- Mapa inicial: Leaflet e tiles OpenStreetMap; exibe somente coordenadas cadastradas, requer conexão externa para os tiles e mantém atribuição aos contribuidores.
- Autenticação: ASP.NET Core Identity, JWT de acesso mantido em memória, refresh token rotativo persistido somente como hash em cookie `HttpOnly` e proteção CSRF. Ainda não há provisionamento do primeiro usuário.

No backend, o inventário está organizado em Controllers (HTTP), Application (validação e casos de uso), Repositories (consultas/persistência) e Identity (autenticação e `DbContext`). As migrations EF seguem no projeto da API e permanecem como fonte oficial do esquema.

## Configuração local da API

Defina `FOGO_JWT_KEY` com pelo menos 32 bytes aleatórios usando um gerenciador de segredos ou variável de ambiente; nunca versione o valor. Configure `ConnectionStrings__PostgreSQL` com uma base PostgreSQL criada para este ambiente e, opcionalmente, `FOGO_ORIGINS` como lista de origens separadas por ponto e vírgula. Em desenvolvimento HTTP local, `FOGO_HTTPS_DEV=true` ativa cookies Secure; prefira HTTPS. A API falha ao iniciar se a chave ou conexão não estiver configurada.

O login não está aberto para cadastro público. Ainda falta implementar/documentar provisionamento administrativo seguro; não há usuário nem senha padrão.

Com a API em execução, iniciar `npm.cmd run dev` dentro de `frontend/` abre o portal em `http://localhost:5173`; o navegador solicita o cookie CSRF ao backend e a API é acessada pelo proxy Vite. Sem um usuário provisionado em banco, não será possível entrar ainda.

## Próximas etapas

1. Provisionar PostgreSQL e aplicar migrations somente a uma base alvo revisada; nunca resetar bancos existentes.
2. Implementar provisionamento administrativo seguro e validar sessões e operações com PostgreSQL.
3. Validar mapa e inventário integrados com dados persistidos; a API limita o mapa a 500 pontos por padrão e 1.000 no máximo por consulta.
4. Avaliar cadastro de agentes, atualização em tempo real, integrações e predição conforme dados, autorização e prazo.

## Validação executada

- `dotnet test backend/Fogo.Tests/Fogo.Tests.csproj --no-restore`: passou (24 testes; cobrem autorização, descoberta dos Controllers, contrato JSON, validações, bootstrap CSRF e modelo EF, mas não persistência nem fluxos completos de sessão).
- `dotnet build backend/Fogo.Api/Fogo.Api.csproj --no-restore`: passou.
- Migration inicial do Identity e das sessões gerada em `backend/Fogo.Api/Identity/Migrations`; não foi aplicada porque PostgreSQL ainda não está instalado.
- Migration incremental de equipamentos também gerada. O espelho SQL para leitura da arquitetura do banco está em `backend/database/CREATE.sql` e não contém dados iniciais.
- `npm install` em `frontend/`: passou, 0 vulnerabilidades reportadas.
- `npm run build` em `frontend/`: passou (TypeScript e Vite).
- `npm audit` em `frontend/`: nenhum pacote vulnerável reportado.
- `frontend/`: mapa Leaflet construído para exibir localizações retornadas pela API; tiles OpenStreetMap exigem internet e a experiência visual integrada ainda precisa de validação com uma sessão e banco reais.
- Auditoria NuGet dos projetos API e testes: nenhum pacote vulnerável reportado.
- PostgreSQL continua indisponível neste ambiente; migrations e `CREATE.sql` não foram aplicados nem validados em instância real.

Nenhuma integração ou previsão será apresentada como funcional sem validação. Dados demonstrativos, caso sejam adicionados, deverão estar claramente identificados como simulados.

## Uso de IA

GitHub Copilot foi utilizado como assistente de análise e implementação. O responsável pelo projeto deve revisar e compreender o código antes da entrega.
