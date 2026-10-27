# F.O.G.O.

Ferramenta Onisciente de Gestão e Operações — etapa bônus do desafio da Dsin.

## Estado

O F.O.G.O. reúne inventário de equipamentos com características livres, quadro de agentes, indicadores e mapa operacional. A aplicação oferece autenticação, filtros e atualização de telas via SignalR. Avisos e indicadores derivados da situação declarada e a projeção do calendário de manutenção apoiam a operação; não são telemetria nem previsão de incidentes. Consulte as premissas explícitas em [docs/arquitetura.md](docs/arquitetura.md).

**Estado em 27/10/2026:** o mapa combinado inclui indicadores derivados de situação, cores por estado de agentes, popup detalhado de equipamento, manutenção vencida/próxima, resumo por localidade e projeção determinística do calendário em 30 dias. Características específicas são cadastradas como pares livres nome/valor. Após autenticar, o mapa solicita permissão para centralizar na localização do navegador e reúne equipamentos e agentes georreferenciados. Uma consulta autenticada à API pública Overpass pode exibir objetos `highway=speed_camera` do OpenStreetMap; disponibilidade e precisão dependem da fonte colaborativa e da rede, e cada ponto tem link para o objeto original. A migration de demonstração inclui quatro agentes e três radares ilustrativos em Tupã e região, todos marcados como simulados — não representam pessoas ou radares confirmados. O sistema não prevê incidentes nem rastreia telemetria continuamente.

A migration de câmeras públicas cadastra 94 transmissões encontradas nos portais municipais de 15 cidades. Os links HLS foram conferidos nas páginas de origem. Uma lista colaborativa de coordenadas pode conter pontos de referência ou estimativas, que não comprovam a posição física da câmera; por isso, somente os cruzamentos explicitamente identificados como validados recebem coordenadas de câmera na migration, enquanto referências públicas e pendências permanecem sem marcador. O player carrega a transmissão somente após ação do usuário. O histórico diário de agentes contém apenas posições persistidas no cadastro ou em alterações de coordenadas; não há telemetria contínua e períodos sem registros são informados como indisponíveis.

A relação exata de cidade e ponto conforme indicado no portal está em [docs/cameras-publicas-localidades.csv](docs/cameras-publicas-localidades.csv), pronta para conferência geográfica. Coordenadas de locais de referência (praças, fachadas, parques ou trechos de avenida) ajudam a situar o nome, mas não localizam necessariamente a câmera; só use-as após confirmação explícita de que a câmera está naquele ponto. Os seis cruzamentos classificados como `VALIDADO` na lista fornecida foram adicionados por migration incremental.

Nesta máquina, os SDKs .NET 10.0.403 e Node.js 24.22.1/npm 11.19.1 estão em diretório local do usuário, fora do repositório. No PowerShell, pode ser necessário incluir `%LOCALAPPDATA%\FogoTools\dotnet` e `%LOCALAPPDATA%\FogoTools\node-v24.22.1-win-x64` no `PATH`; use `npm.cmd` se a política local bloquear scripts `.ps1`. O Docker Desktop foi iniciado; a stack e o PostgreSQL foram validados em runtime.

No desenvolvimento, o Vite encaminha `/api` para `https://localhost:7001`. Se o perfil local da API usar outra porta, defina `FOGO_API_URL` antes de iniciar o frontend. O navegador precisa confiar no certificado HTTPS de desenvolvimento da API.

## Direção técnica

- Backend: ASP.NET Core Web API com C#.
- Frontend: React, TypeScript e Vite.
- Persistência: PostgreSQL com Entity Framework Core e migrations; o Compose local mantém os dados no volume `dados_postgres`.
- Mapa inicial: Leaflet e tiles OpenStreetMap; mostra pontos cadastrados, dados demonstrativos rotulados e, quando disponível, radares mapeados no OpenStreetMap. Mantém atribuição e link à fonte; a geolocalização do navegador exige autorização e não é enviada nem persistida.
- Autenticação: ASP.NET Core Identity, JWT de acesso mantido em memória, refresh token rotativo persistido somente como hash em cookie `HttpOnly` e proteção CSRF. Cadastro exige confirmação por e-mail; recuperação de senha usa token temporário de uso único. As chaves de Data Protection persistem no volume `dados_protecao_chaves` no Compose.
- Agentes: cadastro/edição e consulta paginada, filtros por categoria e situação informada, indicadores simples e inclusão de pontos com coordenadas no mapa. A situação é manual; não existe rastreamento em tempo real da equipe.
- Características por equipamento: objeto JSON de até 30 propriedades textuais (nome até 60 caracteres, valor até 500), preservado em JSONB e mostrado no popup; não existe lista impositiva de campos por tipo.
- Painel operacional: “instalado” exclui apenas “ainda não instalado”; “indisponível” = instalados menos operacionais; disponibilidade declarada = operacionais/instalados, indefinida sem instalados. “Em atividade declarada” soma agentes Disponíveis e Em atendimento. Isso reflete cadastro manual, não presença nem uptime.
- Manutenção e localidades: alertas exibem registros imediatos declarados, datas vencidas (antes do dia UTC atual) e datas nos próximos sete dias, agrupados por bairro/cidade/UF. A projeção de 30 dias só soma manutenção programada por data/localidade; não estima falhas.
- Câmeras públicas: catálogo carregado pela API, player HLS sob demanda e marcadores somente para pontos com coordenadas verificadas. As localizações ausentes não são estimadas.
- Histórico de agentes: consulta por dia e desenho do trajeto somente a partir de posições gravadas na criação ou alteração de coordenadas.

No backend, o inventário está organizado em Controllers (HTTP), Application (validação e casos de uso), Repositories (consultas/persistência) e Identity (autenticação e `DbContext`). As migrations EF seguem no projeto da API e permanecem como fonte oficial do esquema.

## Stack local com Docker Compose

Com Docker Desktop/Engine e Docker Compose instalados, copie `.env.example` para `.env` e substitua `POSTGRES_PASSWORD` por um segredo aleatório URL-safe e `FOGO_JWT_KEY` por uma chave aleatória com pelo menos 32 bytes. Para habilitar cadastro, confirmação e recuperação por e-mail, configure também `FOGO_SMTP_HOST`, `FOGO_SMTP_PORT`, `FOGO_SMTP_USER`, `FOGO_SMTP_PASSWORD`, `FOGO_SMTP_FROM` e `FOGO_PUBLIC_URL`; credenciais SMTP são segredos e nunca devem ser versionadas. Use `FOGO_SMTP_SECURITY=StartTls` (padrão) ou `SslOnConnect` conforme o provedor; não use `None` fora de uma rede local confiável. Não compartilhe nem versione `.env`. Na raiz do projeto, execute `docker compose up --build` e abra `http://localhost:8080`.

O Compose inicia PostgreSQL, API e frontend; somente o frontend é publicado na porta de loopback local 8080. A configuração local permite acessar por `http://localhost:8080` ou `http://127.0.0.1:8080`; os dois endereços são origens diferentes e ambos estão na allow-list CORS. Como esta stack local usa HTTP, `FOGO_COOKIES_SEGUROS=false` permite que ambos os hosts de loopback recebam os cookies necessários; fora deste Compose o padrão continua exigindo cookies Secure, e qualquer publicação deve usar HTTPS e `FOGO_COOKIES_SEGUROS=true`. Se mudar `FOGO_ORIGINS` com a API já em execução, aplique a configuração com `docker compose up -d api` e atualize a página para obter um novo cookie CSRF. O Nginx encaminha `/api` à API pela rede privada do Compose. O volume `dados_postgres` preserva os dados entre reinicializações. A API aguarda o healthcheck do banco, aplica as migrations EF ao iniciar e expõe um healthcheck para que o frontend espere sua prontidão. A migration `AdicionarDadosDemonstracaoTupa` insere dados ilustrativos marcados como simulados; isso não cria usuários automaticamente.

Depois que a stack estiver pronta, crie a primeira conta de forma interativa com `docker compose exec -it api dotnet Fogo.Api.dll --provisionar-primeiro-usuario`. Para criar contas adicionais por procedimento local, use `docker compose exec -it api dotnet Fogo.Api.dll --provisionar-usuario`. Ambos os comandos pedem e-mail e senha com confirmação; a senha não é exibida nem deve ser colocada em argumentos, `.env` ou logs. O primeiro comando recusa se já houver usuário; o segundo exige acesso ao terminal da máquina/ambiente e ao banco. Esses comandos confirmam o e-mail pelo procedimento local. Também é possível criar conta pela tela inicial: o endereço precisa ser confirmado pelo link enviado por e-mail antes do primeiro login. “Esqueci minha senha” envia link de redefinição válido por duas horas; as respostas públicas não revelam se um e-mail está cadastrado. SMTP ausente ou indisponível não derruba a API, mas impede a entrega das mensagens. O cadastro público recebe as permissões atualmente disponíveis para usuários autenticados; a aplicação ainda não implementa papéis ou permissões diferenciadas. Configure SMTP e publique a aplicação somente com controle de acesso adequado; conceda acesso aos comandos de provisionamento apenas a operadores confiáveis.

`docker compose down` para a stack sem apagar o volume. `docker compose down -v` também remove o banco persistido e é destrutivo. Esta configuração é para execução local; publicação exige HTTPS no proxy de borda e gestão de segredos própria, não o arquivo `.env`.

## Configuração local da API

Defina `FOGO_JWT_KEY` com pelo menos 32 bytes aleatórios usando um gerenciador de segredos ou variável de ambiente; nunca versione o valor. Configure `ConnectionStrings__PostgreSQL` com uma base PostgreSQL criada para este ambiente e `FOGO_ORIGINS` como lista explícita de origens HTTP/HTTPS separadas por ponto e vírgula. Fora de Development, a API falha ao iniciar se a lista estiver ausente/vazia ou contiver URLs com caminho, consulta ou fragmento. Somente em Development, se omitida, é usada a origem local `http://localhost:5173`. Em desenvolvimento HTTP local, `FOGO_HTTPS_DEV=true` ativa cookies Secure; prefira HTTPS. `FOGO_COOKIES_SEGUROS` pode sobrescrever a política; mantenha `true` em qualquer ambiente servido por HTTPS e não o desative fora do Compose local de loopback. A API também falha ao iniciar se a chave ou conexão não estiver configurada. Respostas 403 no login indicam recusa da origem ou do token CSRF; confirme que `FOGO_ORIGINS` corresponde exatamente ao endereço usado no navegador e atualize a página para obter um novo cookie CSRF.

O cadastro público exige confirmação de e-mail e depende da configuração SMTP. A recuperação usa tokens protegidos por Data Protection e válidos por duas horas. Configure `FOGO_PUBLIC_URL` com a origem HTTPS pública exata (HTTP é aceito somente em loopback local); links não são montados a partir do cabeçalho `Host` da requisição. Os endpoints públicos validam origem e CSRF e têm limitação de requisições. O primeiro usuário ainda pode ser criado pelo comando interativo quando o SMTP não estiver configurado; não há usuário nem senha padrão. Como a matriz de permissões continua pendente, contas criadas publicamente recebem as permissões atualmente disponíveis para todos os usuários autenticados.

Com a API em execução, iniciar `npm.cmd run dev` dentro de `frontend/` abre o portal em `http://localhost:5173`; o navegador solicita o cookie CSRF ao backend e API/hub SignalR são acessados pelos proxies Vite. No portal, crie uma conta e confirme o e-mail; sem SMTP configurado, use o provisionamento local documentado acima.

## Próximas etapas

1. Iniciar o Docker Engine e executar a stack Compose; as migrations são aplicadas na inicialização, sem criar registros de domínio nem contas.
2. Criar o primeiro usuário pelo procedimento local seguro e validar login, sessões, migrations e operações com PostgreSQL real.
3. Fazer a revisão final de segurança/permissões e testar os fluxos da aplicação em navegador contra a stack integrada.
4. Confirmar com o responsável as regras provisórias de indicadores e janelas antes de tratá-las como política institucional.
5. Avaliar fontes autorizadas e histórico disponível antes de considerar previsão de incidentes, falhas ou demanda.
6. Preparar a entrega e demonstração com dados reais identificados ou dados simulados rotulados como tais.

## Validação executada

- `dotnet test backend/Fogo.Tests/Fogo.Tests.csproj --no-restore`: 78 testes passaram nesta revisão; incluem autenticação/autorização, proteção CSRF nos fluxos públicos de e-mail, inventário, agentes, métricas e projeções. Não substituem cobertura de entrega SMTP em um provedor real.
- `dotnet build backend/Fogo.Api/Fogo.Api.csproj --no-restore`: passou.
- A migration `AdicionarCamerasPublicasEHistoricoAgentes` foi gerada pelo EF Core com arquivo Designer e snapshot atualizado. O script SQL foi gerado sem aplicação ao banco e contém 94 inserções de câmeras; a validação do modelo não detectou alterações pendentes.
- Migration incremental de equipamentos também gerada. O espelho SQL para leitura da arquitetura do banco está em `backend/database/CREATE.sql` e não contém dados iniciais.
- `npm install` em `frontend/`: passou, 0 vulnerabilidades reportadas.
- `npm run build` em `frontend/`: passou (TypeScript e Vite).
- `npm audit` em `frontend/`: nenhum pacote vulnerável reportado.
- Docker Compose: stack reconstruída com `docker compose up -d --build`; PostgreSQL e API saudáveis, frontend em execução. A migration inseriu quatro agentes e três radares demonstrativos marcados como simulados. `/api/saude` respondeu `disponivel` e o portal respondeu HTTP 200 em `http://localhost:8080`.
- `frontend/`: mapa Leaflet é a primeira visão após autenticação, usa ícones distintos para equipamento e agente, pagina a consulta e exibe origem simulada. A localização do usuário depende de autorização do navegador e permanece na sessão; a consulta de radares Overpass é opcional e não foi possível confirmar resultados reais da região durante a validação (indisponibilidade externa). Tiles OpenStreetMap exigem internet.
- SignalR: evento `equipamentoAlterado` após criação/atualização persistida; clientes autenticados recarregam inventário/indicadores e o mapa aberto. A entrega é transitória (sem outbox), portanto a reconexão também recarrega dados pela API.
- Auditoria NuGet dos projetos API e testes: nenhum pacote vulnerável reportado.
- PostgreSQL Compose e aplicação automática das migrations foram validados nesta execução. `backend/database/CREATE.sql` continua sendo apenas o espelho SQL de referência, não foi aplicado diretamente.

Os testes do serviço OSM usam respostas simuladas; a disponibilidade de radares reais depende da Overpass API e não foi confirmada na região. Os dados adicionados pela migration são explicitamente demonstrativos, não oficiais.

## Uso de IA

GitHub Copilot foi utilizado como assistente de análise e implementação. O responsável pelo projeto deve revisar e compreender o código antes da entrega.

## Créditos das câmeras públicas

Os nomes e links das transmissões foram consultados nos portais públicos municipais da [Ao Vivo na Net](https://aovivonanet.servicesecurity.com.br/), serviço da Service Security. O catálogo é mantido nesta aplicação por migration; disponibilidade, conteúdo e continuidade dos streams dependem da publicação de origem.
