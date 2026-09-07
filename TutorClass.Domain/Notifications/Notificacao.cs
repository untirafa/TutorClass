using TutorClass.Domain.Common;

namespace TutorClass.Domain.Notifications;

public sealed class Notificacao : Entity
{
    private Notificacao()
    {
    }

    public Notificacao(
        Guid usuarioId,
        TipoNotificacao tipo,
        string titulo,
        string mensagem,
        string? rotaAcao = null,
        Guid? referenciaId = null,
        string? chaveEvento = null,
        PrioridadeNotificacao prioridade = PrioridadeNotificacao.Normal)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new DomainException("Usuario da notificacao e obrigatorio.");
        }

        UsuarioId = usuarioId;
        Tipo = tipo;
        Prioridade = prioridade;
        ReferenciaId = referenciaId;
        ChaveEvento = NormalizeOptional(chaveEvento, "Chave do evento", 160);
        DefinirConteudo(titulo, mensagem, rotaAcao);
    }

    public Guid UsuarioId { get; private set; }
    public TipoNotificacao Tipo { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string Mensagem { get; private set; } = string.Empty;
    public bool Lida { get; private set; }
    public DateTime? DataLeituraUtc { get; private set; }
    public string? RotaAcao { get; private set; }
    public Guid? ReferenciaId { get; private set; }
    public string? ChaveEvento { get; private set; }
    public PrioridadeNotificacao Prioridade { get; private set; } = PrioridadeNotificacao.Normal;

    public void MarcarComoLida()
    {
        if (Lida)
        {
            return;
        }

        Lida = true;
        DataLeituraUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    private void DefinirConteudo(string titulo, string mensagem, string? rotaAcao)
    {
        Titulo = NormalizeRequired(titulo, "Titulo", 140);
        Mensagem = NormalizeRequired(mensagem, "Mensagem", 500);
        RotaAcao = NormalizeOptional(rotaAcao, "Rota de acao", 240);
    }

    private static string NormalizeRequired(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} da notificacao e obrigatorio.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} da notificacao deve possuir no maximo {maxLength} caracteres.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} da notificacao deve possuir no maximo {maxLength} caracteres.");
        }

        return normalized;
    }
}
