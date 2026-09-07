using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Notifications;

namespace TutorClass.Application.Services;

public sealed class NotificacaoService
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 50;

    private readonly IApplicationDbContext _db;

    public NotificacaoService(IApplicationDbContext db)
    {
        _db = db;
    }

    public Notificacao CriarSeNaoExistir(
        Guid usuarioId,
        TipoNotificacao tipo,
        string titulo,
        string mensagem,
        string rotaAcao,
        Guid? referenciaId,
        string chaveEvento,
        PrioridadeNotificacao prioridade = PrioridadeNotificacao.Normal)
    {
        EnsureUsuarioAtivo(usuarioId);

        var chave = NormalizeKey(chaveEvento);
        var existente = _db.Notificacoes.SingleOrDefault(n => n.ChaveEvento == chave);
        if (existente is not null)
        {
            return existente;
        }

        var notificacao = new Notificacao(usuarioId, tipo, titulo, mensagem, rotaAcao, referenciaId, chave, prioridade);
        _db.AddEntity(notificacao);
        return notificacao;
    }

    public async Task<NotificacaoResponse> CriarPersistidaAsync(
        Guid usuarioId,
        TipoNotificacao tipo,
        string titulo,
        string mensagem,
        string rotaAcao,
        Guid? referenciaId,
        string chaveEvento,
        CancellationToken cancellationToken,
        PrioridadeNotificacao prioridade = PrioridadeNotificacao.Normal)
    {
        var notificacao = CriarSeNaoExistir(usuarioId, tipo, titulo, mensagem, rotaAcao, referenciaId, chaveEvento, prioridade);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(notificacao);
    }

    public Task<IReadOnlyCollection<NotificacaoResponse>> ListarAsync(Guid usuarioId, bool somenteNaoLidas, int page, int pageSize, CancellationToken cancellationToken)
    {
        EnsureUsuarioAtivo(usuarioId);
        var pageNumber = page < DefaultPage ? DefaultPage : page;
        var size = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var query = _db.Notificacoes.Where(n => n.UsuarioId == usuarioId);
        if (somenteNaoLidas)
        {
            query = query.Where(n => !n.Lida);
        }

        var notificacoes = query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((pageNumber - 1) * size)
            .Take(size)
            .AsEnumerable()
            .Select(ToResponse)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<NotificacaoResponse>>(notificacoes);
    }

    public Task<NotificacaoResumoResponse> ObterResumoAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        EnsureUsuarioAtivo(usuarioId);
        return Task.FromResult(new NotificacaoResumoResponse(_db.Notificacoes.Count(n => n.UsuarioId == usuarioId && !n.Lida)));
    }

    public async Task<NotificacaoResponse> MarcarComoLidaAsync(Guid usuarioId, Guid notificacaoId, CancellationToken cancellationToken)
    {
        EnsureUsuarioAtivo(usuarioId);

        var notificacao = _db.Notificacoes.SingleOrDefault(n => n.Id == notificacaoId)
            ?? throw new DomainException("Notificacao nao encontrada.");

        if (notificacao.UsuarioId != usuarioId)
        {
            throw new UnauthorizedAccessException("Usuario nao pode alterar notificacao de outro usuario.");
        }

        notificacao.MarcarComoLida();
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(notificacao);
    }

    public async Task<MarcarTodasNotificacoesLidasResponse> MarcarTodasComoLidasAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        EnsureUsuarioAtivo(usuarioId);

        var notificacoes = _db.Notificacoes
            .Where(n => n.UsuarioId == usuarioId && !n.Lida)
            .ToArray();

        foreach (var notificacao in notificacoes)
        {
            notificacao.MarcarComoLida();
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new MarcarTodasNotificacoesLidasResponse(notificacoes.Length);
    }

    private void EnsureUsuarioAtivo(Guid usuarioId)
    {
        if (!_db.Usuarios.Any(u => u.Id == usuarioId && u.Ativo))
        {
            throw new DomainException("Usuario nao encontrado ou inativo.");
        }
    }

    private static string NormalizeKey(string chaveEvento)
    {
        if (string.IsNullOrWhiteSpace(chaveEvento))
        {
            throw new DomainException("Chave do evento da notificacao e obrigatoria.");
        }

        return chaveEvento.Trim().ToUpperInvariant();
    }

    private static NotificacaoResponse ToResponse(Notificacao notificacao) => new(
        notificacao.Id,
        notificacao.Tipo,
        notificacao.Titulo,
        notificacao.Mensagem,
        notificacao.Lida,
        notificacao.CreatedAtUtc,
        notificacao.DataLeituraUtc,
        notificacao.RotaAcao,
        notificacao.ReferenciaId,
        notificacao.Prioridade);
}
