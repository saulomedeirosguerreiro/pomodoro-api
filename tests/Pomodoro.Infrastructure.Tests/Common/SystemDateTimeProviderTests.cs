using FluentAssertions;
using Pomodoro.Infrastructure.Common;
using Xunit;

namespace Pomodoro.Infrastructure.Tests.Common;

public class SystemDateTimeProviderTests
{
    [Fact]
    public void UtcNow_RetornaHorarioAtualProximoDoRelogioReal()
    {
        var provider = new SystemDateTimeProvider();

        provider.UtcNow.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
