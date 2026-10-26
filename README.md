# F.O.G.O.

Ferramenta Onisciente de Gestão e Operações — etapa bônus do desafio da Dsin.

## Estado

O F.O.G.O. reúne um inventário de equipamentos, um quadro de agentes e um mapa operacional. A aplicação oferece autenticação, cadastro e consulta de registros, filtros, indicadores e atualização de telas via SignalR. O quadro de manutenção exibe equipamentos marcados para manutenção; não calcula prioridade nem prevê falhas. As decisões, limites e validações conhecidas estão em [docs/arquitetura.md](docs/arquitetura.md).

**Estado em 26/10/2026:** inventário de equipamentos, cadastro de agentes, mapa combinado e notificações SignalR estão implementados. O módulo preditivo não está disponível. Os builds e testes automatizados podem ser executados localmente; a validação com PostgreSQL e a stack completa segue bloqueada porque o Docker Engine não está acessível neste ambiente.

Nesta máquina, os SDKs .NET 10.0.403 e Node.js 24.22.1/npm 11.19.1 estão em diretório local do usuário, fora do repositório. No PowerShell, pode ser necessário incluir `%LOCALAPPDATA%\FogoTools\dotnet` e `%LOCALAPPDATA%\FogoTools\node-v24.22.1-win-x64` no `PATH`; use `npm.cmd` se a política local bloquear scripts `.ps1`. O Docker CLI está instalado, mas o daemon do Docker Desktop não está acessível. PostgreSQL não foi validado em runtime.

No desenvolvimento, o Vite encaminha `/api` para `https://localhost:7001`. Se o perfil local da API usar outra porta, defina `FOGO_API_URL` antes de iniciar o frontend. O navegador precisa confiar no certificado HTTPS de desenvolvimento da API.

## Direção técnica

- Backend: ASP.NET Core Web API com C#.
- Frontend: React, TypeScript e Vite.
- Persistência: PostgreSQL com Entity Framework Core e migrations; PostgreSQL ainda precisa ser provisionado no ambiente.
- Mapa inicial: Leaflet e tiles OpenStreetMap; exibe somente coordenadas cadastradas, requer conexão externa para os tiles e mantém atribuição aos contribuidores.
- Autenticação: ASP.NET Core Identity, JWT de acesso mantido em memória, refresh token rotativo persistido somente como hash em cookie `HttpOnly` e proteção CSRF. A primeira conta é criada pelo comando interativo; nenhuma credencial de teste padrão é semeada.
- Agentes: cadastro/edição e consulta paginada, filtros por categoria e situação informada, indicadores simples e inclusão de pontos com coordenadas no mapa. A situação é manual; não existe rastreamento em tempo real da equipe.

No backend, o inventário está organizado em Controllers (HTTP), Application (validação e casos de uso), Repositories (consultas/persistência) e Identity (autenticação e `DbContext`). As migrations EF seguem no projeto da API e permanecem como fonte oficial do esquema.

## Stack local com Docker Compose

Com Docker Desktop/Engine e Docker Compose instalados, copie `.env.example` para `.env` e substitua `POSTGRES_PASSWORD` por um segredo aleatório URL-safe e `FOGO_JWT_KEY` por uma chave aleatória com pelo menos 32 bytes. Não compartilhe nem versione `.env`. Na raiz do projeto, execute `docker compose up --build` e abra `http://localhost:8080`.

O Compose inicia PostgreSQL, API e frontend; somente o frontend é publicado na porta local 8080. A configuração local permite acessar por `http://localhost:8080` ou `http://127.0.0.1:8080`; os dois endereços são origens diferentes e ambos estão na allow-list CORS. Se mudar `FOGO_ORIGINS` com a API já em execução, aplique a configuração com `docker compose up -d api` e atualize a página para obter um novo cookie CSRF. O Nginx encaminha `/api` à API pela rede privada do Compose. O volume `dados_postgres` preserva os dados entre reinicializações. A API aguarda o healthcheck do banco, aplica as migrations EF ao iniciar e expõe um healthcheck para que o frontend espere sua prontidão. Isso cria/atualiza o esquema, mas **não** insere equipamentos de demonstração nem cria usuários automaticamente.

Depois que a stack estiver pronta, crie a primeira conta de forma interativa com `docker compose exec -it api dotnet Fogo.Api.dll --provisionar-primeiro-usuario`. Para criar contas adicionais, use `docker compose exec -it api dotnet Fogo.Api.dll --provisionar-usuario`. Ambos os comandos pedem e-mail e senha com confirmação; a senha não é exibida nem deve ser colocada em argumentos, `.env` ou logs. O primeiro comando recusa se já houver usuário; o segundo exige acesso ao terminal da máquina/ambiente e ao banco e pode ser usado para contas posteriores. Ambos exigem migrations aplicadas e confirmam o e-mail pelo procedimento local. Não há cadastro público. A aplicação ainda não implementa papéis ou permissões diferenciadas entre usuários autenticados; conceda acesso aos comandos de provisionamento somente a operadores confiáveis.

`docker compose down` para a stack sem apagar o volume. `docker compose down -v` também remove o banco persistido e é destrutivo. Esta configuração é para execução local; publicação exige HTTPS no proxy de borda e gestão de segredos própria, não o arquivo `.env`.

## Configuração local da API

Defina `FOGO_JWT_KEY` com pelo menos 32 bytes aleatórios usando um gerenciador de segredos ou variável de ambiente; nunca versione o valor. Configure `ConnectionStrings__PostgreSQL` com uma base PostgreSQL criada para este ambiente e `FOGO_ORIGINS` como lista explícita de origens HTTP/HTTPS separadas por ponto e vírgula. Fora de Development, a API falha ao iniciar se a lista estiver ausente/vazia ou contiver URLs com caminho, consulta ou fragmento. Somente em Development, se omitida, é usada a origem local `http://localhost:5173`. Em desenvolvimento HTTP local, `FOGO_HTTPS_DEV=true` ativa cookies Secure; prefira HTTPS. A API também falha ao iniciar se a chave ou conexão não estiver configurada. Respostas 403 no login indicam recusa da origem ou do token CSRF; confirme que `FOGO_ORIGINS` corresponde exatamente ao endereço usado no navegador e atualize a página para obter um novo cookie CSRF.

O login não está aberto para cadastro público. O primeiro usuário deve ser criado pelo comando interativo documentado na seção Docker Compose; não há usuário nem senha padrão. Como a matriz de permissões continua pendente, essa conta não recebe um papel de autorização diferenciado.

Com a API em execução, iniciar `npm.cmd run dev` dentro de `frontend/` abre o portal em `http://localhost:5173`; o navegador solicita o cookie CSRF ao backend e API/hub SignalR são acessados pelos proxies Vite. Sem um usuário provisionado em banco, não será possível entrar ainda.

## Próximas etapas

1. Iniciar o Docker Engine e executar a stack Compose; as migrations são aplicadas na inicialização, sem criar registros de domínio nem contas.
2. Criar o primeiro usuário pelo procedimento local seguro e validar login, sessões, migrations e operações com PostgreSQL real.
3. Fazer a revisão final de segurança/permissões e testar os fluxos da aplicação em navegador contra a stack integrada.
4. Avaliar fontes autorizadas e histórico disponível antes de considerar integrações externas ou previsões.
5. Preparar a entrega e demonstração com dados reais identificados ou dados simulados rotulados como tais.

## Validação executada

- `dotnet test backend/Fogo.Tests/Fogo.Tests.csproj --no-restore`: 65 testes passaram na última execução; incluem autenticação/autorização básica, inventário, serviços de agentes e limites das localizações. Não substituem integração contra PostgreSQL nem cobrem sessão bem-sucedida completa.
- `dotnet build backend/Fogo.Api/Fogo.Api.csproj --no-restore`: passou.
- Migration inicial do Identity e das sessões gerada em `backend/Fogo.Api/Identity/Migrations`; não foi aplicada porque PostgreSQL ainda não está instalado.
- Migration incremental de equipamentos também gerada. O espelho SQL para leitura da arquitetura do banco está em `backend/database/CREATE.sql` e não contém dados iniciais.
- `npm install` em `frontend/`: passou, 0 vulnerabilidades reportadas.
- `npm run build` em `frontend/`: passou (TypeScript e Vite).
- `npm audit` em `frontend/`: nenhum pacote vulnerável reportado.
- Docker Compose: configuração validada com `docker compose config --quiet`; inicialização não validada porque o Docker Engine não está acessível neste ambiente.
- `frontend/`: mapa Leaflet construído para exibir localizações retornadas pela API; tiles OpenStreetMap exigem internet e a experiência visual integrada ainda precisa de validação com uma sessão e banco reais.
- SignalR: evento `equipamentoAlterado` após criação/atualização persistida; clientes autenticados recarregam inventário/indicadores e o mapa aberto. A entrega é transitória (sem outbox), portanto a reconexão também recarrega dados pela API.
- Auditoria NuGet dos projetos API e testes: nenhum pacote vulnerável reportado.
- PostgreSQL continua sem validação neste ambiente; migrations e `CREATE.sql` não foram aplicados em uma instância real.

Nenhuma integração ou previsão será apresentada como funcional sem validação. Dados demonstrativos, caso sejam adicionados, deverão estar claramente identificados como simulados.

## Uso de IA

GitHub Copilot foi utilizado como assistente de análise e implementação. O responsável pelo projeto deve revisar e compreender o código antes da entrega.
