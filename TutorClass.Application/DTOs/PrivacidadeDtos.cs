namespace TutorClass.Application.DTOs;

public sealed record DocumentoLegalResponse(string Tipo, string Versao, string Url);
public sealed record PrivacidadeResponse(
    IReadOnlyList<DocumentoLegalResponse> Documentos,
    string? ContatoPrivacidade,
    string? Orientacoes,
    bool AceiteObrigatorio = false);
