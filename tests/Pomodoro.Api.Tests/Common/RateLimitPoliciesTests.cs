using System.Net;
using FluentAssertions;
using Pomodoro.Api.Common;
using Xunit;

namespace Pomodoro.Api.Tests.Common;

public class RateLimitPoliciesTests
{
    [Fact]
    public void ResolveClientPartitionKey_ComIpNulo_RetornaUnknown()
    {
        RateLimitPolicies.ResolveClientPartitionKey(null).Should().Be("unknown");
    }

    [Fact]
    public void ResolveClientPartitionKey_ComIpInformado_RetornaORepresentacaoEmTexto()
    {
        var ip = IPAddress.Parse("203.0.113.5");

        RateLimitPolicies.ResolveClientPartitionKey(ip).Should().Be("203.0.113.5");
    }
}
