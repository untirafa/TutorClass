using System.Text.Json.Serialization;

namespace TutorClass.Mobile.Models;

public class ProfessorResumoDto
{
    public Guid Id { get; init; }
    public string Nome { get; init; } = "";
    public decimal? PrecoHora { get; init; }
    public string? NivelReputacao { get; init; }
    public decimal? MediaAvaliacoes { get; init; }
    public int QuantidadeAvaliacoes { get; init; }
    public string? TextoAvaliacoes { get; init; }
    public string[]? Materias { get; init; }
}

public sealed class ProfessorDetalhesDto : ProfessorResumoDto
{
    public QualificacaoProfessorDto[]? Qualificacoes { get; init; }
    public DisponibilidadeProfessorDto[]? Disponibilidades { get; init; }
}

public sealed class QualificacaoProfessorDto
{
    public string Titulo { get; init; } = "";
    public string? Instituicao { get; init; }
    public string? Descricao { get; init; }
    public DateOnly? DataConclusao { get; init; }
}

public sealed class DisponibilidadeProfessorDto
{
    [JsonConverter(typeof(JsonStringEnumConverter<DayOfWeek>))]
    public DayOfWeek DiaSemana { get; init; }
    public TimeOnly Inicio { get; init; }
    public TimeOnly Fim { get; init; }
}
