namespace TutorClass.Application.DTOs;

public sealed class ContextoIaAluno
{
    public Guid AlunoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Plano { get; set; } = string.Empty;
    public int MensagensUsadasHoje { get; set; }
    public int? MensagensRestantesHoje { get; set; }
    public List<string> Materias { get; set; } = new();
    public List<AulaContextoIa> ProximasAulas { get; set; } = new();
    public List<MentorContextoIa> MentoresAtivos { get; set; } = new();
}

public sealed record AulaContextoIa(Guid AulaId, string Materia, DateTime InicioUtc, string Professor);
public sealed record MentorContextoIa(Guid ProfessorId, string Nome, string? Materia = null);
