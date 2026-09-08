using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Financial;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Mentoring;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class AnalyticsTests
{
    [Fact]
    public async Task Endpoint_de_analytics_exige_administrador()
    {
        await using var factory = new TutorClassApiFactory();
        var scenario = await SeedScenarioAsync(factory);
        var anonymous = factory.CreateClient();

        var anonymousResponse = await anonymous.GetAsync("/api/admin/analytics/resumo");

        var studentClient = factory.CreateClient();
        await AuthorizeAsync(studentClient, scenario.AlunoEmail);
        var studentResponse = await studentClient.GetAsync("/api/admin/analytics/resumo");

        var adminClient = factory.CreateClient();
        await AuthorizeAsync(adminClient, scenario.AdminEmail);
        var adminResponse = await adminClient.GetAsync("/api/admin/analytics/resumo");

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, studentResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
    }

    [Fact]
    public async Task Indicadores_retornam_dados_agregados_corretos()
    {
        await using var db = CreateDbContext();
        SeedScenario(db);
        var service = new AnalyticsService(db);

        var resumo = await service.ObterResumoAsync(CancellationToken.None);

        Assert.Equal(2, resumo.TotalAlunos);
        Assert.Equal(2, resumo.TotalProfessoresAprovados);
        Assert.Equal(6, resumo.TotalAulas);
        Assert.Equal(3, resumo.AulasConcluidas);
        Assert.Equal(2, resumo.AulasCanceladas);
        Assert.Equal(4.5m, resumo.MediaAvaliacoes);
        Assert.Equal(1, resumo.MentoriasAtivas);
        Assert.Equal("Matematica", resumo.MateriaMaisProcurada);
        Assert.Equal(12m, resumo.ReceitaComissaoSimulada);
        Assert.Equal(88m, resumo.RepassesProfessoresSimulados);
        Assert.Equal(20m, resumo.AdicionaisMentoriaSimulados);
        Assert.Contains(resumo.AssinaturasPorPlano, p => p.Plano == "Foco" && p.Quantidade == 1);
        Assert.Contains(resumo.AssinaturasPorPlano, p => p.Plano == "Mentoria" && p.Quantidade == 1);
    }

    [Fact]
    public async Task Ranking_de_materias_funciona()
    {
        await using var db = CreateDbContext();
        SeedScenario(db);
        var service = new AnalyticsService(db);

        var ranking = await service.ListarRankingMateriasAsync(CancellationToken.None);

        var primeira = ranking.First();
        Assert.Equal("Matematica", primeira.Nome);
        Assert.Equal(4, primeira.QuantidadeAulas);
        Assert.Equal(66.67m, primeira.Percentual);
        Assert.Equal("Alta", primeira.Tendencia);
        Assert.Contains(ranking, m => m.Nome == "Historia" && m.Tendencia == "Baixa");
    }

    [Fact]
    public async Task Classificacao_de_demanda_retorna_baixa_media_ou_alta()
    {
        await using var db = CreateDbContext();
        SeedScenario(db);
        var service = new AnalyticsService(db);

        var previsoes = await service.PreverDemandaMateriasAsync(CancellationToken.None);
        var labelsPermitidos = new[] { "Baixa", "Media", "Alta" };

        Assert.All(previsoes, p => Assert.Contains(p.DemandaPrevista, labelsPermitidos));
        Assert.Equal("Alta", previsoes.Single(p => p.Nome == "Matematica").DemandaPrevista);
        Assert.Equal("Baixa", previsoes.Single(p => p.Nome == "Historia").DemandaPrevista);
    }

    [Fact]
    public async Task Dataset_vazio_nao_quebra_api()
    {
        await using var db = CreateDbContext();
        var service = new AnalyticsService(db);

        var resumo = await service.ObterResumoAsync(CancellationToken.None);
        var ranking = await service.ListarRankingMateriasAsync(CancellationToken.None);
        var demanda = await service.PreverDemandaMateriasAsync(CancellationToken.None);

        Assert.Equal(0, resumo.TotalAulas);
        Assert.Null(resumo.MateriaMaisProcurada);
        Assert.Empty(ranking);
        Assert.Empty(demanda);
    }

    [Fact]
    public async Task Respostas_nao_expoem_informacao_pessoal_sensivel()
    {
        await using var db = CreateDbContext();
        SeedScenario(db);
        var service = new AnalyticsService(db);

        var resumo = await service.ObterResumoAsync(CancellationToken.None);
        var ranking = await service.ListarRankingMateriasAsync(CancellationToken.None);
        var demanda = await service.PreverDemandaMateriasAsync(CancellationToken.None);
        var json = JsonSerializer.Serialize(new { resumo, ranking, demanda });

        Assert.DoesNotContain("@teste.local", json);
        Assert.DoesNotContain("Password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Hash", json, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<AnalyticsScenario> SeedScenarioAsync(TutorClassApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
        var scenario = SeedScenario(db);

        foreach (var usuario in db.Usuarios)
        {
            usuario.DefinirPasswordHash(hasher.HashPassword(usuario, "TutorClass@123"));
        }

        await db.SaveChangesAsync();
        return scenario;
    }

    private static AnalyticsScenario SeedScenario(TutorClassDbContext db)
    {
        var admin = new Administrador("Admin Analytics", "admin.analytics@teste.local", "hash");
        var aluno = new Aluno("Aluno Analytics", "aluno.analytics@teste.local", "hash");
        var outroAluno = new Aluno("Outro Aluno Analytics", "outro.aluno.analytics@teste.local", "hash");
        var professor = CreateProfessor("Professor Analytics", "prof.analytics@teste.local", 100m);
        var outroProfessor = CreateProfessor("Outro Professor Analytics", "outro.prof.analytics@teste.local", 80m);
        var matematica = new Materia("Matematica");
        var historia = new Materia("Historia");

        db.AddRange(admin, aluno, outroAluno, professor, outroProfessor, matematica, historia);
        db.SaveChanges();
        db.AddRange(
            new ProfessorMateria(professor.Id, matematica.Id),
            new ProfessorMateria(outroProfessor.Id, historia.Id),
            new AssinaturaAluno(aluno.Id, PlanoAluno.Foco, 29.99m, 19.99m, true),
            new AssinaturaAluno(outroAluno.Id, PlanoAluno.Mentoria, 79.99m, 59.99m, true),
            new MentoriaAlunoProfessor(outroAluno.Id, outroProfessor.Id, 1));
        db.SaveChanges();

        var aulaMatematica1 = AddLesson(db, aluno.Id, professor.Id, matematica.Id, DateTime.UtcNow.AddDays(-5), StatusAula.Concluida);
        AddLesson(db, outroAluno.Id, professor.Id, matematica.Id, DateTime.UtcNow.AddDays(-10), StatusAula.Concluida);
        AddLesson(db, aluno.Id, professor.Id, matematica.Id, DateTime.UtcNow.AddDays(-15), StatusAula.Cancelada);
        AddLesson(db, aluno.Id, professor.Id, matematica.Id, DateTime.UtcNow.AddDays(-40), StatusAula.Concluida);
        AddLesson(db, outroAluno.Id, outroProfessor.Id, historia.Id, DateTime.UtcNow.AddDays(-35), StatusAula.Solicitada);
        AddLesson(db, outroAluno.Id, outroProfessor.Id, historia.Id, DateTime.UtcNow.AddDays(-45), StatusAula.Cancelada);

        var pagamento = new PagamentoAula(aulaMatematica1.Id, aluno.Id, aluno.Id, professor.Id, 100m);
        pagamento.MarcarComoPago();
        db.Add(pagamento);
        db.AddRange(
            new AvaliacaoProfessor(aulaMatematica1.Id, aluno.Id, professor.Id, 5, null),
            new AvaliacaoProfessor(Guid.NewGuid(), outroAluno.Id, outroProfessor.Id, 4, null),
            new AdicionalMentoria(outroProfessor.Id, outroAluno.Id, aulaMatematica1.Id, Guid.NewGuid(), 20m));
        db.SaveChanges();

        return new AnalyticsScenario(admin.Email, aluno.Email);
    }

    private static Aula AddLesson(TutorClassDbContext db, Guid alunoId, Guid professorId, Guid materiaId, DateTime inicioUtc, StatusAula status)
    {
        var aula = new Aula(alunoId, professorId, materiaId, inicioUtc, 60, 100m);
        db.Add(aula);
        db.SaveChanges();

        if (status == StatusAula.Confirmada)
        {
            aula.Confirmar(professorId);
        }
        else if (status == StatusAula.Concluida)
        {
            aula.Confirmar(professorId);
            aula.Concluir(professorId);
        }
        else if (status == StatusAula.Cancelada)
        {
            aula.Cancelar(alunoId, null);
        }

        db.SaveChanges();
        return aula;
    }

    private static Professor CreateProfessor(string nome, string email, decimal precoHora)
    {
        var professor = new Professor(nome, email, "hash");
        professor.AtualizarPerfilProfissional("Professor aprovado para analytics.", precoHora);
        professor.Aprovar();
        return professor;
    }

    private static async Task AuthorizeAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "TutorClass@123"));
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
    }

    private static TutorClassDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TutorClassDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TutorClassDbContext(options);
    }

    private sealed record AnalyticsScenario(string AdminEmail, string AlunoEmail);
}
