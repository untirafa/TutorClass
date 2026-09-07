namespace TutorClass.Application.DTOs;

public sealed record CriarMaterialRequest(
    Guid AlunoId,
    Guid? AulaId,
    Guid? MateriaId,
    string Titulo,
    string? Descricao,
    string Tipo,
    string? NomeArquivo,
    string UrlArquivo);

public sealed record AtualizarMaterialRequest(
    string Titulo,
    string? Descricao,
    string Tipo,
    string? NomeArquivo,
    string UrlArquivo);

public sealed record MaterialResponse(
    Guid Id,
    Guid ProfessorId,
    string ProfessorNome,
    Guid AlunoId,
    string AlunoNome,
    Guid MateriaId,
    string MateriaNome,
    Guid? AulaId,
    string Titulo,
    string? Descricao,
    string Tipo,
    string? NomeArquivo,
    string UrlArquivo,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
