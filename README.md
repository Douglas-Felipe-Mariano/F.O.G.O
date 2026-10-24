# F.O.G.O.

Ferramenta Onisciente de Gestão e Operações — etapa bônus do desafio da Dsin.

## Estado

O desafio está dividido em inventário inteligente, central de operações e cidade preditiva. A base contém endpoint técnico de saúde, autenticação fundacional, API protegida para cadastro, consulta, filtros, atualização e contagens agregadas de equipamentos, além de interface web de inventário/login com mapa dos equipamentos persistidos georreferenciados. A métrica de manutenção abre uma fila dos equipamentos explicitamente marcados, filtrável pelo inventário. Cadastro de agentes e predição ainda não foram implementados. As decisões e pendências estão em [docs/arquitetura.md](docs/arquitetura.md).

**Etapa atual (24/10):** núcleo do inventário e central (mapa, fila de manutenção e atualização SignalR) implementados. A stack Docker Compose e o comando de provisionamento inicial existem, mas Docker/PostgreSQL ainda precisam ser validados em runtime.

Os SDKs foram preparados em diretório local de usuário (fora do repositório): .NET SDK 10.0.402 e Node.js 24.20.0/npm 11.19.0. Os pacotes foram baixados de fontes oficiais e seus hashes conferidos. Para compilar no PowerShell desta máquina, foi necessário adicionar `FogoTools\dotnet` e `FogoTools\node-v24.20.0-win-x64` ao PATH da sessão; npm foi chamado via `npm.cmd` devido à política local que bloqueia scripts `.ps1`. PostgreSQL, Docker e `psql` não estão instalados nesta máquina; a stack Compose está definida, mas não pôde ser executada aqui.

No desenvolvimento, o Vite encaminha `/api` para `https://localhost:7001`. Se o perfil local da API usar outra porta, defina `FOGO_API_URL` antes de iniciar o frontend. O navegador precisa confiar no certificado HTTPS de desenvolvimento da API.

## Direção técnica

- Backend: ASP.NET Core Web API com C#.
- Frontend: React, TypeScript e Vite.
- Persistência: PostgreSQL com Entity Framework Core e migrations; PostgreSQL ainda precisa ser provisionado no ambiente.
- Mapa inicial: Leaflet e tiles OpenStreetMap; exibe somente coordenadas cadastradas, requer conexão externa para os tiles e mantém atribuição aos contribuidores.
- Autenticação: ASP.NET Core Identity, JWT de acesso mantido em memória, refresh token rotativo persistido somente como hash em cookie `HttpOnly` e proteção CSRF. A primeira conta é criada pelo comando interativo; nenhuma credencial de teste padrão é semeada.

No backend, o inventário está organizado em Controllers (HTTP), Application (validação e casos de uso), Repositories (consultas/persistência) e Identity (autenticação e `DbContext`). As migrations EF seguem no projeto da API e permanecem como fonte oficial do esquema.

## Stack local com Docker Compose

Com Docker Desktop/Engine e Docker Compose instalados, copie `.env.example` para `.env` e substitua `POSTGRES_PASSWORD` por um segredo aleatório URL-safe e `FOGO_JWT_KEY` por uma chave aleatória com pelo menos 32 bytes. Não compartilhe nem versione `.env`. Na raiz do projeto, execute `docker compose up --build` e abra `http://localhost:8080`.

O Compose inicia PostgreSQL, API e frontend; somente o frontend é publicado na porta local 8080. O Nginx encaminha `/api` à API pela rede privada do Compose. O volume `dados_postgres` preserva os dados entre reinicializações. A API aguarda o healthcheck do banco, aplica as migrations EF ao iniciar e expõe um healthcheck para que o frontend espere sua prontidão. Isso cria/atualiza o esquema, mas **não** insere equipamentos de demonstração nem cria usuários automaticamente.

Depois que a stack estiver pronta, crie a primeira conta de forma interativa com `docker compose exec -it api dotnet Fogo.Api.dll --provisionar-primeiro-usuario`. O comando pede e-mail e senha com confirmação; a senha não é exibida nem deve ser colocada em argumentos, `.env` ou logs. Só é permitido quando ainda não há usuários e não existem migrations pendentes. O e-mail é confirmado por esse procedimento local. A aplicação ainda não implementa papéis ou permissões diferenciadas entre usuários autenticados.

`docker compose down` para a stack sem apagar o volume. `docker compose down -v` também remove o banco persistido e é destrutivo. Esta configuração é para execução local; publicação exige HTTPS no proxy de borda e gestão de segredos própria, não o arquivo `.env`.

## Configuração local da API

Defina `FOGO_JWT_KEY` com pelo menos 32 bytes aleatórios usando um gerenciador de segredos ou variável de ambiente; nunca versione o valor. Configure `ConnectionStrings__PostgreSQL` com uma base PostgreSQL criada para este ambiente e, opcionalmente, `FOGO_ORIGINS` como lista de origens separadas por ponto e vírgula. Em desenvolvimento HTTP local, `FOGO_HTTPS_DEV=true` ativa cookies Secure; prefira HTTPS. A API falha ao iniciar se a chave ou conexão não estiver configurada.

O login não está aberto para cadastro público. O primeiro usuário deve ser criado pelo comando interativo documentado na seção Docker Compose; não há usuário nem senha padrão. Como a matriz de permissões continua pendente, essa conta não recebe um papel de autorização diferenciado.

Com a API em execução, iniciar `npm.cmd run dev` dentro de `frontend/` abre o portal em `http://localhost:5173`; o navegador solicita o cookie CSRF ao backend e API/hub SignalR são acessados pelos proxies Vite. Sem um usuário provisionado em banco, não será possível entrar ainda.

## Próximas etapas

1. Executar e validar a stack Compose com Docker; as migrations são aplicadas na inicialização e não criam registros de domínio nem contas.
2. Executar o comando de provisionamento seguro e validar sessões, migrations e CRUD contra PostgreSQL real.
3. Validar mapa, fila de manutenção e inventário de ponta a ponta; implementar cadastro de agentes se confirmado no escopo. O mapa limita consultas a 500 pontos por padrão e 1.000 no máximo.
4. Validar atualizações SignalR ponta a ponta com PostgreSQL e executar uma revisão da matriz de permissões; implementar cadastro de agentes se confirmado no escopo.
5. Avaliar fontes autorizadas e disponibilidade de histórico antes de integrações ou predição.
6. Fechar a entrega: revisão de segurança, instruções em clone limpo, validação do pacote e demonstração sem dados fictícios apresentados como reais.

## Validação executada

- `dotnet test backend/Fogo.Tests/Fogo.Tests.csproj --no-restore`: passou (28 testes; incluem autorização, recusa de negociação anônima no hub, descoberta dos Controllers, contrato JSON, validações, bootstrap CSRF, modelo EF e encaminhamento dos filtros do mapa e da fila de manutenção; não cobre publicação SignalR nem persistência/fluxos completos de sessão).
- `dotnet build backend/Fogo.Api/Fogo.Api.csproj --no-restore`: passou.
- Migration inicial do Identity e das sessões gerada em `backend/Fogo.Api/Identity/Migrations`; não foi aplicada porque PostgreSQL ainda não está instalado.
- Migration incremental de equipamentos também gerada. O espelho SQL para leitura da arquitetura do banco está em `backend/database/CREATE.sql` e não contém dados iniciais.
- `npm install` em `frontend/`: passou, 0 vulnerabilidades reportadas.
- `npm run build` em `frontend/`: passou (TypeScript e Vite).
- `npm audit` em `frontend/`: nenhum pacote vulnerável reportado.
- Docker Compose: arquivos de build e stack adicionados; execução ainda não validada porque Docker não está instalado neste ambiente.
- `frontend/`: mapa Leaflet construído para exibir localizações retornadas pela API; tiles OpenStreetMap exigem internet e a experiência visual integrada ainda precisa de validação com uma sessão e banco reais.
- SignalR: evento `equipamentoAlterado` após criação/atualização persistida; clientes autenticados recarregam inventário/indicadores e o mapa aberto. A entrega é transitória (sem outbox), portanto a reconexão também recarrega dados pela API.
- Auditoria NuGet dos projetos API e testes: nenhum pacote vulnerável reportado.
- PostgreSQL continua indisponível neste ambiente; migrations e `CREATE.sql` não foram aplicados nem validados em instância real.

Nenhuma integração ou previsão será apresentada como funcional sem validação. Dados demonstrativos, caso sejam adicionados, deverão estar claramente identificados como simulados.

## Uso de IA

GitHub Copilot foi utilizado como assistente de análise e implementação. O responsável pelo projeto deve revisar e compreender o código antes da entrega.
