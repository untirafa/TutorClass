namespace TutorClass.Domain.Users;

public sealed class Responsavel : Usuario
{
    private readonly List<ResponsavelAluno> _alunos = [];

    private Responsavel()
    {
    }

    public Responsavel(string nome, string email, string passwordHash, string? telefone = null)
        : base(nome, email, passwordHash)
    {
        Telefone = telefone?.Trim();
    }

    public string? Telefone { get; private set; }
    public IReadOnlyCollection<ResponsavelAluno> Alunos => _alunos.AsReadOnly();
}
