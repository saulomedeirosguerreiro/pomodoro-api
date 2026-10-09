using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Pomodoro.Infrastructure.Persistence;

/// <summary>
/// Nem SQLite nem o mapeamento padrão do Postgres (<c>timestamp without time zone</c>) guardam
/// timezone: um DateTime volta do banco com Kind=Unspecified, o que faz o JSON sair sem o sufixo
/// "Z" e o frontend interpretar a data como hora local (quebra L-9). Toda data da aplicação é
/// gravada em UTC, então forçamos Kind=Utc na leitura.
/// Na escrita, o Npgsql exige Kind=Unspecified para "timestamp without time zone" — gravar
/// Kind=Utc (o que <c>_clock.UtcNow</c> sempre produz) lançaria exceção em todo SaveChangesAsync.
/// Normalizamos para Unspecified sem alterar o valor; dados importados do guest que já chegam com
/// Kind=Unspecified (ver ImportGuestDataHandler) continuam gravando normalmente.
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(
        v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}
