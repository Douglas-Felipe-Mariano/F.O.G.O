# Arquitetura — F.O.G.O.

## Estado

Este documento registra decisões já confirmadas pelo responsável do projeto. O enunciado da etapa bônus foi recebido e descreve inventário de equipamentos e agentes, central operacional com mapa e indicadores, e módulo preditivo. A base contém API de saúde, autenticação fundacional, API protegida de inventário de equipamentos, consulta agregada por situação/manutenção e interface web de login, equipamentos e mapa geográfico. Cadastro de agentes e predição ainda não foram implementados.

**Etapa atual (24/10/2026):** Fase 2 (inventário/autorização) tem seu núcleo implementado; a Fase 3 inclui mapa baseado em coordenadas persistidas e fila filtrável de manutenção. A stack local Docker Compose foi definida, com aplicação opt-in de migrations ao iniciar a API. Ainda bloqueiam validação ponta a ponta: Docker/PostgreSQL não disponíveis neste ambiente e primeiro usuário administrador não provisionado.

## Direção confirmada

- Backend: ASP.NET Core Web API em C#.
- Persistência: PostgreSQL com Entity Framework Core; migrations são o mecanismo oficial de evolução do esquema.
- Frontend: React com TypeScript e Vite.
- Atualização em tempo real: SignalR quando houver eventos e contratos definidos.
- Mapa: Leaflet com tiles OpenStreetMap como solução inicial, exibindo apenas coordenadas persistidas e com atribuição visível.
- Street View: integração com Google Street View somente com configuração e avaliação de custos aplicáveis.
- API: Swagger/OpenAPI.
- Execução local: Docker Compose se for compatível com o ambiente e útil ao projeto.
- Stack local: Docker Compose com PostgreSQL persistente, API ASP.NET, frontend servido por Nginx e migrations EF aplicadas na inicialização quando `FOGO_APLICAR_MIGRATIONS=true`; o Compose não cria equipamento de demonstração nem usuário padrão.
- Organização: monólito modular, proporcional ao MVP; sem microserviços ou abstrações não justificadas.
- Organização interna atual: controllers recebem HTTP e delegam para Application Services; repositories encapsulam consultas EF Core; `FogoDbContext` mantém persistência/configuração; contratos e entidades do inventário ficam em `Inventario`. Autenticação permanece em handlers minimalistas até uma refatoração específica justificar controllers próprios.

## Persistência e nomenclatura

Tabelas e colunas de negócio devem usar nomes em português. Mapeie também as tabelas internas do ASP.NET Core Identity em português quando tecnicamente compatível, preservando relações e restrições necessárias.

Migrations do EF Core são a fonte oficial do esquema. Um SQL de criação será mantido como representação derivada e documentada do esquema efetivo; não se deve atualizá-lo independentemente de forma a permitir divergência silenciosa. Dados iniciais de desenvolvimento serão separados do esquema estrutural. Não executar operações destrutivas em bancos existentes sem autorização explícita.

Não criar uma entidade de municípios apenas para organizar o mapa. Registros geográficos devem ter localização própria. Um cadastro formal de municípios exige decisão posterior.

## Segurança

Autenticação e autorização fazem parte da fundação, não de uma etapa final. A direção confirmada inclui ASP.NET Core Identity, JWT de curta duração, refresh tokens rotacionáveis com hash persistido, revogação de sessões e autorização no backend. Tokens de longa duração não devem ser armazenados em `localStorage`; a implementação usa cookies `HttpOnly`/`Secure` configuráveis, proteção CSRF e `SameSite=Strict`.

Não haverá cadastro público irrestrito nem credenciais padrão versionadas. O primeiro administrador deve ser provisionado por procedimento seguro e documentado. A matriz detalhada de permissões dos perfis iniciais ainda precisa de confirmação antes da implementação das regras específicas.

## Estado da fundação de autenticação

- Implementado como base: ASP.NET Core Identity com tabelas mapeadas em português, JWT de acesso com emissor/audiência/assinatura/validade verificados, refresh token aleatório persistido somente como hash e rotacionado/revogado, cookies `HttpOnly`, `SameSite=Strict`, CSRF double-submit também no login, rate limit e fallback de autorização que exige usuário autenticado.
- Configuração obrigatória: `FOGO_JWT_KEY` (mínimo de 32 bytes) e `ConnectionStrings__PostgreSQL`; `FOGO_ORIGINS` é opcional. `FOGO_HTTPS_DEV=true` habilita cookies Secure em desenvolvimento HTTP. Segredos não devem ser versionados.
- Ainda não validado contra PostgreSQL real: migration/aplicação, persistência, rotação e revogação ponta a ponta. Os testes atuais cobrem rotas HTTP básicas e recusas, não um fluxo de sessão bem-sucedido. Esta base não equivale a auditoria de segurança.
- O primeiro administrador segue pendente; não existe cadastro público nem credencial padrão. A matriz de permissões continua pendente de confirmação.
- A migration inicial do Identity e das sessões está em `backend/Fogo.Api/Identity/Migrations`; foi gerada sem conexão ao banco e não foi aplicada. Testes de rotação, replay, revogação e persistência com PostgreSQL permanecem necessários.

## Dados, mapa e integrações

O frontend web conecta-se ao backend pelo proxy Vite (`FOGO_API_URL`, por padrão `https://localhost:7001`). Ele solicita cookie CSRF antes de iniciar/restaurar a sessão, mantém JWT somente em memória e usa cookies para refresh. Os controllers de equipamentos em `/api/equipamentos` exigem JWT e oferecem cadastro, consulta por identificador, listagem paginada com filtros opcionais por tipo/situação/bairro e necessidade de manutenção, e atualização completa. A métrica de manutenção abre essa listagem filtrada; os registros mostram o tipo e a próxima data declarados, sem atribuir prioridade ou criar regras de vencimento. O endpoint `GET /api/equipamentos/localizacoes` aceita os filtros de tipo/situação/bairro, retorna até 500 pontos por padrão, permite no máximo 1.000 por chamada e informa quantidade total/truncamento dentro do filtro aplicado; considera apenas equipamentos persistidos com coordenadas completas. A interface permite filtrar os marcadores, consultar detalhes e enquadrar sua extensão, usando tiles OpenStreetMap com atribuição; após salvar um equipamento com o mapa aberto, as localizações são atualizadas. Sem coordenadas não há marcador e o estado vazio explica isso. A camada de mapa ainda precisa ser verificada visualmente com sessão e banco reais. Os controllers delegam regras e fluxo à camada Application e operações de dados aos repositories. `/api/indicadores/equipamentos` retorna apenas totais persistidos por situação e quantidade que necessita manutenção, sem classificar disponibilidade ou criticidade. A validação exige endereço, situação e origem informada; coordenadas são opcionais, mas devem ser fornecidas em par e dentro dos limites geográficos. Não foram incluídos registros seed. A tabela e as regras de integridade estão nas migrations EF; `backend/database/CREATE.sql` é o SQL idempotente gerado pelas migrations para consulta, sem dados de domínio, e deve ser regenerado após mudanças no modelo. No Compose, o frontend é publicado em `localhost:8080`, a API e o PostgreSQL ficam na rede interna, e o volume do banco persiste entre reinicializações; a configuração serve para desenvolvimento local, não substitui proxy HTTPS e gestão de segredos de produção.

Dados reais, importados, cadastrados manualmente e simulados devem ser distinguíveis. Dados simulados não podem ser apresentados como eventos reais. O mapa exibe marcadores somente para registros existentes; ausência de marcador não significa ausência de equipamentos na região.

Cada fonte externa exige avaliação de formato, autorização, licença, atribuição, limites e frequência de atualização. A integração com Service Security ainda não está confirmada: não fazer scraping agressivo, contornar autenticação nem extrair streams protegidos. Sem integração autorizada, considerar somente cadastro de metadados e link para o portal original, se apropriado.

Previsões dependem de análise dos dados disponíveis e de aprovação da abordagem. Não escolher algoritmo nem inventar probabilidades sem base. Dados sintéticos devem ser identificados como tais.

## Implementação por etapas

1. SDKs .NET 10 e Node.js 24/npm 11 estão preparados fora do repositório; arquivos Compose existem, mas Docker/PostgreSQL ainda precisam ser instalados e a stack validada em runtime.
2. Completar a autenticação: provisionar com segurança o primeiro usuário e testar migrations, login, refresh, revogação e CRUD contra PostgreSQL real.
3. Validar o fluxo integrado de inventário, mapa e fila de manutenção com dados persistidos; decidir e implementar cadastro de agentes conforme o escopo aprovado.
4. Definir contratos e autorização do hub antes de implementar eventos SignalR após persistência de alterações reais.
5. Avaliar autorização/licença/fontes e histórico antes de integrações externas ou previsão.
6. Fechar a entrega com revisão de segurança, testes/builds, instruções verificadas em clone limpo e pacote/demonstração conforme os requisitos.

## Riscos e pendências

- O enunciado da etapa bônus foi recebido. Ele permite escolher tecnologia e resolver um ou mais módulos. A arquitetura .NET/React/PostgreSQL é uma decisão do projeto, não uma exigência do enunciado. A avaliação considera resolução, criatividade, efetividade, desempenho, qualidade técnica, experiência de uso e diferenciais.
- Em 24/10/2026, faltam três dias até o prazo de 27/10/2026 às 23h59. A implementação de toda a visão pretendida nesse período é de alto risco; priorizar critérios oficiais e fatias integradas, sem retirar requisitos silenciosamente.
- PostgreSQL, Docker e `psql` não estão instalados; os testes atuais não exigem acesso ao banco. Os arquivos Docker/Compose foram escritos, mas não foi possível validar sua execução ou formato com Docker nesta máquina. Os SDKs .NET 10.0.402 e Node.js 24.20.0/npm 11.19.0 foram baixados de fontes oficiais, hashes verificados e extraídos para `%LOCALAPPDATA%\FogoTools`, fora do repositório. Uma tentativa via WinGet não encontrou instalador aplicável; a instalação portátil local foi concluída sem modificar a política de execução do PowerShell. O npm foi executado por `npm.cmd`; `frontend/.npmrc` mantém scripts de instalação desabilitados por padrão.
- O enunciado apresenta exemplos de tipos, campos, situações e manutenção; não define transições de estado, semântica de indisponibilidade, fórmula de disponibilidade, janela de manutenção próxima, critério de criticidade, atividade do agente nem matriz de permissões.
- Viabilidade/licença das integrações, dados históricos e método preditivo dependem de avaliação. Nenhum dado real ou simulado foi incluído.

## Validação registrada

Validação atual: 27 testes passaram; builds da API e frontend passaram; `npm audit` não reportou vulnerabilidades. A suíte cobre autorização, descoberta dos Controllers, serialização camelCase, validações, bootstrap CSRF, modelo EF sem conexão e encaminhamento dos filtros do mapa e da fila de manutenção em teste unitário, mas não é substituta da integração PostgreSQL nem cobre sessão/CRUD bem-sucedidos. PostgreSQL não está instalado; migrations e `CREATE.sql` não foram aplicados a banco real. O mapa compila, porém seu render integrado e consumo de tiles não foram verificados no navegador com sessão autenticada.