# PomoGarden — API

Backend do PomoGarden: Pomodoro gamificado (cadastro, login, temporizador, tarefas vinculadas a pomodoros,
progresso de XP/nível/sementes/streak e conquistas). O frontend (React + TS + Vite) vive num repositório
irmão: [pomodoro-ui](https://github.com/saulomedeirosguerreiro/pomodoro-ui).

## Tecnologias utilizadas

- **.NET 8 / C#**, ASP.NET Core **Minimal API**
- **Clean Architecture**: `Pomodoro.Domain` → `Pomodoro.Application` → `Pomodoro.Infrastructure` → `Pomodoro.Api`
- **EF Core + PostgreSQL** (persistência; schema 100% via Migrations, sem SQL manual)
- **JWT** (`Microsoft.AspNetCore.Authentication.JwtBearer`) + **BCrypt.Net-Next** (hash de senha)
- **FluentValidation** (validação de entrada)
- **xUnit + FluentAssertions 7.x + NSubstitute + WebApplicationFactory + Testcontainers** (testes), **coverlet** (cobertura)

## Como instalar as dependências

Pré-requisitos: **.NET SDK 8** e **Docker** (Postgres local via Docker Compose; a suíte de testes também
sobe containers Postgres via Testcontainers — ver "Como executar os testes").

```bash
dotnet tool restore   # instala dotnet-ef e reportgenerator (versões fixas no repo)
dotnet restore
```

## Como configurar as variáveis de ambiente

Lidas via ambiente (ou `--Chave:Sub=valor` na linha de comando):

| Variável | Obrigatória | Exemplo |
|---|---|---|
| `JWT_SECRET` (ou `Jwt:Secret`) | **Sim** — a API não sobe sem ela | uma string aleatória com 32+ caracteres |
| `ConnectionStrings__Default` | **Sim** — a API não sobe sem ela, nem em desenvolvimento | `Host=localhost;Port=5433;Database=pomodoro;Username=pomodoro;Password=<a senha definida em docker-compose.yml>` |
| `CORS_ORIGIN` (ou `Cors:AllowedOrigin`) | Recomendada — precisa bater com a origem do frontend | `http://localhost:5173` |

PowerShell:
```powershell
$env:JWT_SECRET = "troque-por-uma-string-aleatoria-de-32-caracteres-ou-mais"
```

Bash:
```bash
export JWT_SECRET="troque-por-uma-string-aleatoria-de-32-caracteres-ou-mais"
```

> Por segurança, nenhuma senha de banco fica hardcoded em `appsettings.Development.json` (só
> `Cors:AllowedOrigin`, que não é segredo, já vem pronto lá). `ConnectionStrings__Default` e
> `JWT_SECRET` precisam ser definidos manualmente — de propósito (RNF-02): a API deve recusar subir
> sem esses dois configurados.

## Como configurar o banco de dados

PostgreSQL via Docker Compose, schema via EF Core Migrations (sem SQL manual). A senha nunca fica em
texto puro em nenhum arquivo versionado — `docker-compose.yml` lê `POSTGRES_PASSWORD` de um `.env`
local (não versionado; o Docker Compose carrega `.env` automaticamente):

```bash
cp .env.example .env    # depois edite .env e defina POSTGRES_PASSWORD com uma senha sua, forte
docker compose up -d postgres
export ConnectionStrings__Default="Host=localhost;Port=5433;Database=pomodoro;Username=pomodoro;Password=<a mesma senha do .env>"
dotnet ef database update --project src/Pomodoro.Infrastructure --startup-project src/Pomodoro.Api
```

PowerShell:
```powershell
Copy-Item .env.example .env    # depois edite .env e defina POSTGRES_PASSWORD com uma senha sua, forte
docker compose up -d postgres
$env:ConnectionStrings__Default = "Host=localhost;Port=5433;Database=pomodoro;Username=pomodoro;Password=<a mesma senha do .env>"
dotnet ef database update --project src/Pomodoro.Infrastructure --startup-project src/Pomodoro.Api
```

Essa mesma variável precisa estar exportada (ou definida via `dotnet run --project src/Pomodoro.Api
-- --ConnectionStrings:Default="..."`) antes de `dotnet run`/`dotnet run -- seed` — diferente de antes,
`appsettings.Development.json` não traz mais uma connection string padrão.

> **Porta 5433, não 5432:** o `docker-compose.yml` deste repo publica o Postgres do container na porta
> 5433 do host porque a máquina de desenvolvimento original já tinha um PostgreSQL nativo ocupando a
> 5432. Se a sua máquina não tiver esse conflito, troque `"5433:5432"` por `"5432:5432"` no
> `docker-compose.yml` e ajuste a porta nas connection strings acima.

Isso cria as tabelas `users`, `pomodoros`, `tasks`, `user_achievements` e `guest_imports` no banco
`pomodoro` do container.

## Usuário de teste

Depois de migrar o banco, rode o seed (idempotente — pode rodar quantas vezes quiser):

```bash
dotnet run --project src/Pomodoro.Api -- seed
```

Credenciais:
- **E-mail:** `teste@pomodoro.com`
- **Senha:** `Teste123`

O seed cria um usuário com dados prontos para ver as telas gamificadas populadas:
- Sessões de foco concluídas em **3 dias seguidos** (streak > 0) mais uma pausa curta a cada dia e uma sessão interrompida de exemplo;
- **3 tarefas**, uma em cada status (`a_fazer`, `em_curso`, `feito`) — a `em_curso` é a "tarefa em foco";
- **3 conquistas já desbloqueadas** ("Primeira Semente", "Constância de 3 dias" e "Primeira Colheita"),
  avaliadas pelo mesmo caminho usado em produção (nada é inserido "na mão").

## Como executar a API

```bash
dotnet run --project src/Pomodoro.Api
```

A API sobe em `http://localhost:5134` (perfil `http` de `launchSettings.json`; ajustável via
`ASPNETCORE_URLS`/`--urls`). Endpoints:

```
POST   /api/auth/register
POST   /api/auth/login                  (retorna access token no corpo + refresh token em cookie httpOnly)
POST   /api/auth/refresh                (lê o cookie; rotaciona o refresh token e emite um novo access token)
POST   /api/auth/logout                 (revoga o refresh token no servidor e limpa o cookie)
GET    /api/users/me                    (autenticado)
DELETE /api/users/me                    (autenticado; exige a senha atual no corpo — exclusão definitiva, em cascata)
GET    /api/users/me/progress           (autenticado, ?tz=<IANA>; default America/Sao_Paulo)

POST   /api/pomodoros                   (autenticado; aceita taskId opcional, só para type=foco)
GET    /api/pomodoros                   (autenticado, ?limit=&offset=)
GET    /api/pomodoros/{id}              (autenticado)

GET    /api/tasks                       (autenticado, ?status=a_fazer|em_curso|feito)
POST   /api/tasks                       (autenticado)
PATCH  /api/tasks/{id}                  (autenticado)
PATCH  /api/tasks/{id}/status           (autenticado; status=em_curso tira o foco de qualquer outra tarefa)
DELETE /api/tasks/{id}                  (autenticado)

GET    /api/achievements                (autenticado — catálogo completo + status do usuário)
```

## Como executar os testes

100% de cobertura de linha/branch/método (Domain, Application, Infrastructure, Api). A única
exceção documentada são 3 linhas do branch `dotnet run -- seed` em `Program.cs`, que só roda via CLI e não
faz parte do pipeline HTTP exercitado pelos testes de integração.

> **Docker é pré-requisito para `dotnet test`:** os testes de `Pomodoro.Infrastructure.Tests` e
> `Pomodoro.Api.Tests` sobem um container Postgres via Testcontainers (um por processo de teste,
> independente do container do `docker-compose.yml` usado para desenvolvimento) para validar o schema
> real da migration contra um Postgres de verdade. Sem o Docker em execução, esses dois projetos de
> teste falham ao tentar iniciar o container.

```bash
dotnet test Pomodoro.sln
```

Com relatório de cobertura mesclado (requer `dotnet tool restore` antes, uma vez):
```bash
dotnet test Pomodoro.sln --settings coverage.runsettings --collect:"XPlat Code Coverage" --results-directory ./coverage-tmp
dotnet reportgenerator -reports:"coverage-tmp/*/coverage.cobertura.xml" -targetdir:coverage-report -reporttypes:"TextSummary"
cat coverage-report/Summary.txt
```

## Deploy em produção (Render, via Docker)

O Render não roda .NET/C# nativamente — só builda e executa imagens Docker. Por isso este repo traz
um `Dockerfile` (multi-stage: SDK para publish, `aspnet:8.0` para runtime) na raiz de `backend/`.

No Render, ao criar o **Web Service**:
- **Root Directory:** `backend`
- **Runtime:** Docker (detectado automaticamente pelo `Dockerfile` na raiz do diretório acima)

Variáveis de ambiente a configurar no painel do Render (aba *Environment*):

| Variável | Obrigatória | Observação |
|---|---|---|
| `JWT_SECRET` | **Sim** | string aleatória com 32+ caracteres — gere uma só para produção, diferente da de dev |
| `ConnectionStrings__Default` | **Sim** | ver conversão de formato abaixo |
| `CORS_ORIGIN` | Recomendada | URL pública do frontend em produção (ex.: `https://meu-app.vercel.app`) |

`PORT` **não precisa ser configurada manualmente** — o Render injeta essa variável sozinho em runtime,
e o `ENTRYPOINT` do `Dockerfile` já monta `ASPNETCORE_URLS` a partir dela automaticamente. `ASPNETCORE_ENVIRONMENT=Production`
também já vem fixado na imagem.

### Connection string: Postgres gerenciado entrega URI, não `Host=...`

Provedores de Postgres gerenciado (Render Postgres, Supabase, Neon etc.) fornecem a connection string
no formato URI — `postgres://usuario:senha@host:porta/banco` — mas o Npgsql (usado por este projeto)
espera o formato chave=valor. Converta manualmente antes de colar em `ConnectionStrings__Default`:

```
postgres://pomodoro:SENHA@dpg-xxxxx.oregon-postgres.render.com/pomodoro_db
```
vira
```
Host=dpg-xxxxx.oregon-postgres.render.com;Port=5432;Database=pomodoro_db;Username=pomodoro;Password=SENHA;Ssl Mode=Require;Trust Server Certificate=true
```

(`Ssl Mode=Require` porque bancos gerenciados exigem TLS; sem isso o Npgsql recusa a conexão.)

### Migrations não rodam dentro do container publicado

A imagem final usa `aspnet:8.0` (runtime), que não inclui o SDK nem a ferramenta `dotnet-ef` — então
`dotnet ef database update` não funciona de dentro do container em produção. Rode a migration a partir
da sua máquina (ou de um passo de CI com o SDK instalado), apontando para o banco de produção, **antes**
de o primeiro deploy receber tráfego:

```bash
dotnet ef database update --project src/Pomodoro.Infrastructure --startup-project src/Pomodoro.Api \
  --connection "Host=dpg-xxxxx.oregon-postgres.render.com;Port=5432;Database=pomodoro_db;Username=pomodoro;Password=SENHA;Ssl Mode=Require;Trust Server Certificate=true"
```

### Testar a imagem localmente antes de subir

```bash
docker build -t pomodoro-api -f Dockerfile .
docker run --rm -p 8080:8080 \
  -e PORT=8080 \
  -e JWT_SECRET="uma-chave-de-teste-com-32-caracteres-ou-mais" \
  -e ConnectionStrings__Default="Host=host.docker.internal;Port=5433;Database=pomodoro;Username=pomodoro;Password=<a senha do seu .env local>" \
  -e CORS_ORIGIN="http://localhost:5173" \
  pomodoro-api
```

## Notas de arquitetura e decisões

- **Isolamento entre usuários:** o `user_id` nunca vem do corpo da requisição — sempre do claim `sub` do token JWT.
- **Erros:** envelope único `{ "error": { "code", "message", "fields"? } }`, com `422` (validação), `401` (não autenticado), `404` (não encontrado ou de outro dono), `409` (conflito) e `500` (genérico, sem stack trace — detalhe só no log do servidor).
- **Antifraude no registro (`POST /api/pomodoros`):** rejeita `started_at` no futuro, `completed_at` mais de 60s no futuro, e qualquer sobreposição de intervalo com outra sessão do mesmo usuário — sessões de outros usuários nunca entram nessa checagem.
- **Datas:** gravadas em UTC no banco. Para streak e resumo do dia, o cliente manda `tz` (IANA, ex. `America/Sao_Paulo`) e o servidor converte antes de agrupar por dia — sem `tz`, o default é `America/Sao_Paulo`; um `tz` inválido retorna `422`.
- **XP, Nível, Sementes e Streak** são uma **projeção calculada no servidor** a partir do histórico de `pomodoros` — não existe tabela de saldo. Fórmulas (`Pomodoro.Domain.Services.ProgressRules`): foco concluído = 25 XP, pausa concluída = 5 XP, sessão interrompida = 0 XP; nível sobe a cada `200 × nível atual` XP; sementes = 15 por foco concluído + 10 de bônus por pausa longa concluída (fecha um ciclo). Streak conta dias seguidos com ao menos um foco concluído, terminando hoje ou ontem — só "quebra" quando um dia inteiro passa sem foco.
- **Tarefas:** entidade de backend (não localStorage), com vínculo opcional `pomodoros.task_id` (`ON DELETE SET NULL` — apagar uma tarefa nunca apaga o histórico de pomodoros). O vínculo só é aceito para `type=foco`, para uma tarefa do mesmo usuário e que ainda não esteja `feito`; qualquer violação devolve a mesma mensagem genérica (não revela a existência de tarefas de outro usuário). No máximo uma tarefa por usuário fica `em_curso` por vez — marcar uma nova tira a anterior do foco automaticamente.
- **Conquistas:** catálogo fixo no código (`Pomodoro.Domain.Achievements.AchievementCatalog`, 10 itens), avaliado no servidor a cada sessão registrada e a cada tarefa concluída. A avaliação é idempotente (nunca grava a mesma conquista duas vezes) e **retroativa** (quem já tinha histórico recebe as conquistas na primeira consulta a `GET /api/achievements`).
- **Exclusão de conta (`DELETE /api/users/me`):** exige a senha atual no corpo da requisição (reautenticação, não só o token) antes de apagar — sem isso, retorna `401`. É definitiva: o usuário é removido e, por cascata de FK, todas as suas sessões, tarefas, conquistas e refresh tokens vão junto. O **access token** continua sem lista de revogação — um JWT emitido antes da exclusão permanece criptograficamente válido até expirar (24h), mas qualquer chamada que precise ler o usuário no banco passa a responder `404`. O **refresh token**, por ser persistido, já não sofre desse problema: a cascata de FK apaga as linhas em `refresh_tokens`, então `POST /api/auth/refresh` para de funcionar imediatamente após a exclusão.
- **Refresh token (`POST /api/auth/refresh`, `POST /api/auth/logout`):** access token JWT (24h) + refresh token opaco (30 dias por padrão, `Jwt:RefreshTokenExpirationDays`), persistido como hash SHA-256 em `refresh_tokens` e entregue ao cliente só via cookie `httpOnly` (nunca no corpo JSON). A cada `/refresh` o token é **rotacionado**: o antigo é revogado e um novo é emitido; reapresentar um token já rotacionado é tratado como sinal de roubo e **revoga todas as sessões do usuário**. Troca de senha também revoga todos os refresh tokens ativos. Em produção (front no Vercel, back no Render — domínios diferentes) o cookie usa `SameSite=None; Secure`; em desenvolvimento (mesmo `localhost`, portas diferentes), `SameSite=Lax` sem `Secure`. Exige `CORS_ORIGIN` configurada com origem explícita (`AllowCredentials` não funciona com `*`) e o cliente enviando `credentials: 'include'`.
- **`JWT_SECRET` via env var "solta":** o provider de env vars do ASP.NET Core não mapeia uma chave sem o prefixo `Jwt:` (ex. `JWT_SECRET`) para a seção `Jwt` usada por `JwtOptions`/`JwtTokenService`. `Program.cs` resolve o segredo (`Jwt:Secret` OU `JWT_SECRET`) e faz o replay em `builder.Configuration["Jwt:Secret"]` logo em seguida — sem isso, a Api sobe normalmente mas o login quebra ao assinar o token. Coberto por `JwtSecretFromFlatEnvVarTests`.

## Projeto irmão

O frontend (React + TypeScript + Vite) fica em [pomodoro-ui](https://github.com/saulomedeirosguerreiro/pomodoro-ui).
Para rodar os dois juntos localmente, garanta que `Cors:AllowedOrigin` aqui bate com a origem onde o
frontend sobe (`http://localhost:5173` por padrão) e que o `VITE_API_URL` do frontend aponta para
`http://localhost:5134` (ou a porta configurada aqui).
