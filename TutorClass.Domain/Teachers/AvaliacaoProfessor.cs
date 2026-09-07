using TutorClass.Domain.Common;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Users;

namespace TutorClass.Domain.Teachers;

public sealed class AvaliacaoProfessor : Entity
{
    private AvaliacaoProfessor()
    {
    }

    public AvaliacaoProfessor(Guid aulaId, Guid alunoId, Guid professorId, int nota, string? comentario)
    {
        if (aulaId == Guid.Empty) throw new DomainException("Aula e obrigatoria.");
        if (alunoId == Guid.Empty) throw new DomainException("Aluno e obrigatorio.");
        if (professorId == Guid.Empty) throw new DomainException("Professor e obrigatorio.");
        if (nota is < 1 or > 5) throw new DomainException("Nota deve estar entre 1 e 5.");

        AulaId = aulaId;
        AlunoId = alunoId;
        ProfessorId = professorId;
        Nota = nota;
        Comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();
    }

    public Guid AulaId { get; private set; }
    public Aula? Aula { get; private set; }
    public Guid AlunoId { get; private set; }
    public Aluno? Aluno { get; private set; }
    public Guid ProfessorId { get; private set; }
    public Professor? Professor { get; private set; }
    public int Nota { get; private set; }
    public string? Comentario { get; private set; }
}
