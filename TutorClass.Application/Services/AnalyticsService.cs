using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Financial;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;

namespace TutorClass.Application.Services;

public sealed class AnalyticsService
{
    private static readonly DemandTrainingSample[] TrainingSamples =
    [
        new(0, -60, 0, 30, "Baixa"),
        new(2, -20, 1, 50, "Baixa"),
        new(4, 0, 2, 65, "Media"),
        new(8, 15, 4, 75, "Media"),
        new(12, 35, 6, 80, "Alta"),
        new(20, 60, 10, 90, "Alta")
    ];

    private readonly IApplicationDbContext _db;

    public AnalyticsService(IApplicationDbContext db)
    {
        _db = db;
    }

    public Task<AnalyticsResumoResponse> ObterResumoAsync(CancellationToken cancellationToken)
    {
        var aulas = _db.Aulas.ToArray();
        var pagamentosPagos = _db.PagamentosAula
            .Where(p => p.Status == StatusPagamentoFinanceiro.Pago)
            .ToArray();
        var adicionais = _db.AdicionaisMentoria.ToArray();
        var avaliacoes = _db.AvaliacoesProfessor.ToArray();

        var resumo = new AnalyticsResumoResponse(
            TotalAlunos: _db.Usuarios.OfType<Aluno>().Count(a => a.Ativo),
            TotalProfessoresAprovados: _db.Professores.Count(p => p.Ativo && p.Status == StatusProfessor.Aprovado),
            TotalAulas: aulas.Length,
            AulasConcluidas: aulas.Count(a => a.Status == StatusAula.Concluida),
            AulasCanceladas: aulas.Count(a => a.Status == StatusAula.Cancelada),
            MediaAvaliacoes: avaliacoes.Length == 0 ? 0m : decimal.Round((decimal)avaliacoes.Average(a => a.Nota), 2, MidpointRounding.AwayFromZero),
            MentoriasAtivas: _db.MentoriasAlunoProfessor.Count(m => m.Ativo),
            AssinaturasPorPlano: ObterAssinaturasPorPlano(),
            MateriaMaisProcurada: ObterMateriaMaisProcurada(aulas),
            ProfessorMaisContratado: ObterProfessorMaisContratado(aulas),
            ReceitaComissaoSimulada: decimal.Round(pagamentosPagos.Sum(p => p.ValorComissao), 2, MidpointRounding.AwayFromZero),
            RepassesProfessoresSimulados: decimal.Round(pagamentosPagos.Sum(p => p.ValorLiquidoProfessor), 2, MidpointRounding.AwayFromZero),
            AdicionaisMentoriaSimulados: decimal.Round(adicionais.Sum(a => a.Valor), 2, MidpointRounding.AwayFromZero));

        return Task.FromResult(resumo);
    }

    public Task<IReadOnlyCollection<AnalyticsMateriaRankingResponse>> ListarRankingMateriasAsync(CancellationToken cancellationToken)
    {
        var aulas = _db.Aulas.ToArray();
        var totalAulas = aulas.Length;
        var materias = _db.Materias.ToArray();

        var ranking = materias
            .Select(materia =>
            {
                var aulasDaMateria = aulas.Where(a => a.MateriaId == materia.Id).ToArray();
                var percentual = totalAulas == 0
                    ? 0m
                    : decimal.Round(aulasDaMateria.Length * 100m / totalAulas, 2, MidpointRounding.AwayFromZero);

                return new AnalyticsMateriaRankingResponse(
                    materia.Id,
                    materia.Nome,
                    aulasDaMateria.Length,
                    percentual,
                    CalcularTendencia(aulasDaMateria));
            })
            .OrderByDescending(m => m.QuantidadeAulas)
            .ThenBy(m => m.Nome)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<AnalyticsMateriaRankingResponse>>(ranking);
    }

    public Task<IReadOnlyCollection<DemandaMateriaResponse>> PreverDemandaMateriasAsync(CancellationToken cancellationToken)
    {
        var aulas = _db.Aulas.ToArray();
        var materias = _db.Materias.ToArray();
        var classifier = DemandCentroidClassifier.Train(TrainingSamples);

        var previsoes = materias
            .Select(materia =>
            {
                var features = BuildFeatures(materia.Id, aulas);
                return new DemandaMateriaResponse(
                    materia.Id,
                    materia.Nome,
                    features.AulasUltimos30Dias,
                    features.Aulas30DiasAnteriores,
                    features.CrescimentoPercentual,
                    features.QuantidadeAlunosDistintos,
                    features.TaxaConclusao,
                    classifier.Predict(features));
            })
            .OrderByDescending(p => p.AulasUltimos30Dias)
            .ThenBy(p => p.Nome)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<DemandaMateriaResponse>>(previsoes);
    }

    private IReadOnlyCollection<AnalyticsPlanoQuantidadeResponse> ObterAssinaturasPorPlano()
    {
        var assinaturasAtivas = _db.AssinaturasAluno
            .Where(a => a.Ativa)
            .GroupBy(a => a.Plano)
            .Select(g => new AnalyticsPlanoQuantidadeResponse(g.Key.ToString(), g.Count()))
            .ToList();

        var alunosComPlanoPago = _db.AssinaturasAluno
            .Where(a => a.Ativa)
            .Select(a => a.AlunoId)
            .Distinct()
            .ToHashSet();

        var alunosGratuitos = _db.Usuarios
            .OfType<Aluno>()
            .Count(a => a.Ativo && !alunosComPlanoPago.Contains(a.Id));

        assinaturasAtivas.Add(new AnalyticsPlanoQuantidadeResponse(PlanoAluno.Gratuito.ToString(), alunosGratuitos));

        return assinaturasAtivas
            .OrderBy(a => a.Plano)
            .ToArray();
    }

    private string? ObterMateriaMaisProcurada(IReadOnlyCollection<Aula> aulas)
    {
        var materiaId = aulas
            .GroupBy(a => a.MateriaId)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => (Guid?)g.Key)
            .FirstOrDefault();

        return materiaId is null
            ? null
            : _db.Materias.Where(m => m.Id == materiaId.Value).Select(m => m.Nome).SingleOrDefault();
    }

    private string? ObterProfessorMaisContratado(IReadOnlyCollection<Aula> aulas)
    {
        var professorId = aulas
            .GroupBy(a => a.ProfessorId)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => (Guid?)g.Key)
            .FirstOrDefault();

        return professorId is null
            ? null
            : _db.Professores.Where(p => p.Id == professorId.Value).Select(p => p.Nome).SingleOrDefault();
    }

    private static DemandFeatures BuildFeatures(Guid materiaId, IReadOnlyCollection<Aula> aulas)
    {
        var aulasDaMateria = aulas.Where(a => a.MateriaId == materiaId).ToArray();
        return BuildFeaturesFromLessons(aulasDaMateria);
    }

    private static DemandFeatures BuildFeaturesFromLessons(IReadOnlyCollection<Aula> aulasDaMateria)
    {
        var now = DateTime.UtcNow;
        var recentStart = now.AddDays(-30);
        var previousStart = now.AddDays(-60);
        var recent = aulasDaMateria.Count(a => a.InicioUtc >= recentStart && a.InicioUtc <= now);
        var previous = aulasDaMateria.Count(a => a.InicioUtc >= previousStart && a.InicioUtc < recentStart);
        var growth = previous == 0
            ? recent > 0 ? 100m : 0m
            : decimal.Round((recent - previous) * 100m / previous, 2, MidpointRounding.AwayFromZero);
        var completionRate = aulasDaMateria.Count == 0
            ? 0m
            : decimal.Round(aulasDaMateria.Count(a => a.Status == StatusAula.Concluida) * 100m / aulasDaMateria.Count, 2, MidpointRounding.AwayFromZero);

        return new DemandFeatures(
            recent,
            previous,
            growth,
            aulasDaMateria.Select(a => a.AlunoId).Distinct().Count(),
            completionRate);
    }

    private static string CalcularTendencia(IReadOnlyCollection<Aula> aulas)
    {
        var features = BuildFeaturesFromLessons(aulas);
        if (features.AulasUltimos30Dias == 0)
        {
            return "Baixa";
        }

        if (features.Aulas30DiasAnteriores == 0)
        {
            return features.AulasUltimos30Dias >= 3 ? "Alta" : "Media";
        }

        return features.CrescimentoPercentual switch
        {
            >= 20m => "Alta",
            <= -20m => "Baixa",
            _ => "Media"
        };
    }

    private sealed record DemandFeatures(
        int AulasUltimos30Dias,
        int Aulas30DiasAnteriores,
        decimal CrescimentoPercentual,
        int QuantidadeAlunosDistintos,
        decimal TaxaConclusao);

    private sealed record DemandTrainingSample(
        int AulasUltimos30Dias,
        decimal CrescimentoPercentual,
        int QuantidadeAlunosDistintos,
        decimal TaxaConclusao,
        string Label);

    private sealed class DemandCentroidClassifier
    {
        private readonly IReadOnlyCollection<DemandCentroid> _centroids;

        private DemandCentroidClassifier(IReadOnlyCollection<DemandCentroid> centroids)
        {
            _centroids = centroids;
        }

        public static DemandCentroidClassifier Train(IReadOnlyCollection<DemandTrainingSample> samples)
        {
            var centroids = samples
                .GroupBy(s => s.Label)
                .Select(g => new DemandCentroid(
                    g.Key,
                    g.Average(s => NormalizeLessons(s.AulasUltimos30Dias)),
                    g.Average(s => NormalizeGrowth(s.CrescimentoPercentual)),
                    g.Average(s => NormalizeStudents(s.QuantidadeAlunosDistintos)),
                    g.Average(s => NormalizeRate(s.TaxaConclusao))))
                .ToArray();

            return new DemandCentroidClassifier(centroids);
        }

        public string Predict(DemandFeatures features)
        {
            if (features.AulasUltimos30Dias == 0 && features.Aulas30DiasAnteriores == 0)
            {
                return "Baixa";
            }

            return _centroids
                .OrderBy(c => Distance(c, features))
                .Select(c => c.Label)
                .First();
        }

        private static double Distance(DemandCentroid centroid, DemandFeatures features)
        {
            var lessons = NormalizeLessons(features.AulasUltimos30Dias) - centroid.AulasUltimos30Dias;
            var growth = NormalizeGrowth(features.CrescimentoPercentual) - centroid.CrescimentoPercentual;
            var students = NormalizeStudents(features.QuantidadeAlunosDistintos) - centroid.QuantidadeAlunosDistintos;
            var completion = NormalizeRate(features.TaxaConclusao) - centroid.TaxaConclusao;

            return Math.Sqrt(lessons * lessons + growth * growth + students * students + completion * completion);
        }

        private static double NormalizeLessons(int value) => Math.Min(value, 30) / 30d;

        private static double NormalizeStudents(int value) => Math.Min(value, 30) / 30d;

        private static double NormalizeGrowth(decimal value) => Math.Clamp((double)value, -100d, 100d) / 100d;

        private static double NormalizeRate(decimal value) => Math.Clamp((double)value, 0d, 100d) / 100d;
    }

    private sealed record DemandCentroid(
        string Label,
        double AulasUltimos30Dias,
        double CrescimentoPercentual,
        double QuantidadeAlunosDistintos,
        double TaxaConclusao);
}
