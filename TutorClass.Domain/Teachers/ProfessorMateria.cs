using TutorClass.Domain.Common;
using TutorClass.Domain.Subjects;

namespace TutorClass.Domain.Teachers;

public sealed class ProfessorMateria : Entity
{
    private ProfessorMateria()
    {
    }

    public ProfessorMateria(Guid professorId, Guid materiaId)
    {
        ProfessorId = professorId;
        MateriaId = materiaId;
    }

    public Guid ProfessorId { get; private set; }
    public Professor? Professor { get; private set; }
    public Guid MateriaId { get; private set; }
    public Materia? Materia { get; private set; }
}
