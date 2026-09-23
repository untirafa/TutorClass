using System.Collections.Concurrent;
using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Users;

namespace TutorClass.Application.Services;

public sealed class TutorIaService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> AlunoLocks = new();

    private readonly IApplicationDbContext _db;
    private readonly ITutorIaProvider _provider;
    private readonly IContextoIaService _contexto;
    private readonly TutorIaMemoria _memoria;

    public TutorIaService(IApplicationDbContext db, ITutorIaProvider provider, IContextoIaService? contexto = null, TutorIaMemoria? memoria = null)
    {
        _db = db;
        _provider = provider;
        _contexto = contexto ?? new ContextoIaService(db);
        _memoria = memoria ?? new TutorIaMemoria();
    }

    public Task<TutorIaStatusResponse> ObterStatusAsync(Guid alunoId, CancellationToken cancellationToken)
    {
        EnsureAlunoAtivo(alunoId);
        return Task.FromResult(BuildStatus(alunoId));
    }

    public Task<TutorIaChatResponse> ConversarAsync(Guid alunoId, TutorIaChatRequest request, CancellationToken cancellationToken)
        => ConversarAsync(alunoId, request, null, cancellationToken);

    public async Task<TutorIaChatResponse> ConversarAsync(Guid alunoId, TutorIaChatRequest request,
        Func<string, CancellationToken, Task>? receberTexto, CancellationToken cancellationToken)
    {
        EnsureAlunoAtivo(alunoId);

        var alunoLock = AlunoLocks.GetOrAdd(alunoId, _ => new SemaphoreSlim(1, 1));
        await alunoLock.WaitAsync(cancellationToken);
        try
        {
            var statusAntes = BuildStatus(alunoId);
            if (!statusAntes.PossuiAcesso)
            {
                return Bloqueado(statusAntes);
            }

            if (string.IsNullOrWhiteSpace(request.Mensagem))
                throw new DomainException("Mensagem para a TutorIA e obrigatoria.");
            if (request.Mensagem.Length > 4000)
                throw new DomainException("Mensagem para a TutorIA deve possuir no maximo 4000 caracteres.");

            if (TutorIaModeracao.DeveBloquear(request.Mensagem))
                return new TutorIaChatResponse(TutorIaPrompts.Nome, statusAntes.PossuiAcesso,
                    statusAntes.Plano, TutorIaModeracao.Motivo, TutorIaModeracao.Aviso, null,
                    statusAntes.LimiteDiario, statusAntes.UsadasHoje, statusAntes.RestantesHoje,
                    statusAntes.LimiteAtingido, false, false);

            var contexto = await _contexto.ObterContextoAsync(alunoId, cancellationToken);
            string resposta;
            try
            {
                var pergunta = new TutorIaProviderRequest(alunoId, statusAntes.Plano, request.Mensagem.Trim(),
                    GetHojeBrasil(), contexto, _memoria.Obter(alunoId));
                resposta = receberTexto is null
                    ? await _provider.ResponderAsync(pergunta, cancellationToken)
                    : await _provider.ResponderProgressivamenteAsync(pergunta, receberTexto, cancellationToken);
                if (string.IsNullOrWhiteSpace(resposta)) throw new InvalidOperationException("Resposta vazia.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch
            {
                var statusAtual = BuildStatus(alunoId);
                return new TutorIaChatResponse(
                    TutorIaPrompts.Nome,
                    false,
                    statusAtual.Plano,
                    "ERRO_TUTORIA",
                    "A TutorIA está temporariamente indisponível. Tente novamente em alguns instantes.",
                    null,
                    statusAtual.LimiteDiario,
                    statusAtual.UsadasHoje,
                    statusAtual.RestantesHoje,
                    statusAtual.LimiteAtingido,
                    false,
                    false);
            }

            RegistrarUso(alunoId);
            await _db.SaveChangesAsync(cancellationToken);

            _memoria.Registrar(alunoId, request.Mensagem.Trim(), resposta);
            var statusDepois = BuildStatus(alunoId);
            return new TutorIaChatResponse(
                TutorIaPrompts.Nome,
                true,
                statusDepois.Plano,
                null,
                "Resposta gerada pela TutorIA.",
                resposta,
                statusDepois.LimiteDiario,
                statusDepois.UsadasHoje,
                statusDepois.RestantesHoje,
                statusDepois.LimiteAtingido,
                false,
                false);
        }
        finally
        {
            alunoLock.Release();
        }
    }

    private TutorIaStatusResponse BuildStatus(Guid alunoId)
    {
        var plano = ObterPlanoAtual(alunoId);
        var hoje = GetHojeBrasil();
        var usadasHoje = _db.UsosIaAluno.SingleOrDefault(u => u.AlunoId == alunoId && u.Data == hoje)?.Quantidade ?? 0;

        return plano switch
        {
            PlanoAluno.Gratuito => new TutorIaStatusResponse(
                TutorIaPrompts.Nome,
                false,
                plano,
                0,
                usadasHoje,
                0,
                false,
                "PLANO_SEM_TUTORIA",
                true,
                "A TutorIA nao esta disponivel no plano Gratuito."),

            PlanoAluno.Foco when usadasHoje >= PlanoAlunoService.LimiteIaFocoPorDia => new TutorIaStatusResponse(
                TutorIaPrompts.Nome,
                false,
                plano,
                PlanoAlunoService.LimiteIaFocoPorDia,
                usadasHoje,
                0,
                true,
                "LIMITE_DIARIO_ATINGIDO",
                true,
                "Voce atingiu o limite diario da TutorIA no plano Foco. Suas 7 mensagens de hoje ja foram utilizadas. Seu acesso sera renovado automaticamente amanha."),

            PlanoAluno.Foco => new TutorIaStatusResponse(
                TutorIaPrompts.Nome,
                true,
                plano,
                PlanoAlunoService.LimiteIaFocoPorDia,
                usadasHoje,
                PlanoAlunoService.LimiteIaFocoPorDia - usadasHoje,
                false,
                null,
                false,
                $"{usadasHoje} de {PlanoAlunoService.LimiteIaFocoPorDia} mensagens utilizadas hoje."),

            PlanoAluno.Mentoria => new TutorIaStatusResponse(
                TutorIaPrompts.Nome,
                true,
                plano,
                null,
                usadasHoje,
                null,
                false,
                null,
                false,
                "TutorIA disponivel no plano Mentoria, sem limite diario imposto pelo plano."),

            _ => throw new DomainException("Plano do aluno invalido.")
        };
    }

    private static TutorIaChatResponse Bloqueado(TutorIaStatusResponse status)
    {
        return new TutorIaChatResponse(
            TutorIaPrompts.Nome,
            false,
            status.Plano,
            status.Motivo,
            status.Mensagem,
            null,
            status.LimiteDiario,
            status.UsadasHoje,
            status.RestantesHoje,
            status.LimiteAtingido,
            status.PodeConhecerPlanos,
            status.Motivo == "LIMITE_DIARIO_ATINGIDO");
    }

    private void RegistrarUso(Guid alunoId)
    {
        var hoje = GetHojeBrasil();
        var uso = _db.UsosIaAluno.SingleOrDefault(u => u.AlunoId == alunoId && u.Data == hoje);
        if (uso is null)
        {
            uso = new UsoIaAluno(alunoId, hoje);
            _db.AddEntity(uso);
        }

        uso.RegistrarUso();
    }

    private PlanoAluno ObterPlanoAtual(Guid alunoId)
    {
        return _db.AssinaturasAluno
            .Where(a => a.AlunoId == alunoId && a.Ativa)
            .OrderByDescending(a => a.DataInicioUtc)
            .Select(a => a.Plano)
            .FirstOrDefault();
    }

    private void EnsureAlunoAtivo(Guid alunoId)
    {
        if (!_db.Usuarios.OfType<Aluno>().Any(a => a.Id == alunoId && a.Ativo))
        {
            throw new DomainException("Aluno nao encontrado ou inativo.");
        }
    }

    public static DateOnly GetHojeBrasil()
    {
        var timeZone = ResolveBrazilTimeZone();
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
    }

    private static TimeZoneInfo ResolveBrazilTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
    }
}
