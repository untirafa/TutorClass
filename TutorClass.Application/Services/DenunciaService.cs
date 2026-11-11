using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Moderation;
using TutorClass.Domain.Notifications;
using TutorClass.Domain.Users;

namespace TutorClass.Application.Services;

/// <summary>
/// Denuncias de conteudo e usuarios: registro pelas pessoas usuarias, fila e decisao pela administracao,
/// com sancoes (advertencia ou suspensao da conta) e notificacoes internas.
/// </summary>
public sealed class DenunciaService
{
    private const int MaxDenunciasPorDia = 10;
    private const int DefaultPageSize = 30;
    private const int MaxPageSize = 100;
    private const int MensagensConsultadas = 100;

    private readonly IApplicationDbContext _db;
    private readonly IMentoriaChatStore _store;
    private readonly NotificacaoService _notificacoes;

    public DenunciaService(IApplicationDbContext db, IMentoriaChatStore store, NotificacaoService notificacoes)
    {
        _db = db;
        _store = store;
        _notificacoes = notificacoes;
    }

    public async Task<DenunciaResponse> CriarAsync(Guid denuncianteId, CriarDenunciaRequest request, CancellationToken cancellationToken)
    {
        if (!_db.Usuarios.Any(u => u.Id == denuncianteId && u.Ativo))
        {
            throw new DomainException("Usuario nao encontrado ou inativo.");
        }

        if (string.IsNullOrWhiteSpace(request.AlvoId))
        {
            throw new DomainException("Conteudo denunciado e obrigatorio.");
        }

        var alvoId = request.AlvoId.Trim();
        var desde = DateTime.UtcNow.AddHours(-24);
        if (_db.Denuncias.Count(d => d.DenuncianteId == denuncianteId && d.CreatedAtUtc >= desde) >= MaxDenunciasPorDia)
        {
            throw new DomainException("Limite diario de denuncias atingido. Tente novamente amanha.");
        }

        var alvo = await ResolverAlvoAsync(denuncianteId, request, alvoId, cancellationToken);

        var chave = alvo.AlvoId;
        var jaAberta = _db.Denuncias.Any(d =>
            d.DenuncianteId == denuncianteId &&
            d.TipoAlvo == request.Tipo &&
            d.AlvoId == chave &&
            (d.Status == StatusDenuncia.Aberta || d.Status == StatusDenuncia.EmAnalise));
        if (jaAberta)
        {
            throw new DomainException("Voce ja denunciou este conteudo e a denuncia esta em analise.");
        }

        var denuncia = new Denuncia(
            denuncianteId,
            alvo.UsuarioId,
            request.Tipo,
            alvo.AlvoId,
            request.Motivo,
            request.Descricao,
            alvo.Resumo,
            alvo.VinculoMentoriaId);
        _db.AddEntity(denuncia);

        foreach (var admin in _db.Administradores.Where(a => a.Ativo).ToArray())
        {
            _notificacoes.CriarSeNaoExistir(
                admin.Id,
                TipoNotificacao.NovaDenuncia,
                "Nova denuncia recebida",
                $"Uma denuncia ({DescricaoTipo(request.Tipo)}) aguarda analise na fila de moderacao.",
                "#moderacao",
                denuncia.Id,
                $"DENUNCIA:NOVA:{denuncia.Id}:{admin.Id}",
                PrioridadeNotificacao.Importante);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(denuncia);
    }

    public Task<IReadOnlyCollection<DenunciaResponse>> ListarMinhasAsync(Guid denuncianteId, CancellationToken cancellationToken)
    {
        var lista = _db.Denuncias
            .Where(d => d.DenuncianteId == denuncianteId)
            .OrderByDescending(d => d.CreatedAtUtc)
            .Take(50)
            .ToArray()
            .Select(ToResponse)
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<DenunciaResponse>>(lista);
    }

    public Task<IReadOnlyCollection<DenunciaAdminResponse>> ListarAdminAsync(StatusDenuncia? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var pageNumber = page < 1 ? 1 : page;
        var size = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var query = _db.Denuncias.AsQueryable();
        if (status is not null)
        {
            query = query.Where(d => d.Status == status);
        }

        // Fila: abertas primeiro, depois em analise (as mais antigas na frente, para nao deixar ninguem esperando);
        // as decididas vem por ultimo, das mais recentes para as mais antigas.
        var itens = query.ToArray()
            .OrderBy(d => d.Status switch
            {
                StatusDenuncia.Aberta => 0,
                StatusDenuncia.EmAnalise => 1,
                _ => 2
            })
            .ThenBy(d => d.Encerrada ? -d.CreatedAtUtc.Ticks : d.CreatedAtUtc.Ticks)
            .Skip((pageNumber - 1) * size)
            .Take(size)
            .Select(ToAdminResponse)
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<DenunciaAdminResponse>>(itens);
    }

    public Task<DenunciasResumoResponse> ObterResumoAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new DenunciasResumoResponse(
            _db.Denuncias.Count(d => d.Status == StatusDenuncia.Aberta),
            _db.Denuncias.Count(d => d.Status == StatusDenuncia.EmAnalise),
            _db.Denuncias.Count(d => d.Status == StatusDenuncia.Procedente),
            _db.Denuncias.Count(d => d.Status == StatusDenuncia.Improcedente)));
    }

    public async Task<DenunciaAdminResponse> IniciarAnaliseAsync(Guid denunciaId, Guid administradorId, CancellationToken cancellationToken)
    {
        var denuncia = Obter(denunciaId);
        denuncia.IniciarAnalise(administradorId);
        await _db.SaveChangesAsync(cancellationToken);
        return ToAdminResponse(denuncia);
    }

    public async Task<DenunciaAdminResponse> DecidirAsync(Guid denunciaId, Guid administradorId, DecidirDenunciaRequest request, CancellationToken cancellationToken)
    {
        var denuncia = Obter(denunciaId);
        var alvo = _db.Usuarios.SingleOrDefault(u => u.Id == denuncia.AlvoUsuarioId);

        if (request.Procedente && request.Sancao != SancaoDenuncia.Nenhuma)
        {
            if (alvo is null) throw new DomainException("Usuario denunciado nao encontrado.");
            if (alvo is Administrador) throw new DomainException("Nao e possivel aplicar sancao a uma conta administradora.");
            if (alvo.Id == administradorId) throw new DomainException("Voce nao pode aplicar sancao a si mesmo.");
        }

        denuncia.Decidir(administradorId, request.Procedente, request.Sancao, request.Observacao);

        // Quem denunciou recebe o retorno da analise (sem expor a justificativa interna).
        if (_db.Usuarios.Any(u => u.Id == denuncia.DenuncianteId && u.Ativo))
        {
            _notificacoes.CriarSeNaoExistir(
                denuncia.DenuncianteId,
                TipoNotificacao.DenunciaAtualizada,
                "Sua denuncia foi analisada",
                denuncia.Status == StatusDenuncia.Procedente
                    ? "A equipe analisou sua denuncia e tomou providencias. Obrigado por ajudar a manter o TutorClass respeitoso."
                    : "A equipe analisou sua denuncia e nao identificou violacao das regras de convivencia. Obrigado por avisar.",
                "#minhas-denuncias",
                denuncia.Id,
                $"DENUNCIA:DECIDIDA:{denuncia.Id}");
        }

        if (alvo is not null && denuncia.Status == StatusDenuncia.Procedente)
        {
            if (denuncia.Sancao == SancaoDenuncia.Advertencia && alvo.Ativo)
            {
                _notificacoes.CriarSeNaoExistir(
                    alvo.Id,
                    TipoNotificacao.AdvertenciaModeracao,
                    "Aviso da moderacao",
                    "Recebemos uma denuncia sobre seu conteudo e ela foi considerada procedente. Releia as Regras de convivencia: novas ocorrencias podem levar a suspensao da conta.",
                    "#regras-de-convivencia",
                    denuncia.Id,
                    $"DENUNCIA:ADVERTENCIA:{denuncia.Id}",
                    PrioridadeNotificacao.Importante);
            }
            else if (denuncia.Sancao == SancaoDenuncia.Suspensao)
            {
                alvo.Desativar();
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ToAdminResponse(denuncia);
    }

    public async Task<DenunciaAdminResponse> ReativarContaAsync(Guid denunciaId, Guid administradorId, CancellationToken cancellationToken)
    {
        var denuncia = Obter(denunciaId);
        var alvo = _db.Usuarios.SingleOrDefault(u => u.Id == denuncia.AlvoUsuarioId)
            ?? throw new DomainException("Usuario denunciado nao encontrado.");

        denuncia.RegistrarReativacaoDaConta();
        alvo.Reativar();
        await _db.SaveChangesAsync(cancellationToken);
        return ToAdminResponse(denuncia);
    }

    private Denuncia Obter(Guid denunciaId)
    {
        return _db.Denuncias.SingleOrDefault(d => d.Id == denunciaId)
            ?? throw new DomainException("Denuncia nao encontrada.");
    }

    private async Task<AlvoResolvido> ResolverAlvoAsync(Guid denuncianteId, CriarDenunciaRequest request, string alvoId, CancellationToken cancellationToken)
    {
        switch (request.Tipo)
        {
            case TipoAlvoDenuncia.Professor:
            {
                var id = ParseGuid(alvoId, "Professor");
                var professor = _db.Professores.SingleOrDefault(p => p.Id == id)
                    ?? throw new DomainException("Professor nao encontrado.");
                if (professor.Id == denuncianteId) throw new DomainException("Voce nao pode denunciar a si mesmo.");
                return new AlvoResolvido(professor.Id, id.ToString(), $"Perfil do professor {professor.Nome}", null);
            }
            case TipoAlvoDenuncia.Avaliacao:
            {
                var id = ParseGuid(alvoId, "Avaliacao");
                var avaliacao = _db.AvaliacoesProfessor.SingleOrDefault(a => a.Id == id)
                    ?? throw new DomainException("Avaliacao nao encontrada.");
                if (avaliacao.AlunoId == denuncianteId) throw new DomainException("Voce nao pode denunciar a propria avaliacao.");
                var texto = string.IsNullOrWhiteSpace(avaliacao.Comentario) ? "(sem comentario)" : avaliacao.Comentario;
                return new AlvoResolvido(avaliacao.AlunoId, id.ToString(), $"Avaliacao {avaliacao.Nota}/5: {texto}", null);
            }
            case TipoAlvoDenuncia.Mensagem:
            {
                if (request.VinculoMentoriaId is null || request.VinculoMentoriaId == Guid.Empty)
                {
                    throw new DomainException("Mentoria da mensagem e obrigatoria.");
                }

                var vinculo = _db.MentoriasAlunoProfessor.SingleOrDefault(m => m.Id == request.VinculoMentoriaId)
                    ?? throw new DomainException("Vinculo de Mentoria nao encontrado.");
                if (denuncianteId != vinculo.AlunoId && denuncianteId != vinculo.ProfessorId)
                {
                    throw new UnauthorizedAccessException("Usuario nao participa deste chat de Mentoria.");
                }

                var mensagens = await _store.ListarMensagensAsync(vinculo.Id, MensagensConsultadas, null, cancellationToken);
                var mensagem = mensagens.FirstOrDefault(m => string.Equals(m.Id, alvoId, StringComparison.Ordinal))
                    ?? throw new DomainException("Mensagem nao encontrada entre as mais recentes da conversa.");
                if (mensagem.RemetenteUsuarioId == denuncianteId) throw new DomainException("Voce nao pode denunciar a propria mensagem.");
                return new AlvoResolvido(mensagem.RemetenteUsuarioId, mensagem.Id, $"Mensagem na Mentoria: {mensagem.Conteudo}", vinculo.Id);
            }
            default:
                throw new DomainException("Tipo de denuncia invalido.");
        }
    }

    private static Guid ParseGuid(string value, string nome)
    {
        return Guid.TryParse(value, out var id) && id != Guid.Empty
            ? id
            : throw new DomainException($"{nome} invalido.");
    }

    private static string DescricaoTipo(TipoAlvoDenuncia tipo) => tipo switch
    {
        TipoAlvoDenuncia.Professor => "perfil de professor",
        TipoAlvoDenuncia.Avaliacao => "avaliacao",
        _ => "mensagem da Mentoria"
    };

    private DenunciaAdminResponse ToAdminResponse(Denuncia d)
    {
        var denunciante = _db.Usuarios.Where(u => u.Id == d.DenuncianteId).Select(u => u.Nome).SingleOrDefault() ?? "Usuario";
        var alvo = _db.Usuarios.SingleOrDefault(u => u.Id == d.AlvoUsuarioId);
        var total = _db.Denuncias.Count(x => x.AlvoUsuarioId == d.AlvoUsuarioId);
        return new DenunciaAdminResponse(
            d.Id,
            d.TipoAlvo,
            d.AlvoId,
            d.Motivo,
            d.Descricao,
            d.ResumoAlvo,
            d.Status,
            d.Sancao,
            d.ObservacaoModeracao,
            d.DenuncianteId,
            denunciante,
            d.AlvoUsuarioId,
            alvo?.Nome ?? "Usuario",
            alvo is null ? "Usuario" : alvo.GetType().Name,
            alvo?.Ativo ?? false,
            total,
            d.CreatedAtUtc,
            d.AnalisadaEmUtc,
            d.ContaReativadaEmUtc);
    }

    private static DenunciaResponse ToResponse(Denuncia d) => new(
        d.Id,
        d.TipoAlvo,
        d.Motivo,
        d.Descricao,
        d.ResumoAlvo,
        d.Status,
        d.CreatedAtUtc,
        d.AnalisadaEmUtc);

    private sealed record AlvoResolvido(Guid UsuarioId, string AlvoId, string Resumo, Guid? VinculoMentoriaId);
}
