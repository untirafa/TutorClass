namespace TutorClass.Application.DTOs;

public sealed record AnalyticsResumoResponse(
    int TotalAlunos,
    int TotalProfessoresAprovados,
    int TotalAulas,
    int AulasConcluidas,
    int AulasCanceladas,
    decimal MediaAvaliacoes,
    int MentoriasAtivas,
    IReadOnlyCollection<AnalyticsPlanoQuantidadeResponse> AssinaturasPorPlano,
    string? MateriaMaisProcurada,
    string? ProfessorMaisContratado,
    decimal ReceitaComissaoSimulada,
    decimal RepassesProfessoresSimulados,
    decimal AdicionaisMentoriaSimulados);

public sealed record AnalyticsPlanoQuantidadeResponse(string Plano, int Quantidade);

public sealed record AnalyticsMateriaRankingResponse(
    Guid MateriaId,
    string Nome,
    int QuantidadeAulas,
    decimal Percentual,
    string Tendencia);

public sealed record DemandaMateriaResponse(
    Guid MateriaId,
    string Nome,
    int AulasUltimos30Dias,
    int Aulas30DiasAnteriores,
    decimal CrescimentoPercentual,
    int QuantidadeAlunosDistintos,
    decimal TaxaConclusao,
    string DemandaPrevista);
