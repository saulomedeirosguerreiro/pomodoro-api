using Pomodoro.Application.Common;

namespace Pomodoro.Infrastructure.Common;

/// <summary>
/// Postgres <c>timestamp without time zone</c> só guarda precisão de microssegundos (6 dígitos
/// fracionários); o <see cref="DateTime.UtcNow"/> nativo do .NET tem resolução de tick (100ns, 7
/// dígitos). Sem truncar aqui, o valor em memória logo após criar uma entidade (ex.: resposta de uma
/// importação idempotente, que no primeiro envio devolve o valor ainda não persistido) difere do
/// mesmo valor lido de volta do banco numa chamada seguinte, quebrando comparações de igualdade/string
/// entre as duas leituras do "mesmo" registro.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    private const long TicksPerMicrosecond = 10;

    public DateTime UtcNow
    {
        get
        {
            var now = DateTime.UtcNow;
            return now.AddTicks(-(now.Ticks % TicksPerMicrosecond));
        }
    }
}
