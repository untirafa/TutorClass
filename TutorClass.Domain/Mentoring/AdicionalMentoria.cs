using TutorClass.Domain.Common;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;

namespace TutorClass.Domain.Mentoring;

public sealed class AdicionalMentoria : Entity
{
    private AdicionalMentoria()
    {
    }

    public AdicionalMentoria(Guid professorId, Guid alunoId, Guid aulaId, Guid acompanhamentoMentoriaId, decimal valor)
    {
        if (professorId == Guid.Empty || alunoId == Guid.Empty || aulaId == Guid.Empty || acompanhamentoMentoriaId == Guid.Empty)
        {
            throw new DomainException("Professor, aluno, aula e acompanhamento sao obrigatorios.");
        }

        if (valor <= 0)
        {
            throw new DomainException("Valor do adicional deve ser maior que zero.");
        }

        ProfessorId = professorId;
        AlunoId = alunoId;
        AulaId = aulaId;
        AcompanhamentoMentoriaId = acompanhamentoMentoriaId;
        Valor = valor;
        DataGeracaoUtc = DateTime.UtcNow;
    }

    public Guid ProfessorId { get; private set; }
    public Professor? Professor { get; private set; }
    public Guid AlunoId { get; private set; }
    public Aluno? Aluno { get; private set; }
    public Guid AulaId { get; private set; }
    public Aula? Aula { get; private set; }
    public Guid AcompanhamentoMentoriaId { get; private set; }
    public AcompanhamentoMentoria? AcompanhamentoMentoria { get; private set; }
    public decimal Valor { get; private set; }
    public DateTime DataGeracaoUtc { get; private set; }
    public StatusAdicionalMentoria Status { get; private set; } = StatusAdicionalMentoria.Simulado;
}
