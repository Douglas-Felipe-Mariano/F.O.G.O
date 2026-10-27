# F.O.G.O.

**Ferramenta Onisciente de Gestão e Operações** — plataforma web de demonstração para organizar equipamentos urbanos, equipes e ocorrências em um mapa operacional.

## O que é

O F.O.G.O. reúne, em uma única interface autenticada:

- inventário de equipamentos, situação, manutenção e atributos flexíveis;
- cadastro e consulta de agentes, com posições cadastradas manualmente;
- mapa interativo com equipamentos, agentes, câmeras públicas, trânsito e acidentes em aberto;
- registro de acidentes com seleção do local diretamente no mapa, grau, impacto no trânsito e informação sobre feridos;
- indicadores, alertas e painel de inteligência preditiva PROMETHEUS;
- mapa de calor e localização do usuário mediante autorização do navegador.

### Limites importantes

- Os dados de trânsito pré-carregados são **demonstrativos e simulados**. O PROMETHEUS usa regras heurísticas para apoio à decisão; não é uma probabilidade calibrada, não é um modelo treinado nem uma previsão validada.
- A situação e a localização de agentes são dados cadastrados; a plataforma não faz rastreamento contínuo.
- A posição municipal aproximada de uma câmera, quando usada, não representa a localização exata do dispositivo. A disponibilidade dos streams depende da fonte pública.
- Registrar despacho ou acionamento de socorro apenas grava a solicitação na plataforma. Não contata agentes nem serviços de emergência.
- O mapa base e algumas fontes públicas precisam de acesso à internet.

## Como executar localmente

### Requisitos

- Docker Desktop ou Docker Engine com Docker Compose;
- terminal na pasta raiz do repositório.

### 1. Configurar segredos locais

Crie uma cópia local do arquivo de exemplo:

**PowerShell**

```powershell
Copy-Item .env.example .env
```

**Linux/macOS**

```sh
cp .env.example .env
```

Edite `.env` e defina:

- `POSTGRES_PASSWORD`: senha forte e exclusiva para o banco local;
- `FOGO_JWT_KEY`: chave aleatória com pelo menos 32 bytes.

Não versione nem compartilhe `.env` ou seus valores. O e-mail via SMTP é opcional para a inicialização; sem SMTP, o provisionamento local abaixo permite criar a conta inicial. Para cadastro e recuperação de senha por e-mail, configure as variáveis SMTP existentes em `.env.example`.

### 2. Iniciar a plataforma

Na raiz do repositório:

```sh
docker compose up --build -d
```

O Compose inicia PostgreSQL, API e frontend. A API aplica as migrations automaticamente, e o banco conserva os dados no volume `dados_postgres`. Aguarde os serviços ficarem prontos e abra:

**http://localhost:8080**

Para conferir o estado dos serviços:

```sh
docker compose ps
```

O endpoint de saúde da API é `http://localhost:8080/api/saude`.

### 3. Criar a primeira conta

Não há conta padrão. Com a stack em execução, rode no terminal interativo:

```sh
docker compose exec -it api dotnet Fogo.Api.dll --provisionar-primeiro-usuario
```

Informe o e-mail e digite uma senha de pelo menos 12 caracteres quando solicitado. A senha não aparece enquanto é digitada. O procedimento local confirma o e-mail da conta. Depois, entre pela página inicial em `http://localhost:8080`.

Para provisionar outra conta localmente, use `--provisionar-usuario` no lugar de `--provisionar-primeiro-usuario`. Guarde o acesso ao terminal e aos comandos de provisionamento apenas para pessoas autorizadas. A aplicação ainda não diferencia permissões entre usuários autenticados.

### 4. Encerrar ou reiniciar

Parar os serviços sem apagar o banco:

```sh
docker compose down
```

Iniciá-los novamente:

```sh
docker compose up -d
```

**Atenção:** `docker compose down -v` remove também os volumes, inclusive os dados persistidos do banco. Use somente se quiser apagar os dados locais.

## Desenvolvimento e verificações

- Backend: ASP.NET Core Web API, C# e PostgreSQL com Entity Framework Core.
- Frontend: React, TypeScript e Vite.
- Mapa: Leaflet e OpenStreetMap.

Com o .NET 10 instalado, rode os testes do backend a partir da raiz:

```sh
dotnet test backend/Fogo.Tests/Fogo.Tests.csproj
```

Para compilar o frontend:

```sh
cd frontend
npm install
npm run build
```

No PowerShell do Windows, use `npm.cmd` caso a política de execução bloqueie `npm`:

```powershell
Push-Location frontend
npm.cmd install
npm.cmd run build
Pop-Location
```

## Execução fora do ambiente local

O Compose deste repositório é voltado à execução local: a porta web fica vinculada ao loopback e a configuração usa HTTP. Não exponha esta configuração diretamente à internet. Uma implantação acessível externamente requer HTTPS, gestão segura de segredos e configuração adequada de domínio, cookies e e-mail. Configure acesso ao sistema de acordo com o ambiente e considere que usuários autenticados têm as mesmas permissões.
