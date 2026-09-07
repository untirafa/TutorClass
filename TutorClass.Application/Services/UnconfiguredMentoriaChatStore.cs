using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Mentoring;

namespace TutorClass.Application.Services;

public sealed class UnconfiguredMentoriaChatStore : IMentoriaChatStore
{
    public bool Configurado => false;

    public Task<MentoriaChatResponse> ObterOuCriarChatAsync(MentoriaAlunoProfessor vinculo, CancellationToken cancellationToken)
    {
        throw BuildException();
    }

    public Task<MensagemMentoriaResponse> SalvarMensagemAsync(MentoriaChatResponse chat, Guid remetenteUsuarioId, string remetenteNome, string conteudo, CancellationToken cancellationToken)
    {
        throw BuildException();
    }

    public Task<IReadOnlyCollection<MensagemMentoriaResponse>> ListarMensagensAsync(Guid vinculoMentoriaId, int limit, DateTime? beforeUtc, CancellationToken cancellationToken)
    {
        throw BuildException();
    }

    public Task<int> MarcarComoLidasAsync(Guid vinculoMentoriaId, Guid leitorUsuarioId, CancellationToken cancellationToken)
    {
        throw BuildException();
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    private static DomainException BuildException()
    {
        return new DomainException("Chat de Mentoria indisponivel: configure MongoDb:ConnectionString e MongoDb:DatabaseName.");
    }
}
