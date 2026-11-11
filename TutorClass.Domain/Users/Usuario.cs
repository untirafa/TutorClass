using TutorClass.Domain.Common;

namespace TutorClass.Domain.Users;

public abstract class Usuario : Entity
{
    protected Usuario()
    {
    }

    protected Usuario(string nome, string email, string passwordHash)
    {
        Nome = Require(nome, nameof(nome));
        Email = Require(email, nameof(email)).Trim().ToLowerInvariant();
        PasswordHash = Require(passwordHash, nameof(passwordHash));
    }

    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool Ativo { get; private set; } = true;

    public void AtualizarPerfil(string nome)
    {
        Nome = Require(nome, nameof(nome));
        MarkUpdated();
    }

    public void Desativar()
    {
        Ativo = false;
        MarkUpdated();
    }

    public void Reativar()
    {
        Ativo = true;
        MarkUpdated();
    }

    public void DefinirPasswordHash(string passwordHash)
    {
        PasswordHash = Require(passwordHash, nameof(passwordHash));
        MarkUpdated();
    }

    private static string Require(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} e obrigatorio.");
        }

        return value.Trim();
    }
}
