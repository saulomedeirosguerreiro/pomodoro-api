using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Pomodoro.Infrastructure.Persistence;

/// <summary>
/// SQLite não guarda timezone: um DateTime volta do banco com Kind=Unspecified, o que faz
/// o JSON sair sem o sufixo "Z" e o frontend interpretar a data como hora local (quebra L-9).
/// Toda data da aplicação é gravada em UTC, então forçamos Kind=Utc na leitura.
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}
