# Arquitetura — F.O.G.O.

## Estado do projeto

Em 26/10/2026, o F.O.G.O. tem autenticação, inventário de equipamentos, cadastro e quadro de agentes, mapa operacional e notificações de alteração via SignalR. O módulo preditivo e as integrações externas não estão implementados. Os builds e testes automatizados foram executados; a validação ponta a ponta com PostgreSQL está pendente porque o Docker Engine não está acessível neste ambiente.

O projeto usa ASP.NET Core 10, PostgreSQL e Entity Framework Core no backend; React, TypeScript e Vite no frontend. A arquitetura é um monólito modular, organizado em Controllers, Application Services, Repositories e `FogoDbContext`. Migrations EF são a fonte oficial do esquema. `backend/database/CREATE.sql` é um espelho SQL de referência, que deve ser mantido alinhado às migrations; ainda não foi aplicado a uma instância real.

## Módulos e comportamento

### Autenticação

A fundação usa ASP.NET Core Identity, JWT de curta duração, refresh token rotativo armazenado somente como hash, cookie `HttpOnly`, proteção CSRF e limitação de tentativas. O JWT fica em memória no frontend. Não existe cadastro público nem conta/senha padrão.

O primeiro usuário é criado pelo comando local interativo `--provisionar-primeiro-usuario`; contas adicionais podem ser criadas com `--provisionar-usuario`. Ambos solicitam e confirmam a senha sem exibi-la, requerem banco migrado e acesso operacional ao ambiente. Não há matriz de papéis/permissões: qualquer usuário autenticado tem acesso aos recursos protegidos. Os comandos de provisionamento devem ficar restritos a operadores confiáveis.

A API requer `FOGO_JWT_KEY` com pelo menos 32 bytes, `ConnectionStrings__PostgreSQL` e, fora de Development, uma lista explícita de origens em `FOGO_ORIGINS`. O Compose permite `http://localhost:8080` e `http://127.0.0.1:8080`; são origens distintas. Segredos não devem ser guardados no Git, enviados em mensagens ou incluídos em argumentos de comandos. Se algum segredo tiver sido compartilhado, ele deve ser rotacionado.

### Equipamentos e manutenção

Os endpoints `/api/equipamentos` exigem JWT e permitem cadastrar, consultar, listar com filtros e atualizar registros. A listagem é paginada. Indicadores exibem contagens persistidas por situação e registros explicitamente marcados para manutenção. A fila não calcula criticidade, prioridade ou vencimento automático; apresenta o que foi cadastrado.

Coordenadas são opcionais, mas latitude e longitude devem ser informadas juntas e dentro dos limites geográficos. Não foram adicionados registros de domínio demonstrativos.

### Agentes

A API autenticada `/api/agentes` permite cadastrar, consultar, editar, listar com paginação e filtrar por categoria profissional e situação. Os indicadores são contagens dos registros persistidos. A origem do registro distingue valores manuais, importados ou simulados.

A situação (Disponível, Em atendimento ou Indisponível) é informada manualmente. O sistema não determina presença, atividade ou disponibilidade real e não acompanha localização em tempo real. A localização opcional é uma coordenada cadastrada, não telemetria.

### Mapa e atualizações

O mapa combina equipamentos e agentes que tenham coordenadas completas; os símbolos de agente e equipamento são distintos. A consulta de equipamentos aceita filtros por tipo, situação e bairro, retorna até 500 pontos por padrão e permite no máximo 1.000. A consulta de agentes retorna até 1.000 pontos. As respostas incluem total e sinalização de truncamento, portanto pontos além do limite não são exibidos. O mapa usa tiles OpenStreetMap, requer acesso à internet e mantém atribuição aos contribuidores.

O hub autenticado `/hubs/equipamentos` publica eventos mínimos de alteração depois de uma gravação bem-sucedida. As telas consultam novamente a API em vez de receber dados completos pelo evento. A publicação é best-effort e não usa outbox; ao reconectar, a interface recarrega os dados. O fluxo visual com sessão real, PostgreSQL e WebSocket ainda precisa de validação integrada.

Nenhuma previsão ou integração de dados externos está disponível. Cada futura fonte exigirá avaliação de autorização, licença, atribuição, limites e atualização. Não devem ser extraídos streams protegidos nem inventadas probabilidades sem dados e método aprovados. Dados simulados devem ser identificados claramente.

## Execução local

A stack Compose contém PostgreSQL persistente, API e frontend servido por Nginx, com healthchecks e aplicação de migrations configurada na inicialização. O frontend é publicado em `http://localhost:8080`; a API não é publicada diretamente pelo Compose. O volume `dados_postgres` mantém os dados. `docker compose down` preserva o volume; `docker compose down -v` o apaga e é destrutivo.

Para iniciar, é necessário ter Docker Desktop/Engine em execução, copiar `.env.example` para `.env` e definir segredos fortes próprios antes de subir a stack. Em seguida, um operador cria a conta pelo comando de provisionamento documentado no README. Nenhuma conta ou equipamento é criado automaticamente.

Na máquina usada para esta validação, o CLI Docker existe, mas o daemon não responde (pipe do Docker Desktop Linux Engine indisponível). Assim, a stack não está ativa e `http://localhost:8080` é o endereço configurado, não um endereço atualmente acessível. Não foi possível executar migrations nem confirmar login com banco real. Os SDKs .NET 10.0.403 e Node.js 24.22.1/npm 11.19.1 estão preparados fora do repositório em `%LOCALAPPDATA%\FogoTools`; use `npm.cmd` no PowerShell se scripts `.ps1` estiverem bloqueados.

## Próximas verificações

1. Disponibilizar o Docker Engine sem remover ou recriar volumes existentes e validar `docker compose up --build`.
2. Confirmar aplicação das migrations em PostgreSQL, provisionar uma conta por canal local e verificar login, refresh, revogação e operações.
3. Testar mapa combinado, WebSocket e atualização de telas no navegador contra a stack integrada.
4. Revisar permissões e segurança antes de qualquer distribuição ou exposição fora do ambiente local.
5. Só então avaliar integrações autorizadas ou escopo de previsão com fontes e histórico adequados.

## Validação registrada

A configuração do Compose passou em `docker compose config --quiet`, que verifica os arquivos sem iniciar containers. A API compilou, 65 testes passaram, o frontend compilou e `npm audit` reportou zero vulnerabilidades. O Entity Framework não detectou alterações do modelo sem migration. A execução de `git diff --check` também passou. Esses resultados não substituem testes integrados.

Essas verificações não equivalem a teste de integração com PostgreSQL, teste de sessão bem-sucedida, inspeção visual em navegador ou teste de entrega SignalR a clientes conectados. O Docker Engine indisponível impede essas verificações neste ambiente.

## Decisões de escopo

A arquitetura ASP.NET Core, React e PostgreSQL é uma escolha do projeto, não uma exigência do desafio. O enunciado não define transições de situação, semântica de indisponibilidade, fórmula de disponibilidade, prioridade de manutenção, atividade de agentes ou matriz de permissões; por isso, essas regras não são inferidas pelo sistema.