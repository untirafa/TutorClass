using TutorClass.Domain.Common;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;

namespace TutorClass.Domain.Financial;

public sealed class PagamentoAula : Entity
{
    public const decimal PercentualComissaoPadrao = 12m;

    private PagamentoAula()
    {
    }

    public PagamentoAula(Guid aulaId, Guid alunoId, Guid pagadorUsuarioId, Guid professorId, decimal valorBruto)
        : this(aulaId, alunoId, pagadorUsuarioId, professorId, valorBruto, FormaPagamento.Pix)
    {
    }

    public PagamentoAula(Guid aulaId, Guid alunoId, Guid pagadorUsuarioId, Guid professorId, decimal valorBruto, FormaPagamento formaPagamento)
    {
        if (aulaId == Guid.Empty || alunoId == Guid.Empty || pagadorUsuarioId == Guid.Empty || professorId == Guid.Empty)
        {
            throw new DomainException("Aula, aluno, pagador e professor sao obrigatorios.");
        }

        if (valorBruto <= 0)
        {
            throw new DomainException("Valor bruto da aula deve ser maior que zero.");
        }

        AulaId = aulaId;
        AlunoId = alunoId;
        PagadorUsuarioId = pagadorUsuarioId;
        ProfessorId = professorId;
        ValorBruto = decimal.Round(valorBruto, 2, MidpointRounding.AwayFromZero);
        PercentualComissao = PercentualComissaoPadrao;
        ValorComissao = decimal.Round(ValorBruto * PercentualComissao / 100m, 2, MidpointRounding.AwayFromZero);
        ValorLiquidoProfessor = ValorBruto - ValorComissao;
        FormaPagamento = formaPagamento;
    }

    public Guid AulaId { get; private set; }
    public Aula? Aula { get; private set; }
    public Guid AlunoId { get; private set; }
    public Aluno? Aluno { get; private set; }
    public Guid PagadorUsuarioId { get; private set; }
    public Usuario? PagadorUsuario { get; private set; }
    public Guid ProfessorId { get; private set; }
    public Professor? Professor { get; private set; }
    public decimal ValorBruto { get; private set; }
    public decimal PercentualComissao { get; private set; }
    public decimal ValorComissao { get; private set; }
    public decimal ValorLiquidoProfessor { get; private set; }
    public FormaPagamento FormaPagamento { get; private set; }
    public StatusPagamentoFinanceiro Status { get; private set; } = StatusPagamentoFinanceiro.Pendente;
    public DateTime? DataPagamentoUtc { get; private set; }

    public void MarcarComoPago()
    {
        if (Status == StatusPagamentoFinanceiro.Pago)
        {
            return;
        }

        if (Status is StatusPagamentoFinanceiro.Cancelado or StatusPagamentoFinanceiro.Reembolsado)
        {
            throw new DomainException("Pagamento cancelado ou reembolsado nao pode ser marcado como pago.");
        }

        Status = StatusPagamentoFinanceiro.Pago;
        DataPagamentoUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Cancelar()
    {
        if (Status == StatusPagamentoFinanceiro.Pago)
        {
            throw new DomainException("Pagamento pago deve ser reembolsado, nao cancelado.");
        }

        Status = StatusPagamentoFinanceiro.Cancelado;
        MarkUpdated();
    }
}
