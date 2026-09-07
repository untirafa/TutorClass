using TutorClass.Domain.Common;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;

namespace TutorClass.Domain.Mentoring;

public sealed class MentoriaAlunoProfessor : Entity
{
    private MentoriaAlunoProfessor()
    {
    }

    public MentoriaAlunoProfessor(Guid alunoId, Guid professorId, int slot)
    {
        if (alunoId == Guid.Empty)
        {
            throw new DomainException("Aluno e obrigatorio.");
        }

        if (professorId == Guid.Empty)
        {
            throw new DomainException("Professor e obrigatorio.");
        }

        if (slot is < 1 or > 2)
        {
            throw new DomainException("Vaga de mentor deve ser 1 ou 2.");
        }

        AlunoId = alunoId;
        ProfessorId = professorId;
        Slot = slot;
        DataInicioUtc = DateTime.UtcNow;
        DataUltimaTrocaUtc = DataInicioUtc;
    }

    public Guid AlunoId { get; private set; }
    public Aluno? Aluno { get; private set; }
    public Guid ProfessorId { get; private set; }
    public Professor? Professor { get; private set; }
    public int Slot { get; private set; }
    public DateTime DataInicioUtc { get; private set; }
    public DateTime? DataFimUtc { get; private set; }
    public bool Ativo { get; private set; } = true;
    public DateTime DataUltimaTrocaUtc { get; private set; }
    public MotivoEncerramentoMentoria? MotivoEncerramento { get; private set; }

    public void Encerrar(MotivoEncerramentoMentoria motivo)
    {
        if (!Ativo)
        {
            return;
        }

        Ativo = false;
        DataFimUtc = DateTime.UtcNow;
        MotivoEncerramento = motivo;
        MarkUpdated();
    }
}
