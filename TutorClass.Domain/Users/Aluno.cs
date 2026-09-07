namespace TutorClass.Domain.Users;

public sealed class Aluno : Usuario
{
    private readonly List<ResponsavelAluno> _responsaveis = [];

    private Aluno()
    {
    }

    public Aluno(string nome, string email, string passwordHash, DateOnly? dataNascimento = null)
        : base(nome, email, passwordHash)
    {
        DataNascimento = dataNascimento;
    }

    public DateOnly? DataNascimento { get; private set; }
    public IReadOnlyCollection<ResponsavelAluno> Responsaveis => _responsaveis.AsReadOnly();
}
