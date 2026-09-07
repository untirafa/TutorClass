using TutorClass.Domain.Common;
using TutorClass.Domain.Users;

namespace TutorClass.Domain.Plans;

public sealed class AssinaturaAluno : Entity
{
    private AssinaturaAluno()
    {
    }

    public AssinaturaAluno(Guid alunoId, PlanoAluno plano, decimal valorMensal, decimal valorCobradoPrimeiroMes, bool usouPromocaoPrimeiraAssinatura)
        : this(alunoId, alunoId, plano, valorMensal, valorCobradoPrimeiroMes, usouPromocaoPrimeiraAssinatura)
    {
    }

    public AssinaturaAluno(Guid alunoId, Guid pagadorUsuarioId, PlanoAluno plano, decimal valorMensal, decimal valorCobradoPrimeiroMes, bool usouPromocaoPrimeiraAssinatura)
    {
        if (alunoId == Guid.Empty)
        {
            throw new DomainException("Aluno e obrigatorio.");
        }

        if (pagadorUsuarioId == Guid.Empty)
        {
            throw new DomainException("Pagador e obrigatorio.");
        }

        if (plano == PlanoAluno.Gratuito)
        {
            throw new DomainException("Plano Gratuito nao gera assinatura paga.");
        }

        if (valorMensal <= 0 || valorCobradoPrimeiroMes <= 0)
        {
            throw new DomainException("Valores da assinatura devem ser maiores que zero.");
        }

        AlunoId = alunoId;
        PagadorUsuarioId = pagadorUsuarioId;
        Plano = plano;
        DataInicioUtc = DateTime.UtcNow;
        ValorMensal = valorMensal;
        ValorCobradoPrimeiroMes = valorCobradoPrimeiroMes;
        UsouPromocaoPrimeiraAssinatura = usouPromocaoPrimeiraAssinatura;
    }

    public Guid AlunoId { get; private set; }
    public Aluno? Aluno { get; private set; }
    public Guid PagadorUsuarioId { get; private set; }
    public Usuario? PagadorUsuario { get; private set; }
    public PlanoAluno Plano { get; private set; }
    public DateTime DataInicioUtc { get; private set; }
    public DateTime? DataFimUtc { get; private set; }
    public bool Ativa { get; private set; } = true;
    public decimal ValorMensal { get; private set; }
    public decimal ValorCobradoPrimeiroMes { get; private set; }
    public bool UsouPromocaoPrimeiraAssinatura { get; private set; }
    public DateTime? DataCancelamentoUtc { get; private set; }

    public void Cancelar()
    {
        if (!Ativa)
        {
            return;
        }

        Ativa = false;
        DataCancelamentoUtc = DateTime.UtcNow;
        DataFimUtc = DataCancelamentoUtc;
        MarkUpdated();
    }
}
