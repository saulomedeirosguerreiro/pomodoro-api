using FluentAssertions;
using Xunit;

namespace Pomodoro.Api.Tests;

public class ProgramTests
{
    [Fact]
    public void FirstNonBlank_RetornaOPrimeiroValorNaoVazio()
    {
        Program.FirstNonBlank("", "  ", null, "valor", "outro").Should().Be("valor");
    }

    [Fact]
    public void FirstNonBlank_ComPrimeiroValorValido_IgnoraOsDemais()
    {
        Program.FirstNonBlank("primeiro", "segundo").Should().Be("primeiro");
    }

    [Fact]
    public void FirstNonBlank_SemNenhumValorValido_RetornaNull()
    {
        Program.FirstNonBlank("", null, "   ").Should().BeNull();
    }
}
