# PomoGarden — API

Backend do PomoGarden: Pomodoro gamificado (cadastro, login, temporizador, tarefas vinculadas a pomodoros,
progresso de XP/nível/sementes/streak e conquistas). O frontend (React + TS + Vite) vive num repositório
irmão: [pomodoro-ui](https://github.com/saulomedeirosguerreiro/pomodoro-ui).

## Tecnologias utilizadas

- **.NET 8 / C#**, ASP.NET Core **Minimal API**
- **Clean Architecture**: `Pomodoro.Domain` → `Pomodoro.Application` → `Pomodoro.Infrastructure` → `Pomodoro.Api`
- **EF Core + SQLite** (persistência; schema 100% via Migrations, sem SQL manual)
- **JWT** (`Microsoft.AspNetCore.Authentication.JwtBearer`) + **BCrypt.Net-Next** (hash de senha)
- **FluentValidation** (validação de entrada)
- **xUnit + FluentAssertions 7.x + NSubstitute + WebApplicationFactory** (testes), **coverlet** (cobertura)

## Como instalar as dependências

Pré-requisito: **.NET SDK 8**.

```bash
dotnet tool restore   # instala dotnet-ef e reportgenerator (versões fixas no repo)
dotnet restore
```

## Como configurar as variáveis de ambiente

Lidas via ambiente (ou `--Chave:Sub=valor` na linha de comando):

| Variável | Obrigatória | Exemplo |
|---|---|---|
| `JWT_SECRET` (ou `Jwt:Secret`) | **Sim** — a API não sobe sem ela | uma string aleatória com 32+ caracteres |
| `ConnectionStrings__Default` | Sim (tem default em `appsettings.Development.json`) | `Data Source=pomodoro.db` |
| `CORS_ORIGIN` (ou `Cors:AllowedOrigin`) | Recomendada — precisa bater com a origem do frontend | `http://localhost:5173` |

PowerShell:
```powershell
$env:JWT_SECRET = "troque-por-uma-string-aleatoria-de-32-caracteres-ou-mais"
```

Bash:
```bash
export JWT_SECRET="troque-por-uma-string-aleatoria-de-32-caracteres-ou-mais"
```

> Em `appsettings.Development.json` já há um `ConnectionStrings:Default` (`Data Source=pomodoro.db`) e
> `Cors:AllowedOrigin` (`http://localhost:5173`) prontos para rodar local. Só `JWT_SECRET` precisa ser
> definido manualmente — de propósito (RNF-02): a API deve recusar subir sem um segredo configurado.

## Como configurar o banco de dados

SQLite, schema via EF Core Migrations (sem SQL manual):

```bash
export ConnectionStrings__Default="Data Source=pomodoro.db"   # mesmo valor do appsettings.Development.json
dotnet ef database update --project src/Pomodoro.Infrastructure --startup-project src/Pomodoro.Api
```

PowerShell:
```powershell
$env:ConnectionStrings__Default = "Data Source=pomodoro.db"
dotnet ef database update --project src/Pomodoro.Infrastructure --startup-project src/Pomodoro.Api
```

Isso cria `src/Pomodoro.Api/pomodoro.db` (caminhos relativos em `ConnectionStrings:Default` resolvem a
partir do diretório do projeto de *startup*, `src/Pomodoro.Api`) com as tabelas `users`, `pomodoros`,
`tasks` e `user_achievements`.

> **Por que exportar `ConnectionStrings__Default` só para este comando?** `dotnet ef` roda por fora do
> host da Api e não lê `appsettings.Development.json` — sem a variável, ele aplica a migração num arquivo
> descartável (`pomodoro.design.db`) só usado para gerar novas migrações, e a Api real (que lê
> `appsettings.Development.json` normalmente) sobe com um banco **sem tabelas**. `dotnet run` (próximo
> passo) não precisa dessa variável.

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
POST   /api/auth/login
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

379 testes, 100% de cobertura de linha/branch/método (Domain, Application, Infrastructure, Api). A única
exceção documentada são 3 linhas do branch `dotnet run -- seed` em `Program.cs`, que só roda via CLI e não
faz parte do pipeline HTTP exercitado pelos testes de integração.

```bash
dotnet test Pomodoro.sln
```

Com relatório de cobertura mesclado (requer `dotnet tool restore` antes, uma vez):
```bash
dotnet test Pomodoro.sln --settings coverage.runsettings --collect:"XPlat Code Coverage" --results-directory ./coverage-tmp
dotnet reportgenerator -reports:"coverage-tmp/*/coverage.cobertura.xml" -targetdir:coverage-report -reporttypes:"TextSummary"
cat coverage-report/Summary.txt
```

## Notas de arquitetura e decisões

- **Isolamento entre usuários:** o `user_id` nunca vem do corpo da requisição — sempre do claim `sub` do token JWT.
- **Erros:** envelope único `{ "error": { "code", "message", "fields"? } }`, com `422` (validação), `401` (não autenticado), `404` (não encontrado ou de outro dono), `409` (conflito) e `500` (genérico, sem stack trace — detalhe só no log do servidor).
- **Antifraude no registro (`POST /api/pomodoros`):** rejeita `started_at` no futuro, `completed_at` mais de 60s no futuro, e qualquer sobreposição de intervalo com outra sessão do mesmo usuário — sessões de outros usuários nunca entram nessa checagem.
- **Datas:** gravadas em UTC no banco. Para streak e resumo do dia, o cliente manda `tz` (IANA, ex. `America/Sao_Paulo`) e o servidor converte antes de agrupar por dia — sem `tz`, o default é `America/Sao_Paulo`; um `tz` inválido retorna `422`.
- **XP, Nível, Sementes e Streak** são uma **projeção calculada no servidor** a partir do histórico de `pomodoros` — não existe tabela de saldo. Fórmulas (`Pomodoro.Domain.Services.ProgressRules`): foco concluído = 25 XP, pausa concluída = 5 XP, sessão interrompida = 0 XP; nível sobe a cada `200 × nível atual` XP; sementes = 15 por foco concluído + 10 de bônus por pausa longa concluída (fecha um ciclo). Streak conta dias seguidos com ao menos um foco concluído, terminando hoje ou ontem — só "quebra" quando um dia inteiro passa sem foco.
- **Tarefas:** entidade de backend (não localStorage), com vínculo opcional `pomodoros.task_id` (`ON DELETE SET NULL` — apagar uma tarefa nunca apaga o histórico de pomodoros). O vínculo só é aceito para `type=foco`, para uma tarefa do mesmo usuário e que ainda não esteja `feito`; qualquer violação devolve a mesma mensagem genérica (não revela a existência de tarefas de outro usuário). No máximo uma tarefa por usuário fica `em_curso` por vez — marcar uma nova tira a anterior do foco automaticamente.
- **Conquistas:** catálogo fixo no código (`Pomodoro.Domain.Achievements.AchievementCatalog`, 10 itens), avaliado no servidor a cada sessão registrada e a cada tarefa concluída. A avaliação é idempotente (nunca grava a mesma conquista duas vezes) e **retroativa** (quem já tinha histórico recebe as conquistas na primeira consulta a `GET /api/achievements`).
- **Exclusão de conta (`DELETE /api/users/me`):** exige a senha atual no corpo da requisição (reautenticação, não só o token) antes de apagar — sem isso, retorna `401`. É definitiva: o usuário é removido e, por cascata de FK, todas as suas sessões, tarefas e conquistas vão junto. Não existe lista de revogação de token — um JWT emitido antes da exclusão permanece criptograficamente válido até expirar (24h), mas qualquer chamada que precise ler o usuário no banco passa a responder `404`.
- **`JWT_SECRET` via env var "solta":** o provider de env vars do ASP.NET Core não mapeia uma chave sem o prefixo `Jwt:` (ex. `JWT_SECRET`) para a seção `Jwt` usada por `JwtOptions`/`JwtTokenService`. `Program.cs` resolve o segredo (`Jwt:Secret` OU `JWT_SECRET`) e faz o replay em `builder.Configuration["Jwt:Secret"]` logo em seguida — sem isso, a Api sobe normalmente mas o login quebra ao assinar o token. Coberto por `JwtSecretFromFlatEnvVarTests`.

## Projeto irmão

O frontend (React + TypeScript + Vite) fica em [pomodoro-ui](https://github.com/saulomedeirosguerreiro/pomodoro-ui).
Para rodar os dois juntos localmente, garanta que `Cors:AllowedOrigin` aqui bate com a origem onde o
frontend sobe (`http://localhost:5173` por padrão) e que o `VITE_API_URL` do frontend aponta para
`http://localhost:5134` (ou a porta configurada aqui).
