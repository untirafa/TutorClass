namespace TutorClass.Mobile.Models;

public sealed class TutorIaResponse
{
    public string Nome { get; init; } = "TutorIA";
    public bool PossuiAcesso { get; init; }
    public string Plano { get; init; } = "";
    public int? LimiteDiario { get; init; }
    public int UsadasHoje { get; init; }
    public int? RestantesHoje { get; init; }
    public bool LimiteAtingido { get; init; }
    public string? Motivo { get; init; }
    public string Mensagem { get; init; } = "";
    public string? Resposta { get; init; }
}
public sealed record TutorIaChatRequest(string Mensagem);
public sealed record MensagemIaItem(string Autor, string Texto);
public sealed class NotificacaoDto
{
    public Guid Id { get; init; }
    public string Titulo { get; init; } = "";
    public string Mensagem { get; init; } = "";
    public bool Lida { get; init; }
    public DateTimeOffset DataCriacaoUtc { get; init; }
}
public sealed record NotificacaoResumoDto(int NaoLidas);
public sealed record MarcarTodasLidasDto(int QuantidadeAlterada);
