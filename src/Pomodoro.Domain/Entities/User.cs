using Pomodoro.Domain.Common;

namespace Pomodoro.Domain.Entities;

public sealed class User
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private User()
    {
    }

    public static User Create(string name, string email, string passwordHash, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Nome não pode ser vazio.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("E-mail não pode ser vazio.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Hash de senha não pode ser vazio.");
        }

        return new User
        {
            Name = name.Trim(),
            Email = email.Trim(),
            PasswordHash = passwordHash,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    /// <summary>Troca o hash de senha (ex.: recuperação sem e-mail) — mantém o restante dos dados intacto.</summary>
    public void UpdatePassword(string newPasswordHash, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new DomainException("Hash de senha não pode ser vazio.");
        }

        PasswordHash = newPasswordHash;
        UpdatedAt = utcNow;
    }
}
