using TutorClass.Domain.Common;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;

namespace TutorClass.Domain.Lessons;

public sealed class AcompanhamentoAula : Entity
{
    private AcompanhamentoAula()
    {
    }

    public AcompanhamentoAula(
        Guid aulaId,
        Guid alunoId,
        Guid professorId,
        string resumo,
        string? pontosFortes,
        string? pontosMelhorar,
        string? proximosPassos)
    {
        if (aulaId == Guid.Empty) throw new DomainException("Aula e obrigatoria.");
        if (alunoId == Guid.Empty) throw new DomainException("Aluno e obrigatorio.");
        if (professorId == Guid.Empty) throw new DomainException("Professor e obrigatorio.");
        if (string.IsNullOrWhiteSpace(resumo)) throw new DomainException("Resumo do acompanhamento e obrigatorio.");

        AulaId = aulaId;
        AlunoId = alunoId;
        ProfessorId = professorId;
        Atualizar(resumo, pontosFortes, pontosMelhorar, proximosPassos);
    }

    public Guid AulaId { get; private set; }
    public Aula? Aula { get; private set; }
    public Guid AlunoId { get; private set; }
    public Aluno? Aluno { get; private set; }
    public Guid ProfessorId { get; private set; }
    public Professor? Professor { get; private set; }
    public string Resumo { get; private set; } = string.Empty;
    public string? PontosFortes { get; private set; }
    public string? PontosMelhorar { get; private set; }
    public string? ProximosPassos { get; private set; }

    public void Atualizar(string resumo, string? pontosFortes, string? pontosMelhorar, string? proximosPassos)
    {
        if (string.IsNullOrWhiteSpace(resumo))
        {
            throw new DomainException("Resumo do acompanhamento e obrigatorio.");
        }

        Resumo = resumo.Trim();
        PontosFortes = NormalizeOptional(pontosFortes);
        PontosMelhorar = NormalizeOptional(pontosMelhorar);
        ProximosPassos = NormalizeOptional(proximosPassos);
        MarkUpdated();
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
