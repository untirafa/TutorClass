namespace TutorClass.Application.Abstractions;

public sealed class MensagemMentoriaBloqueadaException(string categoria) : Exception(
    "Mensagem não enviada. Reformule seu texto com respeito e sem ameaças para continuar a conversa.")
{
    public const string Codigo = "MENSAGEM_BLOQUEADA";
    public string Categoria { get; } = categoria;
}
