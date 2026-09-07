using TutorClass.Domain.Common;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;

namespace TutorClass.Domain.Mentoring;

public sealed class AcompanhamentoMentoria : Entity
{
    private AcompanhamentoMentoria()
    {
    }

    public AcompanhamentoMentoria(Guid aulaId, Guid alunoId, Guid professorId, Guid vinculoMentoriaId)
    {
        if (aulaId == Guid.Empty || alunoId == Guid.Empty || professorId == Guid.Empty || vinculoMentoriaId == Guid.Empty)
        {
            throw new DomainException("Aula, aluno, professor e vinculo de mentoria sao obrigatorios.");
        }

        AulaId = aulaId;
        AlunoId = alunoId;
        ProfessorId = professorId;
        VinculoMentoriaId = vinculoMentoriaId;
    }

    public Guid AulaId { get; private set; }
    public Aula? Aula { get; private set; }
    public Guid AlunoId { get; private set; }
    public Aluno? Aluno { get; private set; }
    public Guid ProfessorId { get; private set; }
    public Professor? Professor { get; private set; }
    public Guid VinculoMentoriaId { get; private set; }
    public MentoriaAlunoProfessor? VinculoMentoria { get; private set; }
    public string? Resumo { get; private set; }
    public string? PontosFortes { get; private set; }
    public string? PontosMelhorar { get; private set; }
    public string? ProximosPassos { get; private set; }
    public string? MetaAtual { get; private set; }
    public string? Observacoes { get; private set; }
    public StatusAcompanhamentoMentoria Status { get; private set; } = StatusAcompanhamentoMentoria.Pendente;
    public DateTime? DataConclusaoUtc { get; private set; }

    public void Atualizar(string? resumo, string? pontosFortes, string? pontosMelhorar, string? proximosPassos, string? metaAtual, string? observacoes)
    {
        Resumo = NormalizeOptional(resumo, "Resumo", 2000);
        PontosFortes = NormalizeOptional(pontosFortes, "Pontos fortes", 1000);
        PontosMelhorar = NormalizeOptional(pontosMelhorar, "Pontos a melhorar", 1000);
        ProximosPassos = NormalizeOptional(proximosPassos, "Proximos passos", 1000);
        MetaAtual = NormalizeOptional(metaAtual, "Meta atual", 1000);
        Observacoes = NormalizeOptional(observacoes, "Observacoes", 1500);
        MarkUpdated();
    }

    public void Concluir()
    {
        if (string.IsNullOrWhiteSpace(Resumo))
        {
            throw new DomainException("Resumo e obrigatorio para concluir acompanhamento de mentoria.");
        }

        Status = StatusAcompanhamentoMentoria.Concluido;
        DataConclusaoUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    private static string? NormalizeOptional(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} deve ter no maximo {maxLength} caracteres.");
        }

        return normalized;
    }
}
