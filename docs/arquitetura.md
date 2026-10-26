# Arquitetura — F.O.G.O.

## Estado do projeto

Em 26/10/2026, o F.O.G.O. tem autenticação, inventário de equipamentos, cadastro e quadro de agentes, mapa operacional inicial e notificações de alteração via SignalR. Há uma consulta pública opcional de radares mapeados no OpenStreetMap. O módulo preditivo não está implementado. A migration de demonstração insere agentes e radares explicitamente simulados em Tupã e cidades próximas.

O projeto usa ASP.NET Core 10, PostgreSQL e Entity Framework Core no backend; React, TypeScript e Vite no frontend. A arquitetura é um monólito modular, organizado em Controllers, Application Services, Repositories e `FogoDbContext`. Migrations EF são a fonte oficial do esquema e foram aplicadas pela API no PostgreSQL Compose local. `backend/database/CREATE.sql` é um espelho SQL de referência, que deve ser mantido alinhado às migrations; ele não foi aplicado diretamente.

## Módulos e comportamento

### Autenticação

A fundação usa ASP.NET Core Identity, JWT de curta duração, refresh token rotativo armazenado somente como hash, cookie `HttpOnly`, proteção CSRF e limitação de tentativas. O JWT fica em memória no frontend. Não existe cadastro público nem conta/senha padrão.

O primeiro usuário é criado pelo comando local interativo `--provisionar-primeiro-usuario`; contas adicionais podem ser criadas com `--provisionar-usuario`. Ambos solicitam e confirmam a senha sem exibi-la, requerem banco migrado e acesso operacional ao ambiente. Não há matriz de papéis/permissões: qualquer usuário autenticado tem acesso aos recursos protegidos. Os comandos de provisionamento devem ficar restritos a operadores confiáveis.

A API requer `FOGO_JWT_KEY` com pelo menos 32 bytes, `ConnectionStrings__PostgreSQL` e, fora de Development, uma lista explícita de origens em `FOGO_ORIGINS`. O Compose permite `http://localhost:8080` e `http://127.0.0.1:8080`; são origens distintas. Segredos não devem ser guardados no Git, enviados em mensagens ou incluídos em argumentos de comandos. Se algum segredo tiver sido compartilhado, ele deve ser rotacionado.

### Equipamentos e manutenção

Os endpoints `/api/equipamentos` exigem JWT e permitem cadastrar, consultar, listar com filtros e atualizar registros. A listagem é paginada. Indicadores exibem contagens persistidas por situação e registros explicitamente marcados para manutenção. A fila não calcula criticidade, prioridade ou vencimento automático; apresenta o que foi cadastrado.

Coordenadas são opcionais, mas latitude e longitude devem ser informadas juntas e dentro dos limites geográficos. A migration `AdicionarDadosDemonstracaoTupa` adiciona três radares e quatro agentes ilustrativos, identificados como `Simulado`; não são registros oficiais, nem confirmam pessoas ou equipamentos reais. Sua aplicação ocorre junto às migrations normais da API.

### Agentes

A API autenticada `/api/agentes` permite cadastrar, consultar, editar, listar com paginação e filtrar por categoria profissional e situação. Os indicadores são contagens dos registros persistidos. A origem do registro distingue valores manuais, importados ou simulados.

A situação (Disponível, Em atendimento ou Indisponível) é informada manualmente. O sistema não determina presença, atividade ou disponibilidade real e não acompanha localização em tempo real. A localização opcional é uma coordenada cadastrada, não telemetria. Os agentes da migration demonstrativa são identificados como simulados também no mapa.

### Mapa e atualizações

Após a autenticação, o mapa é a primeira visão operacional. Ele combina equipamentos e agentes com coordenadas completas, usando ícones distintos; os ícones dos equipamentos também indicam a situação. A interface busca todos os pontos em lotes de até 1.000 registros, por deslocamento, para não deixar localizações de fora por causa do limite de uma única chamada. Os endpoints devolvem total e sinalização de truncamento. O mapa usa tiles OpenStreetMap, requer acesso à internet e mantém atribuição aos contribuidores. A API autenticada `/api/radares/openstreetmap` consulta a Overpass API por objetos `highway=speed_camera` numa bounding box regional, mantém cache em memória e não persiste os resultados. A resposta pode estar vazia ou indisponível; os dados de OSM são colaborativos, podem estar desatualizados e não confirmam situação operacional. Cada marcador externo informa que a situação não foi verificada e liga ao objeto OSM. A geolocalização é solicitada pelo navegador, depende da permissão, serve apenas para centralizar o mapa na sessão atual e não é transmitida nem gravada. A autenticação continua obrigatória e registros sem coordenadas não aparecem.

O hub autenticado `/hubs/equipamentos` publica eventos mínimos de alteração depois de uma gravação bem-sucedida. As telas consultam novamente a API em vez de receber dados completos pelo evento. A publicação é best-effort e não usa outbox; ao reconectar, a interface recarrega os dados. O fluxo visual com sessão real, PostgreSQL e WebSocket ainda precisa de validação integrada.

Não há previsões. A consulta pública OSM depende da disponibilidade e limites da Overpass API; é mantida sob demanda com cache, sem coleta em segundo plano ou gravação no banco. Dados simulados são identificados claramente e nunca devem ser apresentados como oficiais.

## Execução local

A stack Compose contém PostgreSQL persistente, API e frontend servido por Nginx, com healthchecks e aplicação de migrations configurada na inicialização. O frontend é publicado em `http://localhost:8080`; a API não é publicada diretamente pelo Compose. O volume `dados_postgres` mantém os dados. `docker compose down` preserva o volume; `docker compose down -v` o apaga e é destrutivo.

Para iniciar, é necessário ter Docker Desktop/Engine em execução, copiar `.env.example` para `.env` e definir segredos fortes próprios antes de subir a stack. Em seguida, um operador cria a conta pelo comando de provisionamento documentado no README. A migration demonstrativa adiciona pontos simulados; nenhuma conta é criada automaticamente.

O Docker Desktop foi iniciado nesta máquina e a stack Compose está ativa em `http://localhost:8080`; banco e API passaram seus healthchecks. As migrations já estavam aplicadas. `/api/saude` respondeu `disponivel`, o login foi validado e os endpoints geográficos responderam com sucesso. Os SDKs .NET 10.0.403 e Node.js 24.22.1/npm 11.19.1 estão preparados fora do repositório em `%LOCALAPPDATA%\FogoTools`; use `npm.cmd` no PowerShell se scripts `.ps1` estiverem bloqueados.

## Próximas verificações

1. Cadastrar equipamentos e agentes com coordenadas reais/autorizadas e validar os respectivos marcadores no mapa.
2. Testar mapa combinado, WebSocket e atualização de telas no navegador contra a stack integrada.
3. Revisar permissões e segurança antes de qualquer distribuição ou exposição fora do ambiente local.
4. Revisar permissões e segurança antes de qualquer distribuição ou exposição fora do ambiente local.
5. Só então avaliar integrações autorizadas ou escopo de previsão com fontes e histórico adequados.

## Validação registrada

O Compose foi reconstruído; banco e API ficaram saudáveis, o frontend respondeu HTTP 200 e `/api/saude` retornou `disponivel`. A migration de demonstração foi aplicada. A API compilou, 68 testes passaram e o frontend compilou. A API externa Overpass não forneceu resultados verificáveis durante a validação; os testes do adaptador usam respostas simuladas.

O fluxo de login, o mapa interativo e a comunicação com a API haviam sido verificados anteriormente em navegador. Os marcadores demonstrativos estão persistidos e identificados como simulados; resta validar visualmente a nova tela após autenticação e a geolocalização com permissão concedida no navegador.

## Decisões de escopo

A arquitetura ASP.NET Core, React e PostgreSQL é uma escolha do projeto, não uma exigência do desafio. O enunciado não define transições de situação, semântica de indisponibilidade, fórmula de disponibilidade, prioridade de manutenção, atividade de agentes ou matriz de permissões; por isso, essas regras não são inferidas pelo sistema.