using TutorClass.Domain.Common;

namespace TutorClass.Domain.Moderation;

public sealed class Denuncia : Entity
{
    public const int DescricaoMaxLength = 500;
    public const int ObservacaoMaxLength = 500;
    public const int ResumoMaxLength = 500;

    private Denuncia()
    {
    }

    public Denuncia(
        Guid denuncianteId,
        Guid alvoUsuarioId,
        TipoAlvoDenuncia tipoAlvo,
        string alvoId,
        MotivoDenuncia motivo,
        string? descricao,
        string resumoAlvo,
        Guid? vinculoMentoriaId = null)
    {
        if (denuncianteId == Guid.Empty) throw new DomainException("Denunciante e obrigatorio.");
        if (alvoUsuarioId == Guid.Empty) throw new DomainException("Usuario denunciado e obrigatorio.");
        if (denuncianteId == alvoUsuarioId) throw new DomainException("Voce nao pode denunciar a si mesmo.");
        if (string.IsNullOrWhiteSpace(alvoId)) throw new DomainException("Conteudo denunciado e obrigatorio.");
        if (!Enum.IsDefined(tipoAlvo)) throw new DomainException("Tipo de denuncia invalido.");
        if (!Enum.IsDefined(motivo)) throw new DomainException("Motivo da denuncia invalido.");

        var texto = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        if (texto is { Length: > DescricaoMaxLength })
        {
            throw new DomainException($"Descricao deve possuir no maximo {DescricaoMaxLength} caracteres.");
        }

        if (motivo == MotivoDenuncia.Outro && (texto is null || texto.Length < 10))
        {
            throw new DomainException("Para o motivo \"Outro\", descreva o ocorrido com pelo menos 10 caracteres.");
        }

        var resumo = string.IsNullOrWhiteSpace(resumoAlvo) ? "(sem conteudo)" : resumoAlvo.Trim();
        if (resumo.Length > ResumoMaxLength)
        {
            resumo = resumo[..ResumoMaxLength];
        }

        DenuncianteId = denuncianteId;
        AlvoUsuarioId = alvoUsuarioId;
        TipoAlvo = tipoAlvo;
        AlvoId = alvoId.Trim();
        Motivo = motivo;
        Descricao = texto;
        ResumoAlvo = resumo;
        VinculoMentoriaId = vinculoMentoriaId;
        Status = StatusDenuncia.Aberta;
        Sancao = SancaoDenuncia.Nenhuma;
    }

    public Guid DenuncianteId { get; private set; }
    public Guid AlvoUsuarioId { get; private set; }
    public TipoAlvoDenuncia TipoAlvo { get; private set; }
    public string AlvoId { get; private set; } = string.Empty;
    public MotivoDenuncia Motivo { get; private set; }
    public string? Descricao { get; private set; }
    public string ResumoAlvo { get; private set; } = string.Empty;
    public Guid? VinculoMentoriaId { get; private set; }
    public StatusDenuncia Status { get; private set; }
    public SancaoDenuncia Sancao { get; private set; }
    public string? ObservacaoModeracao { get; private set; }
    public Guid? AnalisadaPorId { get; private set; }
    public DateTime? AnalisadaEmUtc { get; private set; }
    public DateTime? ContaReativadaEmUtc { get; private set; }

    public bool Encerrada => Status is StatusDenuncia.Procedente or StatusDenuncia.Improcedente;

    public void IniciarAnalise(Guid administradorId)
    {
        if (Encerrada) throw new DomainException("Esta denuncia ja foi decidida.");
        if (administradorId == Guid.Empty) throw new DomainException("Administrador e obrigatorio.");

        Status = StatusDenuncia.EmAnalise;
        AnalisadaPorId = administradorId;
        MarkUpdated();
    }

    public void Decidir(Guid administradorId, bool procedente, SancaoDenuncia sancao, string? observacao)
    {
        if (Encerrada) throw new DomainException("Esta denuncia ja foi decidida.");
        if (administradorId == Guid.Empty) throw new DomainException("Administrador e obrigatorio.");
        if (!Enum.IsDefined(sancao)) throw new DomainException("Sancao invalida.");

        var texto = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim();
        if (texto is null || texto.Length < 3)
        {
            throw new DomainException("Informe a justificativa da decisao (pelo menos 3 caracteres).");
        }

        if (texto.Length > ObservacaoMaxLength)
        {
            throw new DomainException($"Justificativa deve possuir no maximo {ObservacaoMaxLength} caracteres.");
        }

        if (!procedente && sancao != SancaoDenuncia.Nenhuma)
        {
            throw new DomainException("Denuncia improcedente nao pode ter sancao.");
        }

        Status = procedente ? StatusDenuncia.Procedente : StatusDenuncia.Improcedente;
        Sancao = procedente ? sancao : SancaoDenuncia.Nenhuma;
        ObservacaoModeracao = texto;
        AnalisadaPorId = administradorId;
        AnalisadaEmUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void RegistrarReativacaoDaConta()
    {
        if (Status != StatusDenuncia.Procedente || Sancao != SancaoDenuncia.Suspensao)
        {
            throw new DomainException("Somente denuncias com suspensao aplicada permitem reativar a conta.");
        }

        if (ContaReativadaEmUtc is not null)
        {
            throw new DomainException("A conta ja foi reativada.");
        }

        ContaReativadaEmUtc = DateTime.UtcNow;
        MarkUpdated();
    }
}
