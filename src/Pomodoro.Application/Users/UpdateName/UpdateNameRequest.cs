namespace Pomodoro.Application.Users.UpdateName;

/// <summary>Tela de Perfil — troca só o nome de exibição (e-mail e senha ficam fora deste fluxo).</summary>
public sealed record UpdateNameRequest(string Name);
