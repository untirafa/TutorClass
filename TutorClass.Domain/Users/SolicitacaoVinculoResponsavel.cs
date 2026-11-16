using TutorClass.Domain.Common;

namespace TutorClass.Domain.Users;

public enum StatusSolicitacaoVinculo
{
    Pendente = 1,
    Aprovada = 2,
    Recusada = 3
}

// Pedido de um responsavel para acompanhar um aluno. O responsavel informa apenas o nome completo
// do aluno; a administracao confere e escolhe o aluno correto antes de criar o vinculo.
public sealed class SolicitacaoVinculoResponsavel : Entity
{
    public const int NomeAlunoMaxLength = 200;
    public const int ParentescoMaxLength = 80;
    public const int MotivoMaxLength = 500;

    private SolicitacaoVinculoResponsavel()
    {
    }

    public SolicitacaoVinculoResponsavel(Guid responsavelId, string nomeAlunoInformado, string? parentesco)
    {
        if (responsavelId == Guid.Empty)
        {
            throw new DomainException("Responsavel e obrigatorio.");
        }

        var nome = string.IsNullOrWhiteSpace(nomeAlunoInformado) ? string.Empty : string.Join(' ', nomeAlunoInformado.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (nome.Length < 5 || !nome.Contains(' '))
        {
            throw new DomainException("Informe o nome completo do aluno (nome e sobrenome).");
        }

        if (nome.Length > NomeAlunoMaxLength)
        {
            throw new DomainException($"O nome do aluno deve possuir no maximo {NomeAlunoMaxLength} caracteres.");
        }

        var grau = string.IsNullOrWhiteSpace(parentesco) ? "Nao informado" : parentesco.Trim();
        if (grau.Length > ParentescoMaxLength)
        {
            throw new DomainException($"O parentesco deve possuir no maximo {ParentescoMaxLength} caracteres.");
        }

        ResponsavelId = responsavelId;
        NomeAlunoInformado = nome;
        Parentesco = grau;
        Status = StatusSolicitacaoVinculo.Pendente;
    }

    public Guid ResponsavelId { get; private set; }
    public string NomeAlunoInformado { get; private set; } = string.Empty;
    public string Parentesco { get; private set; } = string.Empty;
    public StatusSolicitacaoVinculo Status { get; private set; }
    public Guid? AlunoId { get; private set; }
    public Guid? AnalisadaPorId { get; private set; }
    public DateTime? AnalisadaEmUtc { get; private set; }
    public string? Motivo { get; private set; }

    public bool Pendente => Status == StatusSolicitacaoVinculo.Pendente;

    public void Aprovar(Guid administradorId, Guid alunoId)
    {
        EnsurePendente(administradorId);
        if (alunoId == Guid.Empty)
        {
            throw new DomainException("Aluno e obrigatorio.");
        }

        Status = StatusSolicitacaoVinculo.Aprovada;
        AlunoId = alunoId;
        AnalisadaPorId = administradorId;
        AnalisadaEmUtc = DateTime.UtcNow;
        Motivo = null;
        MarkUpdated();
    }

    public void Recusar(Guid administradorId, string? motivo)
    {
        EnsurePendente(administradorId);
        var texto = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        if (texto is null || texto.Length < 3)
        {
            throw new DomainException("Informe o motivo da recusa (pelo menos 3 caracteres).");
        }

        if (texto.Length > MotivoMaxLength)
        {
            throw new DomainException($"O motivo deve possuir no maximo {MotivoMaxLength} caracteres.");
        }

        Status = StatusSolicitacaoVinculo.Recusada;
        AnalisadaPorId = administradorId;
        AnalisadaEmUtc = DateTime.UtcNow;
        Motivo = texto;
        MarkUpdated();
    }

    private void EnsurePendente(Guid administradorId)
    {
        if (administradorId == Guid.Empty)
        {
            throw new DomainException("Administrador e obrigatorio.");
        }

        if (!Pendente)
        {
            throw new DomainException("Esta solicitacao ja foi decidida.");
        }
    }
}
