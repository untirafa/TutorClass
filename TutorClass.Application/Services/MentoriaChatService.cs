using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Mentoring;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;

namespace TutorClass.Application.Services;

public sealed class MentoriaChatService
{
    private const int DefaultLimit = 30;
    private const int MaxLimit = 100;

    private readonly IApplicationDbContext _db;
    private readonly IMentoriaChatStore _store;
    private readonly ResponsavelAlunoService _responsaveis;

    public MentoriaChatService(IApplicationDbContext db, IMentoriaChatStore store)
        : this(db, store, new ResponsavelAlunoService(db))
    {
    }

    public MentoriaChatService(IApplicationDbContext db, IMentoriaChatStore store, ResponsavelAlunoService responsaveis)
    {
        _db = db;
        _store = store;
        _responsaveis = responsaveis;
    }

    public async Task<MentoriaChatResponse> ObterChatAsync(Guid vinculoMentoriaId, Guid usuarioId, CancellationToken cancellationToken)
    {
        var vinculo = GetVinculo(vinculoMentoriaId);
        EnsureParticipante(vinculo, usuarioId);

        if (vinculo.Ativo)
        {
            EnsureMentoriaAtivaParaEnvio(vinculo);
        }

        var chat = await _store.ObterOuCriarChatAsync(vinculo, cancellationToken);
        return chat with { SomenteLeitura = !vinculo.Ativo };
    }

    public async Task<MensagemMentoriaResponse> EnviarMensagemAsync(Guid vinculoMentoriaId, Guid remetenteUsuarioId, EnviarMensagemMentoriaRequest request, CancellationToken cancellationToken)
    {
        var vinculo = GetVinculo(vinculoMentoriaId);
        EnsureParticipante(vinculo, remetenteUsuarioId);
        EnsureMentoriaAtivaParaEnvio(vinculo);

        if (string.IsNullOrWhiteSpace(request.Conteudo))
        {
            throw new DomainException("Mensagem e obrigatoria.");
        }

        if (request.Conteudo.Length > 2000)
        {
            throw new DomainException("Mensagem deve possuir no maximo 2000 caracteres.");
        }

        var chat = await _store.ObterOuCriarChatAsync(vinculo, cancellationToken);
        return await _store.SalvarMensagemAsync(chat, remetenteUsuarioId, GetUsuarioNome(remetenteUsuarioId), request.Conteudo.Trim(), cancellationToken);
    }

    public async Task<MensagensMentoriaPageResponse> ListarMensagensAsync(Guid vinculoMentoriaId, Guid usuarioId, int? limit, DateTime? beforeUtc, CancellationToken cancellationToken)
    {
        var vinculo = GetVinculo(vinculoMentoriaId);
        EnsureParticipante(vinculo, usuarioId);

        var take = Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);
        var mensagens = await _store.ListarMensagensAsync(vinculoMentoriaId, take, beforeUtc, cancellationToken);
        DateTime? proximo = mensagens.Count == take ? mensagens.Min(m => m.DataEnvioUtc) : null;
        return new MensagensMentoriaPageResponse(mensagens, proximo);
    }

    public Task<MensagensMentoriaPageResponse> ListarMensagensParaResponsavelAsync(
        Guid vinculoMentoriaId,
        Guid responsavelId,
        int? limit,
        DateTime? beforeUtc,
        CancellationToken cancellationToken)
    {
        throw new UnauthorizedAccessException("Responsáveis não têm acesso às mensagens privadas da Mentoria. Consulte os resumos de acompanhamento autorizados.");
    }

    public async Task<MensagensLidasResponse> MarcarComoLidasAsync(Guid vinculoMentoriaId, Guid usuarioId, CancellationToken cancellationToken)
    {
        var vinculo = GetVinculo(vinculoMentoriaId);
        EnsureParticipante(vinculo, usuarioId);
        var count = await _store.MarcarComoLidasAsync(vinculoMentoriaId, usuarioId, cancellationToken);
        return new MensagensLidasResponse(count);
    }

    private MentoriaAlunoProfessor GetVinculo(Guid vinculoMentoriaId)
    {
        return _db.MentoriasAlunoProfessor.SingleOrDefault(m => m.Id == vinculoMentoriaId)
            ?? throw new DomainException("Vinculo de Mentoria nao encontrado.");
    }

    private void EnsureParticipante(MentoriaAlunoProfessor vinculo, Guid usuarioId)
    {
        if (usuarioId == vinculo.AlunoId || usuarioId == vinculo.ProfessorId)
        {
            return;
        }

        throw new UnauthorizedAccessException("Usuario nao participa deste chat de Mentoria.");
    }

    private void EnsureMentoriaAtivaParaEnvio(MentoriaAlunoProfessor vinculo)
    {
        if (!vinculo.Ativo)
        {
            throw new DomainException("Esta Mentoria foi encerrada. O historico da conversa continua disponivel, mas novas mensagens nao podem ser enviadas.");
        }

        var alunoAtivo = _db.Usuarios.OfType<Aluno>().Any(a => a.Id == vinculo.AlunoId && a.Ativo);
        var professor = _db.Professores.SingleOrDefault(p => p.Id == vinculo.ProfessorId && p.Ativo);
        var planoMentoriaAtivo = _db.AssinaturasAluno.Any(a => a.AlunoId == vinculo.AlunoId && a.Ativa && a.Plano == PlanoAluno.Mentoria);

        if (!alunoAtivo || professor is null || professor.Status != StatusProfessor.Aprovado || !professor.OfereceMentoria || !planoMentoriaAtivo)
        {
            throw new DomainException("Chat disponivel somente para Mentoria ativa entre aluno e professor mentor.");
        }
    }

    private string GetUsuarioNome(Guid usuarioId)
    {
        return _db.Usuarios.Where(u => u.Id == usuarioId).Select(u => u.Nome).SingleOrDefault() ?? "Usuario";
    }
}
