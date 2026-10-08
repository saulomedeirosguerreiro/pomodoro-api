using FluentAssertions;
using NSubstitute;
using Pomodoro.Application.Abstractions;
using Pomodoro.Application.Auth.RecoverPassword;
using Pomodoro.Application.Common;
using Pomodoro.Application.Common.Exceptions;
using Pomodoro.Domain.Entities;
using Xunit;

namespace Pomodoro.Application.Tests.Auth.RecoverPassword;

public class RecoverPasswordHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly RecoverPasswordHandler _handler;
    private static readonly DateTime BaseTime = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    public RecoverPasswordHandlerTests()
    {
        _clock.UtcNow.Returns(BaseTime);
        _handler = new RecoverPasswordHandler(_users, _hasher, _clock);
    }

    [Fact]
    public async Task HandleAsync_ComNomeEEmailCorretos_AtualizaSenhaESalva()
    {
        var user = User.Create("João", "joao@email.com", "hash-antigo", BaseTime.AddDays(-30));
        _users.FindByEmailAsync("joao@email.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Hash("NovaSenha123").Returns("hash-novo");

        await _handler.HandleAsync(
            new RecoverPasswordRequest("João", "joao@email.com", "NovaSenha123"), CancellationToken.None);

        user.PasswordHash.Should().Be("hash-novo");
        _hasher.Received(1).Hash("NovaSenha123");
        await _users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ComNomeBatendoIgnorandoCaixaEEspacos_Sucesso()
    {
        var user = User.Create("João", "joao@email.com", "hash-antigo", BaseTime.AddDays(-30));
        _users.FindByEmailAsync("joao@email.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Hash(Arg.Any<string>()).Returns("hash-novo");

        var act = () => _handler.HandleAsync(
            new RecoverPasswordRequest("  JOÃO  ", "joao@email.com", "NovaSenha123"), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_ComEmailNaoEncontrado_LancaNotFoundExceptionComMensagemGenerica()
    {
        _users.FindByEmailAsync("naoexiste@email.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = () => _handler.HandleAsync(
            new RecoverPasswordRequest("João", "naoexiste@email.com", "NovaSenha123"), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<NotFoundException>();
        exception.Which.Message.Should().Be(RecoverPasswordHandler.GenericMessage);
        _hasher.DidNotReceive().Hash(Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_ComEmailEncontradoMasNomeNaoBate_LancaNotFoundExceptionComMensagemIdenticaAEmailInexistente()
    {
        var user = User.Create("João", "joao@email.com", "hash-antigo", BaseTime.AddDays(-30));
        _users.FindByEmailAsync("joao@email.com", Arg.Any<CancellationToken>()).Returns(user);
        _users.FindByEmailAsync("naoexiste@email.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        var actNomeErrado = () => _handler.HandleAsync(
            new RecoverPasswordRequest("Nome Errado", "joao@email.com", "NovaSenha123"), CancellationToken.None);
        var actEmailInexistente = () => _handler.HandleAsync(
            new RecoverPasswordRequest("João", "naoexiste@email.com", "NovaSenha123"), CancellationToken.None);

        var exceptionNomeErrado = await actNomeErrado.Should().ThrowAsync<NotFoundException>();
        var exceptionEmailInexistente = await actEmailInexistente.Should().ThrowAsync<NotFoundException>();

        exceptionNomeErrado.Which.Message.Should().Be(exceptionEmailInexistente.Which.Message);
        _hasher.DidNotReceive().Hash(Arg.Any<string>());
    }
}
