using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Financial;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Notifications;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Users;

namespace TutorClass.Application.Services;

public sealed class FinanceiroService
{
    private readonly IApplicationDbContext _db;
    private readonly PlanoAlunoService _planos;
    private readonly NotificacaoService _notificacoes;

    public FinanceiroService(IApplicationDbContext db)
        : this(db, new PlanoAlunoService(db), new NotificacaoService(db))
    {
    }

    public FinanceiroService(IApplicationDbContext db, PlanoAlunoService planos)
        : this(db, planos, new NotificacaoService(db))
    {
    }

    public FinanceiroService(IApplicationDbContext db, PlanoAlunoService planos, NotificacaoService notificacoes)
    {
        _db = db;
        _planos = planos;
        _notificacoes = notificacoes;
    }

    public DemonstrativoComissaoAulaResponse CalcularComissao(decimal valorBruto)
    {
        var pagamento = new PagamentoAula(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), valorBruto);
        return new DemonstrativoComissaoAulaResponse(
            pagamento.ValorBruto,
            pagamento.PercentualComissao,
            pagamento.ValorComissao,
            pagamento.ValorLiquidoProfessor);
    }

    public async Task<PagamentoAulaResponse> PagarAulaAsync(Guid aulaId, Guid pagadorUsuarioId, CancellationToken cancellationToken)
    {
        return await PagarAulaAsync(aulaId, pagadorUsuarioId, FormaPagamento.Pix, cancellationToken);
    }

    public async Task<PagamentoAulaResponse> PagarAulaAsync(Guid aulaId, Guid pagadorUsuarioId, FormaPagamento formaPagamento, CancellationToken cancellationToken)
    {
        var aula = await _db.FindAsync<Aula>(aulaId, cancellationToken)
            ?? throw new DomainException("Aula nao encontrada.");

        if (aula.Status == StatusAula.Cancelada)
        {
            throw new DomainException("Aula cancelada nao pode receber pagamento.");
        }

        _planos.EnsurePagadorPodeAcessarAluno(pagadorUsuarioId, aula.AlunoId);

        if (_db.PagamentosAula.Any(p => p.AulaId == aula.Id && p.Status != StatusPagamentoFinanceiro.Cancelado && p.Status != StatusPagamentoFinanceiro.Reembolsado))
        {
            throw new DomainException("Aula ja possui pagamento registrado.");
        }

        var pagamento = new PagamentoAula(aula.Id, aula.AlunoId, pagadorUsuarioId, aula.ProfessorId, aula.PrecoAcordado, formaPagamento);
        pagamento.MarcarComoPago();
        _db.AddEntity(pagamento);
        NotificarPagamentoAulaConfirmado(pagamento);
        await _db.SaveChangesAsync(cancellationToken);
        return ToPagamentoAulaResponse(pagamento);
    }

    public async Task<CobrancaAssinaturaResponse> GerarCobrancaAssinaturaAsync(Guid assinaturaId, Guid usuarioId, GerarCobrancaAssinaturaRequest request, CancellationToken cancellationToken)
    {
        var assinatura = await _db.FindAsync<AssinaturaAluno>(assinaturaId, cancellationToken)
            ?? throw new DomainException("Assinatura nao encontrada.");

        _planos.EnsurePagadorPodeAcessarAluno(usuarioId, assinatura.AlunoId);

        if (assinatura.Plano == PlanoAluno.Gratuito)
        {
            throw new DomainException("Plano Gratuito nao gera cobranca mensal.");
        }

        if (_db.CobrancasAssinatura.Any(c =>
            c.AssinaturaId == assinatura.Id &&
            c.AnoCompetencia == request.AnoCompetencia &&
            c.MesCompetencia == request.MesCompetencia))
        {
            throw new DomainException("Assinatura ja possui cobranca para esta competencia.");
        }

        var valor = CalcularValorCobranca(assinatura, request.AnoCompetencia, request.MesCompetencia);
        var cobranca = new CobrancaAssinatura(assinatura.Id, assinatura.PagadorUsuarioId, request.AnoCompetencia, request.MesCompetencia, valor, request.FormaPagamento);
        cobranca.MarcarComoPago();

        _db.AddEntity(cobranca);
        NotificarCobrancaAssinaturaConfirmada(cobranca, assinatura);
        await _db.SaveChangesAsync(cancellationToken);
        return ToCobrancaAssinaturaResponse(cobranca, assinatura);
    }

    public Task<IReadOnlyCollection<FinanceiroAlunoItemResponse>> HistoricoAlunoAsync(Guid usuarioId, Guid alunoId, CancellationToken cancellationToken)
    {
        _planos.EnsurePagadorPodeAcessarAluno(usuarioId, alunoId);

        var assinaturasAluno = _db.AssinaturasAluno
            .Where(a => a.AlunoId == alunoId && (a.PagadorUsuarioId == usuarioId || a.AlunoId == usuarioId || _db.ResponsaveisAlunos.Any(ra => ra.ResponsavelId == usuarioId && ra.AlunoId == a.AlunoId)))
            .ToArray();

        var assinaturas = assinaturasAluno
            .Select(a => new FinanceiroAlunoItemResponse(
                "Assinatura",
                a.Id,
                a.AlunoId,
                a.PagadorUsuarioId,
                a.ValorCobradoPrimeiroMes,
                $"Plano {a.Plano}",
                a.DataInicioUtc,
                a.Ativa ? "Ativa" : "Cancelada"));

        var assinaturasPorId = assinaturasAluno.ToDictionary(a => a.Id);
        var assinaturaIds = assinaturasPorId.Keys.ToArray();

        var cobrancas = _db.CobrancasAssinatura
            .Where(c => assinaturaIds.Contains(c.AssinaturaId))
            .ToArray()
            .Select(c => new FinanceiroAlunoItemResponse(
                "CobrancaAssinatura",
                c.Id,
                assinaturasPorId[c.AssinaturaId].AlunoId,
                c.PagadorUsuarioId,
                c.Valor,
                $"Cobranca {c.MesCompetencia:00}/{c.AnoCompetencia} - Plano {assinaturasPorId[c.AssinaturaId].Plano}",
                c.PaidAtUtc ?? c.CreatedAtUtc,
                c.Status.ToString()));

        var aulas = _db.PagamentosAula
            .Where(p => p.AlunoId == alunoId && (p.PagadorUsuarioId == usuarioId || p.AlunoId == usuarioId || _db.ResponsaveisAlunos.Any(ra => ra.ResponsavelId == usuarioId && ra.AlunoId == p.AlunoId)))
            .ToArray()
            .Select(p => new FinanceiroAlunoItemResponse(
                "Aula",
                p.Id,
                p.AlunoId,
                p.PagadorUsuarioId,
                p.ValorBruto,
                $"Aula com {GetProfessorNome(p.ProfessorId)}",
                p.DataPagamentoUtc ?? p.CreatedAtUtc,
                p.Status.ToString()));

        return Task.FromResult<IReadOnlyCollection<FinanceiroAlunoItemResponse>>(assinaturas.Concat(cobrancas).Concat(aulas).OrderByDescending(i => i.DataUtc).ToArray());
    }

    public Task<ResumoFinanceiroProfessorResponse> ResumoProfessorAsync(Guid professorId, int ano, int mes, CancellationToken cancellationToken)
    {
        var inicio = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddMonths(1);

        var aulas = _db.PagamentosAula
            .Where(p => p.ProfessorId == professorId && p.Status == StatusPagamentoFinanceiro.Pago && p.DataPagamentoUtc >= inicio && p.DataPagamentoUtc < fim)
            .ToArray()
            .Select(p => new FinanceiroProfessorItemResponse(
                "Aula",
                p.Id,
                p.AulaId,
                p.AlunoId,
                GetAlunoNome(p.AlunoId),
                p.ValorBruto,
                p.ValorComissao,
                p.ValorLiquidoProfessor,
                0m,
                p.DataPagamentoUtc ?? p.CreatedAtUtc,
                p.Status.ToString()))
            .ToArray();

        var adicionais = _db.AdicionaisMentoria
            .Where(a => a.ProfessorId == professorId && a.DataGeracaoUtc >= inicio && a.DataGeracaoUtc < fim)
            .ToArray()
            .Select(a => new FinanceiroProfessorItemResponse(
                "AdicionalMentoria",
                a.Id,
                a.AulaId,
                a.AlunoId,
                GetAlunoNome(a.AlunoId),
                0m,
                0m,
                0m,
                a.Valor,
                a.DataGeracaoUtc,
                a.Status.ToString()))
            .ToArray();

        var itens = aulas.Concat(adicionais).OrderByDescending(i => i.DataUtc).ToArray();
        var valorBruto = aulas.Sum(a => a.ValorBruto);
        var comissao = aulas.Sum(a => a.ValorComissao);
        var liquidoAulas = aulas.Sum(a => a.ValorLiquidoProfessor);
        var adicionaisTotal = adicionais.Sum(a => a.ValorAdicionalMentoria);

        return Task.FromResult(new ResumoFinanceiroProfessorResponse(
            professorId,
            ano,
            mes,
            valorBruto,
            comissao,
            liquidoAulas,
            adicionaisTotal,
            liquidoAulas + adicionaisTotal,
            itens));
    }

    private PagamentoAulaResponse ToPagamentoAulaResponse(PagamentoAula p) => new(
        p.Id,
        p.AulaId,
        p.AlunoId,
        GetAlunoNome(p.AlunoId),
        p.PagadorUsuarioId,
        GetUsuarioNome(p.PagadorUsuarioId),
        p.ProfessorId,
        GetProfessorNome(p.ProfessorId),
        p.ValorBruto,
        p.PercentualComissao,
        p.ValorComissao,
        p.ValorLiquidoProfessor,
        p.FormaPagamento,
        p.Status,
        p.CreatedAtUtc,
        p.DataPagamentoUtc);

    private CobrancaAssinaturaResponse ToCobrancaAssinaturaResponse(CobrancaAssinatura c, AssinaturaAluno assinatura) => new(
        c.Id,
        c.AssinaturaId,
        assinatura.AlunoId,
        c.PagadorUsuarioId,
        GetUsuarioNome(c.PagadorUsuarioId),
        assinatura.Plano,
        c.AnoCompetencia,
        c.MesCompetencia,
        c.Valor,
        c.FormaPagamento,
        c.Status,
        c.CreatedAtUtc,
        c.PaidAtUtc);

    private void NotificarPagamentoAulaConfirmado(PagamentoAula pagamento)
    {
        var alunoNome = GetAlunoNome(pagamento.AlunoId);
        var professorNome = GetProfessorNome(pagamento.ProfessorId);

        _notificacoes.CriarSeNaoExistir(
            pagamento.PagadorUsuarioId,
            TipoNotificacao.PagamentoConfirmado,
            "Pagamento confirmado",
            $"Pagamento da aula de {alunoNome} com {professorNome} confirmado.",
            "/financeiro",
            pagamento.Id,
            $"PAGAMENTO_AULA_CONFIRMADO:{pagamento.Id}:{pagamento.PagadorUsuarioId}");

        if (pagamento.PagadorUsuarioId != pagamento.AlunoId)
        {
            _notificacoes.CriarSeNaoExistir(
                pagamento.AlunoId,
                TipoNotificacao.PagamentoConfirmado,
                "Pagamento da aula confirmado",
                $"O pagamento da sua aula com {professorNome} foi confirmado.",
                "/minhas-aulas",
                pagamento.Id,
                $"PAGAMENTO_AULA_CONFIRMADO:{pagamento.Id}:{pagamento.AlunoId}");
        }
    }

    private void NotificarCobrancaAssinaturaConfirmada(CobrancaAssinatura cobranca, AssinaturaAluno assinatura)
    {
        _notificacoes.CriarSeNaoExistir(
            cobranca.PagadorUsuarioId,
            TipoNotificacao.AssinaturaConfirmada,
            "Assinatura confirmada",
            $"Cobranca do plano {assinatura.Plano} confirmada.",
            "/financeiro",
            cobranca.Id,
            $"ASSINATURA_CONFIRMADA:{cobranca.Id}:{cobranca.PagadorUsuarioId}");
    }

    private static decimal CalcularValorCobranca(AssinaturaAluno assinatura, int ano, int mes)
    {
        return assinatura.UsouPromocaoPrimeiraAssinatura &&
            assinatura.DataInicioUtc.Year == ano &&
            assinatura.DataInicioUtc.Month == mes
                ? assinatura.ValorCobradoPrimeiroMes
                : assinatura.ValorMensal;
    }

    private string GetAlunoNome(Guid alunoId) => _db.Usuarios.OfType<Aluno>().Where(a => a.Id == alunoId).Select(a => a.Nome).SingleOrDefault() ?? "Aluno";
    private string GetProfessorNome(Guid professorId) => _db.Professores.Where(p => p.Id == professorId).Select(p => p.Nome).SingleOrDefault() ?? "Professor";
    private string GetUsuarioNome(Guid usuarioId) => _db.Usuarios.Where(u => u.Id == usuarioId).Select(u => u.Nome).SingleOrDefault() ?? "Usuario";
}
