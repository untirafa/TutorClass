using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Notifications;
using TutorClass.Domain.Users;

namespace TutorClass.Application.Services;

// Fluxo de vinculo: o responsavel informa o nome completo do aluno, a administracao analisa e,
// ao aprovar, escolhe o aluno correto e cria o vinculo (ResponsavelAluno).
public sealed class VinculoResponsavelService
{
    private const int MaxPendentesPorResponsavel = 5;
    private const int MaxCandidatos = 10;

    private readonly IApplicationDbContext _db;
    private readonly NotificacaoService _notificacoes;

    public VinculoResponsavelService(IApplicationDbContext db, NotificacaoService notificacoes)
    {
        _db = db;
        _notificacoes = notificacoes;
    }

    public async Task<SolicitacaoVinculoResponse> SolicitarAsync(Guid responsavelId, SolicitarVinculoRequest request, CancellationToken cancellationToken)
    {
        var responsavel = _db.Usuarios.OfType<Responsavel>().SingleOrDefault(r => r.Id == responsavelId && r.Ativo)
            ?? throw new UnauthorizedAccessException("Esta operacao e permitida somente para o perfil Responsavel.");

        var solicitacao = new SolicitacaoVinculoResponsavel(responsavelId, request.NomeAluno, request.Parentesco);
        var chave = Normalizar(solicitacao.NomeAlunoInformado);

        var pendentes = _db.SolicitacoesVinculoResponsavel
            .Where(s => s.ResponsavelId == responsavelId && s.Status == StatusSolicitacaoVinculo.Pendente)
            .ToArray();
        if (pendentes.Any(s => Normalizar(s.NomeAlunoInformado) == chave))
        {
            throw new DomainException("Voce ja possui uma solicitacao pendente para este aluno.");
        }

        if (pendentes.Length >= MaxPendentesPorResponsavel)
        {
            throw new DomainException($"Voce ja possui {MaxPendentesPorResponsavel} solicitacoes pendentes. Aguarde a analise da administracao.");
        }

        _db.AddEntity(solicitacao);

        foreach (var admin in _db.Administradores.Where(a => a.Ativo).ToArray())
        {
            _notificacoes.CriarSeNaoExistir(
                admin.Id,
                TipoNotificacao.NovaSolicitacaoVinculo,
                "Nova solicitacao de vinculo",
                $"{responsavel.Nome} pediu vinculo com o aluno \"{solicitacao.NomeAlunoInformado}\" e aguarda analise.",
                "#vinculos",
                solicitacao.Id,
                $"VINCULO:NOVA:{solicitacao.Id}:{admin.Id}",
                PrioridadeNotificacao.Importante);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(solicitacao);
    }

    public Task<IReadOnlyCollection<SolicitacaoVinculoResponse>> ListarMinhasAsync(Guid responsavelId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_db.Usuarios.OfType<Responsavel>().Any(r => r.Id == responsavelId && r.Ativo))
        {
            throw new UnauthorizedAccessException("Esta operacao e permitida somente para o perfil Responsavel.");
        }

        var itens = _db.SolicitacoesVinculoResponsavel
            .Where(s => s.ResponsavelId == responsavelId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Take(50)
            .ToArray()
            .Select(ToResponse)
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<SolicitacaoVinculoResponse>>(itens);
    }

    public Task<IReadOnlyCollection<SolicitacaoVinculoAdminResponse>> ListarAdminAsync(Guid administradorId, StatusSolicitacaoVinculo? status, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureAdministrador(administradorId);

        var query = _db.SolicitacoesVinculoResponsavel.AsQueryable();
        if (status is not null)
        {
            query = query.Where(s => s.Status == status);
        }

        var solicitacoes = query.OrderByDescending(s => s.CreatedAtUtc).Take(200).ToArray();
        var responsaveisIds = solicitacoes.Select(s => s.ResponsavelId).Distinct().ToArray();
        var responsaveis = _db.Usuarios.OfType<Responsavel>().Where(r => responsaveisIds.Contains(r.Id)).ToArray()
            .ToDictionary(r => r.Id);
        var alunos = _db.Usuarios.OfType<Aluno>().Where(a => a.Ativo).ToArray();
        var nomesAlunos = alunos.ToDictionary(a => a.Id, a => a.Nome);

        var itens = solicitacoes.Select(s =>
        {
            responsaveis.TryGetValue(s.ResponsavelId, out var responsavel);
            IReadOnlyCollection<CandidatoVinculoResponse> candidatos = s.Pendente
                ? Candidatos(alunos, s.NomeAlunoInformado)
                : Array.Empty<CandidatoVinculoResponse>();
            string? alunoNome = s.AlunoId is { } id && nomesAlunos.TryGetValue(id, out var nome) ? nome : null;
            return new SolicitacaoVinculoAdminResponse(
                s.Id,
                s.ResponsavelId,
                responsavel?.Nome ?? "Responsavel",
                responsavel?.Email ?? string.Empty,
                s.NomeAlunoInformado,
                s.Parentesco,
                s.Status,
                s.Motivo,
                s.AlunoId,
                alunoNome,
                s.CreatedAtUtc,
                s.AnalisadaEmUtc,
                candidatos);
        }).ToArray();

        return Task.FromResult<IReadOnlyCollection<SolicitacaoVinculoAdminResponse>>(itens);
    }

    public async Task AprovarAsync(Guid solicitacaoId, Guid administradorId, AprovarVinculoRequest request, CancellationToken cancellationToken)
    {
        EnsureAdministrador(administradorId);
        var solicitacao = await _db.FindAsync<SolicitacaoVinculoResponsavel>(solicitacaoId, cancellationToken)
            ?? throw new DomainException("Solicitacao nao encontrada.");

        var responsavel = _db.Usuarios.OfType<Responsavel>().SingleOrDefault(r => r.Id == solicitacao.ResponsavelId && r.Ativo)
            ?? throw new DomainException("Responsavel nao encontrado ou inativo.");
        var aluno = _db.Usuarios.OfType<Aluno>().SingleOrDefault(a => a.Id == request.AlunoId && a.Ativo)
            ?? throw new DomainException("Aluno nao encontrado ou inativo.");

        if (_db.ResponsaveisAlunos.Any(ra => ra.ResponsavelId == responsavel.Id && ra.AlunoId == aluno.Id))
        {
            throw new DomainException("Este responsavel ja esta vinculado a este aluno.");
        }

        solicitacao.Aprovar(administradorId, aluno.Id);
        _db.AddEntity(new ResponsavelAluno(responsavel.Id, aluno.Id, solicitacao.Parentesco));

        _notificacoes.CriarSeNaoExistir(
            responsavel.Id,
            TipoNotificacao.VinculoResponsavelAtualizado,
            "Vinculo aprovado",
            $"Sua solicitacao foi aprovada. Agora voce acompanha {aluno.Nome}.",
            "#vinculados",
            solicitacao.Id,
            $"VINCULO:APROVADO:{solicitacao.Id}:{responsavel.Id}",
            PrioridadeNotificacao.Importante);
        _notificacoes.CriarSeNaoExistir(
            aluno.Id,
            TipoNotificacao.VinculoResponsavelAtualizado,
            "Responsavel vinculado",
            $"{responsavel.Nome} agora acompanha suas aulas e pagamentos como responsavel.",
            "#inicio",
            solicitacao.Id,
            $"VINCULO:ALUNO:{solicitacao.Id}:{aluno.Id}");

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecusarAsync(Guid solicitacaoId, Guid administradorId, RecusarVinculoRequest request, CancellationToken cancellationToken)
    {
        EnsureAdministrador(administradorId);
        var solicitacao = await _db.FindAsync<SolicitacaoVinculoResponsavel>(solicitacaoId, cancellationToken)
            ?? throw new DomainException("Solicitacao nao encontrada.");

        solicitacao.Recusar(administradorId, request.Motivo);

        if (_db.Usuarios.Any(u => u.Id == solicitacao.ResponsavelId && u.Ativo))
        {
            _notificacoes.CriarSeNaoExistir(
                solicitacao.ResponsavelId,
                TipoNotificacao.VinculoResponsavelAtualizado,
                "Vinculo nao aprovado",
                $"Sua solicitacao de vinculo com \"{solicitacao.NomeAlunoInformado}\" nao foi aprovada. Motivo: {solicitacao.Motivo}",
                "#vinculados",
                solicitacao.Id,
                $"VINCULO:RECUSADO:{solicitacao.Id}:{solicitacao.ResponsavelId}",
                PrioridadeNotificacao.Importante);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private void EnsureAdministrador(Guid administradorId)
    {
        if (!_db.Administradores.Any(a => a.Id == administradorId && a.Ativo))
        {
            throw new DomainException("Administrador nao encontrado ou inativo.");
        }
    }

    // Alunos ativos cujo nome corresponde ao informado (ignora caixa e acentos): iguais primeiro, depois os que contem todas as palavras.
    private static IReadOnlyCollection<CandidatoVinculoResponse> Candidatos(IEnumerable<Aluno> alunos, string nomeInformado)
    {
        var alvo = Normalizar(nomeInformado);
        var palavras = alvo.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return alunos
            .Select(a => (Aluno: a, Nome: Normalizar(a.Nome)))
            .Where(x => x.Nome == alvo || palavras.All(p => x.Nome.Contains(p, StringComparison.Ordinal)))
            .OrderByDescending(x => x.Nome == alvo)
            .ThenBy(x => x.Aluno.Nome, StringComparer.OrdinalIgnoreCase)
            .Take(MaxCandidatos)
            .Select(x => new CandidatoVinculoResponse(x.Aluno.Id, x.Aluno.Nome, x.Aluno.Email, x.Aluno.DataNascimento))
            .ToArray();
    }

    private static string Normalizar(string valor)
    {
        const string comAcento = "\u00C1\u00C0\u00C2\u00C3\u00C4\u00C9\u00C8\u00CA\u00CB\u00CD\u00CC\u00CE\u00CF\u00D3\u00D2\u00D4\u00D5\u00D6\u00DA\u00D9\u00DB\u00DC\u00C7\u00D1";
        const string semAcento = "AAAAAEEEEIIIIOOOOOUUUUCN";
        var chars = valor.Trim().ToUpperInvariant().ToCharArray();
        for (var k = 0; k < chars.Length; k++)
        {
            var i = comAcento.IndexOf(chars[k]);
            if (i >= 0)
            {
                chars[k] = semAcento[i];
            }
        }

        return string.Join(' ', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static SolicitacaoVinculoResponse ToResponse(SolicitacaoVinculoResponsavel s) => new(
        s.Id,
        s.NomeAlunoInformado,
        s.Parentesco,
        s.Status,
        s.Motivo,
        s.CreatedAtUtc,
        s.AnalisadaEmUtc);
}
