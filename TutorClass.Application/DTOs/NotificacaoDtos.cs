using TutorClass.Domain.Notifications;

namespace TutorClass.Application.DTOs;

public sealed record NotificacaoResponse(
    Guid Id,
    TipoNotificacao Tipo,
    string Titulo,
    string Mensagem,
    bool Lida,
    DateTime DataCriacaoUtc,
    DateTime? DataLeituraUtc,
    string? RotaAcao,
    Guid? ReferenciaId,
    PrioridadeNotificacao Prioridade);

public sealed record NotificacaoResumoResponse(int NaoLidas);

public sealed record MarcarTodasNotificacoesLidasResponse(int QuantidadeAlterada);
