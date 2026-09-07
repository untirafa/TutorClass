using TutorClass.Domain.Common;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;

namespace TutorClass.Domain.Lessons;

public sealed class Aula : Entity
{
    private Aula()
    {
    }

    public Aula(
        Guid alunoId,
        Guid professorId,
        Guid materiaId,
        DateTime inicioUtc,
        int duracaoMinutos,
        decimal precoAcordado)
        : this(alunoId, professorId, materiaId, inicioUtc, duracaoMinutos, precoAcordado, alunoId)
    {
    }

    public Aula(
        Guid alunoId,
        Guid professorId,
        Guid materiaId,
        DateTime inicioUtc,
        int duracaoMinutos,
        decimal precoAcordado,
        Guid solicitadaPorUsuarioId)
    {
        if (alunoId == Guid.Empty)
        {
            throw new DomainException("Aluno e obrigatorio.");
        }

        if (professorId == Guid.Empty)
        {
            throw new DomainException("Professor e obrigatorio.");
        }

        if (materiaId == Guid.Empty)
        {
            throw new DomainException("Materia e obrigatoria.");
        }

        if (duracaoMinutos < 30 || duracaoMinutos > 240)
        {
            throw new DomainException("Duracao da aula deve estar entre 30 e 240 minutos.");
        }

        if (precoAcordado <= 0)
        {
            throw new DomainException("Preco acordado deve ser maior que zero.");
        }

        if (solicitadaPorUsuarioId == Guid.Empty)
        {
            throw new DomainException("Usuario solicitante e obrigatorio.");
        }

        AlunoId = alunoId;
        ProfessorId = professorId;
        MateriaId = materiaId;
        InicioUtc = inicioUtc.Kind == DateTimeKind.Utc ? inicioUtc : DateTime.SpecifyKind(inicioUtc, DateTimeKind.Utc);
        DuracaoMinutos = duracaoMinutos;
        PrecoAcordado = precoAcordado;
        SolicitadaPorUsuarioId = solicitadaPorUsuarioId;
    }

    public Guid AlunoId { get; private set; }
    public Aluno? Aluno { get; private set; }
    public Guid SolicitadaPorUsuarioId { get; private set; }
    public Usuario? SolicitadaPorUsuario { get; private set; }
    public Guid ProfessorId { get; private set; }
    public Professor? Professor { get; private set; }
    public Guid MateriaId { get; private set; }
    public Materia? Materia { get; private set; }
    public DateTime InicioUtc { get; private set; }
    public int DuracaoMinutos { get; private set; }
    public decimal PrecoAcordado { get; private set; }
    public StatusAula Status { get; private set; } = StatusAula.Solicitada;
    public DateTime? ConfirmadaEmUtc { get; private set; }
    public DateTime? CanceladaEmUtc { get; private set; }
    public DateTime? ConcluidaEmUtc { get; private set; }
    public Guid? CanceladaPorUsuarioId { get; private set; }
    public string? MotivoCancelamento { get; private set; }

    public DateTime FimUtc => InicioUtc.AddMinutes(DuracaoMinutos);

    public void Confirmar(Guid professorId)
    {
        EnsureProfessorOwnsLesson(professorId);

        if (Status != StatusAula.Solicitada)
        {
            throw new DomainException("Somente aulas solicitadas podem ser confirmadas.");
        }

        Status = StatusAula.Confirmada;
        ConfirmadaEmUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Cancelar(Guid usuarioId, string? motivo, bool usuarioAutorizadoPorVinculo = false)
    {
        if (!usuarioAutorizadoPorVinculo && usuarioId != AlunoId && usuarioId != ProfessorId)
        {
            throw new DomainException("Somente o aluno ou o professor da aula podem cancela-la.");
        }

        if (Status == StatusAula.Cancelada)
        {
            throw new DomainException("Aula ja esta cancelada.");
        }

        if (Status == StatusAula.Concluida)
        {
            throw new DomainException("Aula concluida nao pode ser cancelada.");
        }

        Status = StatusAula.Cancelada;
        CanceladaPorUsuarioId = usuarioId;
        MotivoCancelamento = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        CanceladaEmUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Reagendar(DateTime novoInicioUtc, int novaDuracaoMinutos)
    {
        if (Status == StatusAula.Cancelada)
        {
            throw new DomainException("Aula cancelada nao pode ser reagendada.");
        }

        if (Status == StatusAula.Concluida)
        {
            throw new DomainException("Aula concluida nao pode ser reagendada.");
        }

        if (novaDuracaoMinutos < 30 || novaDuracaoMinutos > 240)
        {
            throw new DomainException("Duracao da aula deve estar entre 30 e 240 minutos.");
        }

        InicioUtc = novoInicioUtc.Kind == DateTimeKind.Utc ? novoInicioUtc : DateTime.SpecifyKind(novoInicioUtc, DateTimeKind.Utc);
        DuracaoMinutos = novaDuracaoMinutos;
        MarkUpdated();
    }

    public void Concluir(Guid professorId)
    {
        EnsureProfessorOwnsLesson(professorId);

        if (Status != StatusAula.Confirmada)
        {
            throw new DomainException("Somente aulas confirmadas podem ser concluidas.");
        }

        Status = StatusAula.Concluida;
        ConcluidaEmUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public bool Sobrepoe(DateTime inicioUtc, DateTime fimUtc)
    {
        return InicioUtc < fimUtc && inicioUtc < FimUtc;
    }

    private void EnsureProfessorOwnsLesson(Guid professorId)
    {
        if (professorId != ProfessorId)
        {
            throw new DomainException("Somente o professor da aula pode executar esta acao.");
        }
    }
}
