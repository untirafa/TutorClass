using TutorClass.Domain.Common;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Users;

namespace TutorClass.Domain.Financial;

public sealed class CobrancaAssinatura : Entity
{
    private CobrancaAssinatura()
    {
    }

    public CobrancaAssinatura(Guid assinaturaId, Guid pagadorUsuarioId, int ano, int mes, decimal valor, FormaPagamento formaPagamento)
    {
        if (assinaturaId == Guid.Empty || pagadorUsuarioId == Guid.Empty)
        {
            throw new DomainException("Assinatura e pagador sao obrigatorios.");
        }

        if (ano < 2020 || mes < 1 || mes > 12)
        {
            throw new DomainException("Competencia da cobranca invalida.");
        }

        if (valor <= 0)
        {
            throw new DomainException("Valor da cobranca deve ser maior que zero.");
        }

        AssinaturaId = assinaturaId;
        PagadorUsuarioId = pagadorUsuarioId;
        AnoCompetencia = ano;
        MesCompetencia = mes;
        Valor = decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
        FormaPagamento = formaPagamento;
    }

    public Guid AssinaturaId { get; private set; }
    public AssinaturaAluno? Assinatura { get; private set; }
    public Guid PagadorUsuarioId { get; private set; }
    public Usuario? PagadorUsuario { get; private set; }
    public int AnoCompetencia { get; private set; }
    public int MesCompetencia { get; private set; }
    public decimal Valor { get; private set; }
    public FormaPagamento FormaPagamento { get; private set; }
    public StatusPagamentoFinanceiro Status { get; private set; } = StatusPagamentoFinanceiro.Pendente;
    public DateTime? PaidAtUtc { get; private set; }

    public void MarcarComoPago()
    {
        if (Status == StatusPagamentoFinanceiro.Pago)
        {
            return;
        }

        if (Status is StatusPagamentoFinanceiro.Cancelado or StatusPagamentoFinanceiro.Reembolsado)
        {
            throw new DomainException("Cobranca cancelada ou reembolsada nao pode ser paga.");
        }

        Status = StatusPagamentoFinanceiro.Pago;
        PaidAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }
}
