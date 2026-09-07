using TutorClass.Domain.Help;

namespace TutorClass.Application.DTOs;

public sealed record FaqResponse(
    Guid Id,
    string Pergunta,
    string Resposta,
    CategoriaFaq Categoria,
    string? PalavrasChave,
    bool Ativo,
    int OrdemExibicao,
    string? TextoAcao,
    string? RotaAcao,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CriarFaqRequest(
    string Pergunta,
    string Resposta,
    CategoriaFaq Categoria,
    string? PalavrasChave,
    int OrdemExibicao,
    string? TextoAcao = null,
    string? RotaAcao = null);

public sealed record AtualizarFaqRequest(
    string Pergunta,
    string Resposta,
    CategoriaFaq Categoria,
    string? PalavrasChave,
    int OrdemExibicao,
    string? TextoAcao,
    string? RotaAcao,
    bool Ativo);

public sealed record FaqChatRequest(string Mensagem);

public sealed record FaqChatResponse(
    bool Encontrado,
    string? Mensagem,
    Guid? Id,
    string? Pergunta,
    string? Resposta,
    CategoriaFaq? Categoria,
    string? TextoAcao,
    string? RotaAcao);
