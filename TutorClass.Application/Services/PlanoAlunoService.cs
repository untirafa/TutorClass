using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Mentoring;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Users;

namespace TutorClass.Application.Services;

public sealed class PlanoAlunoService
{
    public const int LimiteIaFocoPorDia = 7;

    private readonly IApplicationDbContext _db;
    private readonly ResponsavelAlunoService _responsaveis;

    public PlanoAlunoService(IApplicationDbContext db)
        : this(db, new ResponsavelAlunoService(db))
    {
    }

    public PlanoAlunoService(IApplicationDbContext db, ResponsavelAlunoService responsaveis)
    {
        _db = db;
        _responsaveis = responsaveis;
    }

    public Task<PlanoAlunoStatusResponse> ObterStatusAsync(Guid alunoId, CancellationToken cancellationToken)
    {
        EnsureAlunoAtivo(alunoId);
        return Task.FromResult(ToStatus(alunoId));
    }

    public Task<AssinaturaAlunoResponse> AssinarAsync(Guid alunoId, AssinarPlanoRequest request, CancellationToken cancellationToken)
    {
        return AssinarParaBeneficiarioAsync(alunoId, request.BeneficiarioAlunoId ?? alunoId, request, cancellationToken);
    }

    public async Task<AssinaturaAlunoResponse> AssinarParaBeneficiarioAsync(Guid pagadorUsuarioId, Guid beneficiarioAlunoId, AssinarPlanoRequest request, CancellationToken cancellationToken)
    {
        EnsurePagadorPodeAcessarAluno(pagadorUsuarioId, beneficiarioAlunoId);

        if (request.Plano == PlanoAluno.Gratuito)
        {
            throw new DomainException("Plano Gratuito nao gera assinatura. Cancele a assinatura ativa para voltar ao Gratuito.");
        }

        var assinaturasAtivas = _db.AssinaturasAluno.Where(a => a.AlunoId == beneficiarioAlunoId && a.Ativa).ToArray();
        var tinhaMentoriaAtiva = assinaturasAtivas.Any(a => a.Plano == PlanoAluno.Mentoria);

        foreach (var assinaturaAtiva in assinaturasAtivas)
        {
            assinaturaAtiva.Cancelar();
        }

        if (tinhaMentoriaAtiva && request.Plano != PlanoAluno.Mentoria)
        {
            EncerrarMentoriasPorCancelamentoPlano(beneficiarioAlunoId);
        }

        var jaUsouPromocao = _db.AssinaturasAluno.Any(a => a.AlunoId == beneficiarioAlunoId && a.UsouPromocaoPrimeiraAssinatura);
        var valorMensal = GetValorMensal(request.Plano);
        var valorPrimeiroMes = jaUsouPromocao ? valorMensal : GetValorPromocional(request.Plano);
        var assinatura = new AssinaturaAluno(beneficiarioAlunoId, pagadorUsuarioId, request.Plano, valorMensal, valorPrimeiroMes, !jaUsouPromocao);

        _db.AddEntity(assinatura);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(assinatura);
    }

    public Task<PlanoAlunoStatusResponse> ObterStatusParaUsuarioAsync(Guid usuarioId, Guid alunoId, CancellationToken cancellationToken)
    {
        EnsurePagadorPodeAcessarAluno(usuarioId, alunoId);
        return Task.FromResult(ToStatus(alunoId));
    }

    public async Task<PlanoAlunoStatusResponse> CancelarAtualAsync(Guid alunoId, CancellationToken cancellationToken)
    {
        EnsureAlunoAtivo(alunoId);

        var ativa = _db.AssinaturasAluno
            .Where(a => a.AlunoId == alunoId && a.Ativa)
            .OrderByDescending(a => a.DataInicioUtc)
            .FirstOrDefault();

        if (ativa is not null)
        {
            var eraMentoria = ativa.Plano == PlanoAluno.Mentoria;
            ativa.Cancelar();

            if (eraMentoria)
            {
                EncerrarMentoriasPorCancelamentoPlano(alunoId);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ToStatus(alunoId);
    }

    public Task<UsoIaAlunoResponse> PodeUsarIaAsync(Guid alunoId, CancellationToken cancellationToken)
    {
        EnsureAlunoAtivo(alunoId);
        return Task.FromResult(BuildUsoIaResponse(alunoId, registrar: false));
    }

    public async Task<UsoIaAlunoResponse> RegistrarUsoIaAsync(Guid alunoId, CancellationToken cancellationToken)
    {
        EnsureAlunoAtivo(alunoId);
        var response = BuildUsoIaResponse(alunoId, registrar: false);
        if (!response.Permitido)
        {
            throw new DomainException(response.Mensagem ?? "Uso de IA nao permitido pelo plano atual.");
        }

        if (response.PlanoAtual == PlanoAluno.Mentoria)
        {
            return response with { Mensagem = "Uso de IA registrado sem limite diario imposto pelo plano." };
        }

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var uso = _db.UsosIaAluno.SingleOrDefault(u => u.AlunoId == alunoId && u.Data == hoje);
        if (uso is null)
        {
            uso = new UsoIaAluno(alunoId, hoje);
            _db.AddEntity(uso);
        }

        uso.RegistrarUso();
        await _db.SaveChangesAsync(cancellationToken);
        return BuildUsoIaResponse(alunoId, registrar: true);
    }

    public PlanoAluno ObterPlanoAtual(Guid alunoId)
    {
        return _db.AssinaturasAluno
            .Where(a => a.AlunoId == alunoId && a.Ativa)
            .OrderByDescending(a => a.DataInicioUtc)
            .Select(a => a.Plano)
            .FirstOrDefault();
    }

    public bool PossuiMentoriaAtiva(Guid alunoId)
    {
        return ObterPlanoAtual(alunoId) == PlanoAluno.Mentoria;
    }

    private UsoIaAlunoResponse BuildUsoIaResponse(Guid alunoId, bool registrar)
    {
        var plano = ObterPlanoAtual(alunoId);
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var quantidade = _db.UsosIaAluno.SingleOrDefault(u => u.AlunoId == alunoId && u.Data == hoje)?.Quantidade ?? 0;

        return plano switch
        {
            PlanoAluno.Gratuito => new UsoIaAlunoResponse(plano, false, quantidade, 0, "Seu plano atual nao possui IA educacional."),
            PlanoAluno.Foco when quantidade >= LimiteIaFocoPorDia && !registrar => new UsoIaAlunoResponse(plano, false, quantidade, LimiteIaFocoPorDia, "Limite diario de mensagens da IA atingido."),
            PlanoAluno.Foco => new UsoIaAlunoResponse(plano, true, quantidade, LimiteIaFocoPorDia, null),
            PlanoAluno.Mentoria => new UsoIaAlunoResponse(plano, true, quantidade, null, null),
            _ => new UsoIaAlunoResponse(PlanoAluno.Gratuito, false, quantidade, 0, "Seu plano atual nao possui IA educacional.")
        };
    }

    private PlanoAlunoStatusResponse ToStatus(Guid alunoId)
    {
        var plano = ObterPlanoAtual(alunoId);
        var assinaturaAtiva = plano != PlanoAluno.Gratuito;
        var jaUsouPromocao = _db.AssinaturasAluno.Any(a => a.AlunoId == alunoId && a.UsouPromocaoPrimeiraAssinatura);

        return new PlanoAlunoStatusResponse(
            alunoId,
            plano,
            assinaturaAtiva,
            plano is PlanoAluno.Foco or PlanoAluno.Mentoria,
            plano == PlanoAluno.Foco ? LimiteIaFocoPorDia : plano == PlanoAluno.Gratuito ? 0 : null,
            plano == PlanoAluno.Mentoria,
            plano == PlanoAluno.Mentoria,
            jaUsouPromocao);
    }

    private void EnsureAlunoAtivo(Guid alunoId)
    {
        if (!_db.Usuarios.OfType<Aluno>().Any(a => a.Id == alunoId && a.Ativo))
        {
            throw new DomainException("Aluno nao encontrado ou inativo.");
        }
    }

    public void EnsurePagadorPodeAcessarAluno(Guid usuarioId, Guid alunoId)
    {
        EnsureAlunoAtivo(alunoId);

        var usuario = _db.Usuarios.SingleOrDefault(u => u.Id == usuarioId && u.Ativo)
            ?? throw new DomainException("Pagador nao encontrado ou inativo.");

        if (usuario is Aluno && usuario.Id == alunoId)
        {
            return;
        }

        if (usuario is Responsavel)
        {
            _responsaveis.ValidarVinculo(usuarioId, alunoId);
            return;
        }

        throw new UnauthorizedAccessException("Usuario nao pode acessar dados financeiros deste aluno.");
    }

    private void EncerrarMentoriasPorCancelamentoPlano(Guid alunoId)
    {
        foreach (var mentoria in _db.MentoriasAlunoProfessor.Where(m => m.AlunoId == alunoId && m.Ativo).ToArray())
        {
            mentoria.Encerrar(MotivoEncerramentoMentoria.CancelamentoPlano);
        }
    }

    private static decimal GetValorMensal(PlanoAluno plano) => plano switch
    {
        PlanoAluno.Foco => 29.99m,
        PlanoAluno.Mentoria => 79.99m,
        _ => 0m
    };

    private static decimal GetValorPromocional(PlanoAluno plano) => plano switch
    {
        PlanoAluno.Foco => 19.99m,
        PlanoAluno.Mentoria => 59.99m,
        _ => 0m
    };

    private static AssinaturaAlunoResponse ToResponse(AssinaturaAluno assinatura) => new(
        assinatura.Id,
        assinatura.AlunoId,
        assinatura.PagadorUsuarioId,
        assinatura.Plano,
        assinatura.Ativa,
        assinatura.ValorMensal,
        assinatura.ValorCobradoPrimeiroMes,
        assinatura.UsouPromocaoPrimeiraAssinatura,
        assinatura.DataInicioUtc,
        assinatura.DataFimUtc,
        assinatura.DataCancelamentoUtc);
}
