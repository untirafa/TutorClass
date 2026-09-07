using TutorClass.Domain.Common;

namespace TutorClass.Domain.Teachers;

public sealed class CandidaturaProfessor : Entity
{
    private CandidaturaProfessor()
    {
    }

    public CandidaturaProfessor(Guid professorId, string mensagem)
    {
        ProfessorId = professorId;
        Mensagem = string.IsNullOrWhiteSpace(mensagem) ? "Solicitacao inicial para atuar como professor." : mensagem.Trim();
    }

    public Guid ProfessorId { get; private set; }
    public Professor? Professor { get; private set; }
    public string Mensagem { get; private set; } = string.Empty;
    public StatusCandidaturaProfessor Status { get; private set; } = StatusCandidaturaProfessor.Pendente;
    public Guid? AdministradorAnaliseId { get; private set; }
    public string? Motivo { get; private set; }
    public DateTime? DataAnaliseUtc { get; private set; }
    public bool EstaAtiva => Status is StatusCandidaturaProfessor.Pendente
        or StatusCandidaturaProfessor.EmAnalise
        or StatusCandidaturaProfessor.AguardandoDocumentacao;

    public void IniciarAnalise(Guid administradorId)
    {
        if (Status is not StatusCandidaturaProfessor.Pendente and not StatusCandidaturaProfessor.AguardandoDocumentacao)
        {
            throw new DomainException("Somente candidaturas pendentes ou aguardando documentacao podem entrar em analise.");
        }

        Status = StatusCandidaturaProfessor.EmAnalise;
        AdministradorAnaliseId = administradorId;
        MarkUpdated();
    }

    public void SolicitarComplementacao(Guid administradorId, string motivo)
    {
        if (Status != StatusCandidaturaProfessor.EmAnalise)
        {
            throw new DomainException("Somente candidaturas em analise podem solicitar complementacao.");
        }

        Status = StatusCandidaturaProfessor.AguardandoDocumentacao;
        AdministradorAnaliseId = administradorId;
        Motivo = RequireMotivo(motivo);
        DataAnaliseUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Aprovar(Guid administradorId)
    {
        if (Status != StatusCandidaturaProfessor.EmAnalise)
        {
            throw new DomainException("Somente candidaturas em analise podem ser aprovadas.");
        }

        Status = StatusCandidaturaProfessor.Aprovada;
        AdministradorAnaliseId = administradorId;
        DataAnaliseUtc = DateTime.UtcNow;
        Motivo = null;
        Professor?.Aprovar();
        MarkUpdated();
    }

    public void Reprovar(Guid administradorId, string motivo)
    {
        if (Status != StatusCandidaturaProfessor.EmAnalise)
        {
            throw new DomainException("Somente candidaturas em analise podem ser reprovadas.");
        }

        Status = StatusCandidaturaProfessor.Reprovada;
        AdministradorAnaliseId = administradorId;
        DataAnaliseUtc = DateTime.UtcNow;
        Motivo = RequireMotivo(motivo);
        Professor?.Reprovar();
        MarkUpdated();
    }

    private static string RequireMotivo(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new DomainException("Motivo e obrigatorio.");
        }

        return motivo.Trim();
    }
}
