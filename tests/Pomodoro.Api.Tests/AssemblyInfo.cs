using Xunit;

// Cada classe de teste de integração que usa PomodoroApiFactory/FlatEnvVarFactory bloqueia a thread
// de construção (.GetAwaiter().GetResult()) para iniciar o container Postgres compartilhado e criar
// seu próprio banco (ver PostgresTestDatabaseFactory). Com paralelismo padrão do xUnit, várias
// classes tentam isso ao mesmo tempo logo no início da suíte; combinado com o host ASP.NET Core de
// cada WebApplicationFactory também bloqueando internamente durante o startup, as threads do
// ThreadPool disponíveis para retomar as continuações assíncronas do Testcontainers se esgotam,
// deixando a suíte praticamente parada por minutos (CPU ~0%) até o injetor de threads do runtime
// recuperar aos poucos. Desabilitar o paralelismo neste assembly elimina a contenção por completo —
// o custo é uma suíte um pouco mais lenta, não uma suíte instável.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
