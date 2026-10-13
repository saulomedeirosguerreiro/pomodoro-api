# Render só roda contêineres Docker (sem runtime .NET nativo) — esta imagem compila e serve a Api.
# Build do contexto `backend/` (raiz deste Dockerfile): `docker build -f Dockerfile .`

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Só os .csproj primeiro: o cache de `dotnet restore` sobrevive a mudanças de código-fonte, que não
# tocam esses arquivos.
COPY src/Pomodoro.Domain/Pomodoro.Domain.csproj src/Pomodoro.Domain/
COPY src/Pomodoro.Application/Pomodoro.Application.csproj src/Pomodoro.Application/
COPY src/Pomodoro.Infrastructure/Pomodoro.Infrastructure.csproj src/Pomodoro.Infrastructure/
COPY src/Pomodoro.Api/Pomodoro.Api.csproj src/Pomodoro.Api/
RUN dotnet restore src/Pomodoro.Api/Pomodoro.Api.csproj

COPY src/ src/
RUN dotnet publish src/Pomodoro.Api/Pomodoro.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

# O Render define `PORT` em runtime (não em build) e espera a aplicação escutando em `0.0.0.0:$PORT`;
# a imagem base do .NET 8 não lê essa variável sozinha, então montamos ASPNETCORE_URLS a partir dela
# no entrypoint. `exec` troca o processo do shell pelo dotnet (PID 1), preservando o encerramento
# gracioso do Render via SIGTERM.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet Pomodoro.Api.dll"]
