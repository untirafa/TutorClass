using TutorClass.Domain.Common;

namespace TutorClass.Domain.Teachers;

public sealed class QualificacaoProfessor : Entity
{
    private QualificacaoProfessor()
    {
    }

    public QualificacaoProfessor(
        Guid professorId,
        TipoQualificacaoProfessor tipo,
        string titulo,
        string? instituicao,
        string? descricao,
        DateOnly? dataConclusao,
        string? documentoComprovacao)
    {
        if (string.IsNullOrWhiteSpace(titulo))
        {
            throw new DomainException("Titulo da qualificacao e obrigatorio.");
        }

        ProfessorId = professorId;
        Tipo = tipo;
        Titulo = titulo.Trim();
        Instituicao = instituicao?.Trim();
        Descricao = descricao?.Trim();
        DataConclusao = dataConclusao;
        DocumentoComprovacao = documentoComprovacao?.Trim();
    }

    public Guid ProfessorId { get; private set; }
    public Professor? Professor { get; private set; }
    public TipoQualificacaoProfessor Tipo { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string? Instituicao { get; private set; }
    public string? Descricao { get; private set; }
    public DateOnly? DataConclusao { get; private set; }
    public string? DocumentoComprovacao { get; private set; }
    public StatusVerificacaoQualificacao StatusVerificacao { get; private set; } = StatusVerificacaoQualificacao.Pendente;
    public DateTime? DataVerificacaoUtc { get; private set; }

    public void Verificar()
    {
        if (StatusVerificacao == StatusVerificacaoQualificacao.Verificada)
        {
            throw new DomainException("Qualificacao ja esta verificada.");
        }

        if (StatusVerificacao == StatusVerificacaoQualificacao.Rejeitada)
        {
            throw new DomainException("Qualificacao rejeitada nao pode ser verificada.");
        }

        StatusVerificacao = StatusVerificacaoQualificacao.Verificada;
        DataVerificacaoUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Rejeitar()
    {
        if (StatusVerificacao == StatusVerificacaoQualificacao.Rejeitada)
        {
            throw new DomainException("Qualificacao ja esta rejeitada.");
        }

        if (StatusVerificacao == StatusVerificacaoQualificacao.Verificada)
        {
            throw new DomainException("Qualificacao verificada nao pode ser rejeitada.");
        }

        StatusVerificacao = StatusVerificacaoQualificacao.Rejeitada;
        DataVerificacaoUtc = DateTime.UtcNow;
        MarkUpdated();
    }
}
