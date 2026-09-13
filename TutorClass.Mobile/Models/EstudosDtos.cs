namespace TutorClass.Mobile.Models;

public sealed record MateriaDto(Guid Id, string Nome);
public sealed class AulaResumoDto
{
    public Guid Id { get; init; }
    public string? Materia { get; init; }
    public string? Professor { get; init; }
    public DateTimeOffset? Inicio { get; init; }
    public int? DuracaoMinutos { get; init; }
    public string? Status { get; init; }
    public decimal? PrecoAcordado { get; init; }
}
