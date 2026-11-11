using TutorClass.Domain.Moderation;

namespace TutorClass.Application.DTOs;

/// <summary>
/// Denuncia de um perfil de professor (AlvoId = id do professor), de uma avaliacao (AlvoId = id da avaliacao)
/// ou de uma mensagem da Mentoria (AlvoId = id da mensagem; VinculoMentoriaId obrigatorio).
/// </summary>
public sealed record CriarDenunciaRequest(
    TipoAlvoDenuncia Tipo,
    string AlvoId,
    MotivoDenuncia Motivo,
    string? Descricao,
    Guid? VinculoMentoriaId = null);

/// <summary>Visao de quem denunciou: acompanha o andamento sem expor dados internos da moderacao.</summary>
public sealed record DenunciaResponse(
    Guid Id,
    TipoAlvoDenuncia Tipo,
    MotivoDenuncia Motivo,
    string? Descricao,
    string Resumo,
    StatusDenuncia Status,
    DateTime CriadaEmUtc,
    DateTime? AnalisadaEmUtc);

public sealed record DenunciaAdminResponse(
    Guid Id,
    TipoAlvoDenuncia Tipo,
    string AlvoId,
    MotivoDenuncia Motivo,
    string? Descricao,
    string Resumo,
    StatusDenuncia Status,
    SancaoDenuncia Sancao,
    string? Observacao,
    Guid DenuncianteId,
    string Denunciante,
    Guid AlvoUsuarioId,
    string AlvoUsuario,
    string AlvoPerfil,
    bool AlvoAtivo,
    int DenunciasContraAlvo,
    DateTime CriadaEmUtc,
    DateTime? AnalisadaEmUtc,
    DateTime? ContaReativadaEmUtc);

public sealed record DecidirDenunciaRequest(bool Procedente, SancaoDenuncia Sancao, string Observacao);

public sealed record DenunciasResumoResponse(int Abertas, int EmAnalise, int Procedentes, int Improcedentes);
