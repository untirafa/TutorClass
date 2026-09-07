using TutorClass.Domain.Common;
using TutorClass.Domain.Teachers;

namespace TutorClass.Domain.Subjects;

public sealed class Materia : Entity
{
    private readonly List<ProfessorMateria> _professores = [];

    private Materia()
    {
    }

    public Materia(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException("Nome da materia e obrigatorio.");
        }

        Nome = nome.Trim();
    }

    public string Nome { get; private set; } = string.Empty;
    public IReadOnlyCollection<ProfessorMateria> Professores => _professores.AsReadOnly();
}
