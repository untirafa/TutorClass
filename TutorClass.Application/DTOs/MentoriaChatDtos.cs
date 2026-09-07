namespace TutorClass.Application.DTOs;

public sealed record MentoriaChatResponse(
    string Id,
    Guid VinculoMentoriaId,
    Guid AlunoId,
    Guid ProfessorId,
    bool SomenteLeitura,
    DateTime DataCriacaoUtc);

public sealed record EnviarMensagemMentoriaRequest(string Conteudo);

public sealed record MensagemMentoriaResponse(
    string Id,
    string ChatMentoriaId,
    Guid VinculoMentoriaId,
    Guid RemetenteUsuarioId,
    string RemetenteNome,
    string Conteudo,
    DateTime DataEnvioUtc,
    bool Lida,
    DateTime? DataLeituraUtc);

public sealed record MensagensMentoriaPageResponse(
    IReadOnlyCollection<MensagemMentoriaResponse> Mensagens,
    DateTime? ProximoBeforeUtc);

public sealed record MensagensLidasResponse(int QuantidadeMarcada);
