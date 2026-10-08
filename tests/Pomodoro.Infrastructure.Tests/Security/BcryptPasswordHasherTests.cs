using FluentAssertions;
using Pomodoro.Infrastructure.Security;
using Xunit;

namespace Pomodoro.Infrastructure.Tests.Security;

public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_DepoisVerify_ComSenhaCorreta_RetornaTrue()
    {
        var hash = _hasher.Hash("Senha123");

        _hasher.Verify("Senha123", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_ComSenhaErrada_RetornaFalse()
    {
        var hash = _hasher.Hash("Senha123");

        _hasher.Verify("Errada123", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_ChamadoDuasVezesParaMesmaSenha_GeraHashesDiferentes()
    {
        var hash1 = _hasher.Hash("Senha123");
        var hash2 = _hasher.Hash("Senha123");

        hash1.Should().NotBe(hash2);
    }
}
