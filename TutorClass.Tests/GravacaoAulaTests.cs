using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Common;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class GravacaoAulaTests
{
    private const string VideoUrl = "https://www.w3schools.com/html/mov_bbb.mp4";

    [Fact]
    public async Task Gratuito_participa_de_aula_mas_nao_acessa_gravacao()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var service = new GravacaoAulaService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.BuscarParaAlunoAsync(scenario.AulaGratuitoId, scenario.AlunoGratuitoId, CancellationToken.None));
    }

    [Fact]
    public async Task Foco_participa_de_aula_mas_nao_acessa_gravacao()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var service = new GravacaoAulaService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.BuscarParaAlunoAsync(scenario.AulaFocoId, scenario.AlunoFocoId, CancellationToken.None));
    }

    [Fact]
    public async Task Mentoria_pode_acessar_gravacao_elegivel()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var service = new GravacaoAulaService(db);

        var gravacao = await service.BuscarParaAlunoAsync(scenario.AulaMentoriaId, scenario.AlunoMentoriaId, CancellationToken.None);

        Assert.Equal("Biologia - Sistema Respiratorio", gravacao.Titulo);
        Assert.Equal(VideoUrl, gravacao.UrlVideo);
        Assert.True(gravacao.Disponivel);
    }

    [Fact]
    public async Task Aluno_mentoria_nao_acessa_gravacao_de_outro_aluno()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var outroMentoria = new Aluno("Outro Mentoria", "outro.mentoria@teste.local", "hash");
        db.Add(outroMentoria);
        db.SaveChanges();
        await new PlanoAlunoService(db).AssinarAsync(outroMentoria.Id, new AssinarPlanoRequest(PlanoAluno.Mentoria), CancellationToken.None);
        var service = new GravacaoAulaService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.BuscarParaAlunoAsync(scenario.AulaMentoriaId, outroMentoria.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Gravacao_indisponivel_nao_pode_ser_acessada()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db, gravacaoDisponivel: false);
        var service = new GravacaoAulaService(db);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.BuscarParaAlunoAsync(scenario.AulaMentoriaId, scenario.AlunoMentoriaId, CancellationToken.None));
    }

    [Fact]
    public async Task Gravacao_disponivel_pode_ser_acessada()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var service = new GravacaoAulaService(db);

        var gravacao = await service.BuscarParaAlunoAsync(scenario.AulaMentoriaId, scenario.AlunoMentoriaId, CancellationToken.None);

        Assert.Equal(3120, gravacao.DuracaoSegundos);
    }

    [Fact]
    public async Task Aula_nao_concluida_nao_libera_gravacao()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db, criarGravacao: false);
        var aula = new Aula(scenario.AlunoMentoriaId, scenario.ProfessorId, scenario.MateriaId, DateTime.UtcNow.AddDays(3), 60, 80m);
        db.Add(aula);
        db.SaveChanges();
        var service = new GravacaoAulaService(db);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(aula.Id, Guid.NewGuid(), administrador: true, CreateRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task Administrador_pode_cadastrar_referencia_tecnica()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db, criarGravacao: false);
        var service = new GravacaoAulaService(db);

        var adminId = Guid.NewGuid();

        var gravacao = await service.CriarAsync(scenario.AulaMentoriaId, adminId, administrador: true, CreateRequest(), CancellationToken.None);

        Assert.Equal(scenario.AulaMentoriaId, gravacao.AulaId);
        Assert.Single(db.GravacoesAula);
    }

    [Fact]
    public async Task Professor_nao_decide_disponibilidade_da_gravacao()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db, criarGravacao: false);
        var service = new GravacaoAulaService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CriarAsync(scenario.AulaMentoriaId, scenario.ProfessorId, administrador: false, CreateRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task Usuario_comum_nao_autorizado_nao_pode_cadastrar_referencia()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db, criarGravacao: false);
        var service = new GravacaoAulaService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CriarAsync(scenario.AulaMentoriaId, scenario.AlunoMentoriaId, administrador: false, CreateRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task Mentoria_cancelada_bloqueia_acesso()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        await new PlanoAlunoService(db).CancelarAtualAsync(scenario.AlunoMentoriaId, CancellationToken.None);
        var service = new GravacaoAulaService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.BuscarParaAlunoAsync(scenario.AulaMentoriaId, scenario.AlunoMentoriaId, CancellationToken.None));
    }

    [Fact]
    public async Task Gravacao_e_salva_com_metadados_preservados()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db, criarGravacao: false);
        var service = new GravacaoAulaService(db);

        var gravacao = await service.CriarAsync(scenario.AulaMentoriaId, Guid.NewGuid(), administrador: true, CreateRequest(), CancellationToken.None);

        Assert.Equal("Biologia - Sistema Respiratorio", gravacao.Titulo);
        Assert.Equal(VideoUrl, gravacao.UrlVideo);
        Assert.Equal(3120, gravacao.DuracaoSegundos);
        Assert.NotNull(gravacao.DataDisponibilizacaoUtc);
        Assert.Equal("Biologia", gravacao.Materia);
        Assert.Equal("Helena", gravacao.Professor);
    }

    [Fact]
    public async Task Nao_permite_gravacao_duplicada_para_mesma_aula()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var service = new GravacaoAulaService(db);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarAsync(scenario.AulaMentoriaId, Guid.NewGuid(), administrador: true, CreateRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task Listagem_minhas_retorna_apenas_gravacoes_do_aluno_mentoria()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var outraAula = SeedCompletedLesson(db, scenario.AlunoGratuitoId, scenario.ProfessorId, scenario.MateriaId);
        db.Add(new GravacaoAula(outraAula, "Gravacao - Outro aluno", VideoUrl, 1200));
        db.SaveChanges();
        var service = new GravacaoAulaService(db);

        var gravacoes = await service.ListarMinhasAsync(scenario.AlunoMentoriaId, CancellationToken.None);

        var gravacao = Assert.Single(gravacoes);
        Assert.Equal(scenario.AulaMentoriaId, gravacao.AulaId);
    }

    [Fact]
    public async Task Endpoint_sem_jwt_retorna_unauthorized()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/aulas/{Guid.NewGuid()}/gravacao");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static CriarGravacaoAulaRequest CreateRequest()
    {
        return new CriarGravacaoAulaRequest(
            "Biologia - Sistema Respiratorio",
            VideoUrl,
            3120,
            "Video externo de demonstracao.");
    }

    private static Scenario SeedScenario(TutorClassDbContext db, bool criarGravacao = true, bool gravacaoDisponivel = true)
    {
        var materia = new Materia("Biologia");
        var gratuito = new Aluno("Ana Gratuita", "ana.gravacao@teste.local", "hash");
        var foco = new Aluno("Bruno Foco", "bruno.gravacao@teste.local", "hash");
        var mentoria = new Aluno("Camila Mentoria", "camila.gravacao@teste.local", "hash");
        var professor = CreateProfessor("Helena", "helena.gravacao@teste.local");
        db.AddRange(materia, gratuito, foco, mentoria, professor);
        db.SaveChanges();

        db.Add(new ProfessorMateria(professor.Id, materia.Id));
        db.SaveChanges();

        var planos = new PlanoAlunoService(db);
        planos.AssinarAsync(foco.Id, new AssinarPlanoRequest(PlanoAluno.Foco), CancellationToken.None).GetAwaiter().GetResult();
        planos.AssinarAsync(mentoria.Id, new AssinarPlanoRequest(PlanoAluno.Mentoria), CancellationToken.None).GetAwaiter().GetResult();

        var aulaGratuito = SeedCompletedLesson(db, gratuito.Id, professor.Id, materia.Id);
        var aulaFoco = SeedCompletedLesson(db, foco.Id, professor.Id, materia.Id);
        var aulaMentoria = SeedCompletedLesson(db, mentoria.Id, professor.Id, materia.Id);

        if (criarGravacao)
        {
            var gravacaoGratuito = new GravacaoAula(aulaGratuito, "Gravacao - Gratuito", VideoUrl, 1800);
            var gravacaoFoco = new GravacaoAula(aulaFoco, "Gravacao - Foco", VideoUrl, 1800);
            var gravacaoMentoria = new GravacaoAula(aulaMentoria, "Biologia - Sistema Respiratorio", VideoUrl, 3120);

            if (!gravacaoDisponivel)
            {
                gravacaoMentoria.Desativar();
            }

            db.AddRange(gravacaoGratuito, gravacaoFoco, gravacaoMentoria);
            db.SaveChanges();
        }

        return new Scenario(gratuito.Id, foco.Id, mentoria.Id, professor.Id, materia.Id, aulaGratuito.Id, aulaFoco.Id, aulaMentoria.Id);
    }

    private static Professor CreateProfessor(string nome, string email)
    {
        var professor = new Professor(nome, email, "hash");
        professor.AtualizarPerfilProfissional("Professor para teste de gravacao.", 80m);
        professor.Aprovar();
        return professor;
    }

    private static Aula SeedCompletedLesson(TutorClassDbContext db, Guid alunoId, Guid professorId, Guid materiaId)
    {
        var aula = new Aula(alunoId, professorId, materiaId, DateTime.UtcNow.AddDays(-3).AddMinutes(db.Aulas.Count() * 90), 60, 80m);
        db.Add(aula);
        db.SaveChanges();
        aula.Confirmar(professorId);
        aula.Concluir(professorId);
        db.SaveChanges();
        return aula;
    }

    private static TutorClassDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TutorClassDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TutorClassDbContext(options);
    }

    private sealed record Scenario(
        Guid AlunoGratuitoId,
        Guid AlunoFocoId,
        Guid AlunoMentoriaId,
        Guid ProfessorId,
        Guid MateriaId,
        Guid AulaGratuitoId,
        Guid AulaFocoId,
        Guid AulaMentoriaId);
}
