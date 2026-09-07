using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Common;
using TutorClass.Domain.Financial;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class AulaSchedulingTests
{
    [Fact]
    public async Task Aluno_solicita_aula_valida()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var service = new AulaService(db);

        var aula = await service.SolicitarAsync(scenario.AlunoId, CreateRequest(scenario), CancellationToken.None);

        Assert.Equal(StatusAula.Solicitada, aula.Status);
        Assert.Equal(scenario.ProfessorId, aula.Professor.Id);
        Assert.Equal(scenario.AlunoId, aula.Aluno.Id);
    }

    [Fact]
    public async Task Horario_passado_e_rejeitado()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var service = new AulaService(db);

        var request = CreateRequest(scenario) with { Inicio = DateTime.UtcNow.AddDays(-1) };

        await Assert.ThrowsAsync<DomainException>(() => service.SolicitarAsync(scenario.AlunoId, request, CancellationToken.None));
    }

    [Fact]
    public async Task Professor_nao_aprovado_nao_recebe_aula()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db, approvedProfessor: false);
        var service = new AulaService(db);

        await Assert.ThrowsAsync<DomainException>(() => service.SolicitarAsync(scenario.AlunoId, CreateRequest(scenario), CancellationToken.None));
    }

    [Fact]
    public async Task Professor_precisa_ensinar_materia()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db, teachesSubject: false);
        var service = new AulaService(db);

        await Assert.ThrowsAsync<DomainException>(() => service.SolicitarAsync(scenario.AlunoId, CreateRequest(scenario), CancellationToken.None));
    }

    [Fact]
    public async Task Fora_da_disponibilidade_rejeita()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var service = new AulaService(db);

        var request = CreateRequest(scenario) with { Inicio = scenario.InicioUtc.AddHours(5) };

        await Assert.ThrowsAsync<DomainException>(() => service.SolicitarAsync(scenario.AlunoId, request, CancellationToken.None));
    }

    [Fact]
    public async Task Conflito_do_professor_rejeita()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var outroAluno = new Aluno("Outro Aluno", "outro.aluno@teste.local", "hash");
        db.Add(outroAluno);
        await db.SaveChangesAsync();
        var service = new AulaService(db);

        await service.SolicitarAsync(scenario.AlunoId, CreateRequest(scenario), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() => service.SolicitarAsync(outroAluno.Id, CreateRequest(scenario), CancellationToken.None));
    }

    [Fact]
    public async Task Conflito_do_aluno_rejeita()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var outroProfessor = SeedProfessor(db, "Outro Professor", "outro.professor@teste.local", scenario.MateriaId, scenario.InicioUtc, approved: true);
        var service = new AulaService(db);

        await service.SolicitarAsync(scenario.AlunoId, CreateRequest(scenario), CancellationToken.None);

        var request = CreateRequest(scenario) with { ProfessorId = outroProfessor };
        await Assert.ThrowsAsync<DomainException>(() => service.SolicitarAsync(scenario.AlunoId, request, CancellationToken.None));
    }

    [Fact]
    public async Task Preco_acordado_vem_da_regra_do_backend()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db, precoHora: 90);
        var service = new AulaService(db);

        var aula = await service.SolicitarAsync(scenario.AlunoId, CreateRequest(scenario) with { DuracaoMinutos = 30 }, CancellationToken.None);

        Assert.Equal(45m, aula.PrecoAcordado);
    }

    [Fact]
    public async Task Professor_correto_confirma()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var service = new AulaService(db);
        var aula = await service.SolicitarAsync(scenario.AlunoId, CreateRequest(scenario), CancellationToken.None);

        var confirmada = await service.ConfirmarAsync(aula.Id, scenario.ProfessorId, CancellationToken.None);

        Assert.Equal(StatusAula.Confirmada, confirmada.Status);
    }

    [Fact]
    public async Task Outro_professor_nao_confirma()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var outroProfessorId = SeedProfessor(db, "Professor Intruso", "intruso@teste.local", scenario.MateriaId, scenario.InicioUtc, approved: true);
        var service = new AulaService(db);
        var aula = await service.SolicitarAsync(scenario.AlunoId, CreateRequest(scenario), CancellationToken.None);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ConfirmarAsync(aula.Id, outroProfessorId, CancellationToken.None));
    }

    [Fact]
    public async Task Aluno_cancela_propria_aula()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var service = new AulaService(db);
        var aula = await service.SolicitarAsync(scenario.AlunoId, CreateRequest(scenario), CancellationToken.None);

        var cancelada = await service.CancelarAsync(aula.Id, scenario.AlunoId, "Nao poderei participar.", CancellationToken.None);

        Assert.Equal(StatusAula.Cancelada, cancelada.Status);
    }

    [Fact]
    public async Task Outro_aluno_nao_cancela()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var outroAluno = new Aluno("Outro Aluno", "outro.cancelar@teste.local", "hash");
        db.Add(outroAluno);
        await db.SaveChangesAsync();
        var service = new AulaService(db);
        var aula = await service.SolicitarAsync(scenario.AlunoId, CreateRequest(scenario), CancellationToken.None);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CancelarAsync(aula.Id, outroAluno.Id, null, CancellationToken.None));
    }

    [Fact]
    public async Task Cancelada_nao_pode_ser_concluida()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var service = new AulaService(db);
        var aula = await service.SolicitarAsync(scenario.AlunoId, CreateRequest(scenario), CancellationToken.None);
        await service.CancelarAsync(aula.Id, scenario.AlunoId, null, CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() => service.ConcluirAsync(aula.Id, scenario.ProfessorId, CancellationToken.None));
    }

    [Fact]
    public async Task Confirmada_pode_ser_concluida()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var service = new AulaService(db);
        var aula = await service.SolicitarAsync(scenario.AlunoId, CreateRequest(scenario), CancellationToken.None);
        await service.ConfirmarAsync(aula.Id, scenario.ProfessorId, CancellationToken.None);

        var concluida = await service.ConcluirAsync(aula.Id, scenario.ProfessorId, CancellationToken.None);

        Assert.Equal(StatusAula.Concluida, concluida.Status);
    }

    [Fact]
    public async Task Aula_paga_pode_ser_reagendada_sem_gerar_novo_pagamento()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var aulaService = new AulaService(db);
        var financeiro = new FinanceiroService(db);
        var aula = await aulaService.SolicitarAsync(scenario.AlunoId, CreateRequest(scenario), CancellationToken.None);
        await financeiro.PagarAulaAsync(aula.Id, scenario.AlunoId, FormaPagamento.Pix, CancellationToken.None);
        var novoInicio = scenario.InicioUtc.AddDays(7);

        var reagendada = await aulaService.ReagendarAsync(aula.Id, scenario.AlunoId, new ReagendarAulaRequest(novoInicio, null), CancellationToken.None);

        Assert.Equal(novoInicio, reagendada.Inicio);
        Assert.Equal(StatusAula.Solicitada, reagendada.Status);
        Assert.Single(db.PagamentosAula.Where(p => p.AulaId == aula.Id));
    }

    [Fact]
    public async Task Reagendamento_registra_horario_anterior_no_historico()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var service = new AulaService(db);
        var aula = await service.SolicitarAsync(scenario.AlunoId, CreateRequest(scenario), CancellationToken.None);
        var novoInicio = scenario.InicioUtc.AddDays(7);

        await service.ReagendarAsync(aula.Id, scenario.ProfessorId, new ReagendarAulaRequest(novoInicio, 60), CancellationToken.None);

        var historico = Assert.Single(db.AulasReagendamentos);
        Assert.Equal(aula.Id, historico.AulaId);
        Assert.Equal(scenario.InicioUtc, historico.InicioAnteriorUtc);
        Assert.Equal(scenario.InicioUtc.AddMinutes(60), historico.FimAnteriorUtc);
        Assert.Equal(novoInicio, historico.InicioNovoUtc);
    }

    [Fact]
    public async Task Conflito_do_professor_rejeita_reagendamento()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var outroAluno = new Aluno("Outro Aluno Reagendamento", "outro.reagendamento@teste.local", "hash");
        db.Add(outroAluno);
        await db.SaveChangesAsync();
        var service = new AulaService(db);
        var aula = await service.SolicitarAsync(scenario.AlunoId, CreateRequest(scenario), CancellationToken.None);
        var conflito = scenario.InicioUtc.AddDays(7);
        var outraAula = new Aula(outroAluno.Id, scenario.ProfessorId, scenario.MateriaId, conflito, 60, 100m);
        db.Add(outraAula);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainException>(() => service.ReagendarAsync(aula.Id, scenario.AlunoId, new ReagendarAulaRequest(conflito, null), CancellationToken.None));
    }

    [Fact]
    public async Task Conflito_do_aluno_rejeita_reagendamento()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var outroProfessor = SeedProfessor(db, "Professor Reagendamento", "prof.reagendamento@teste.local", scenario.MateriaId, scenario.InicioUtc, approved: true);
        var service = new AulaService(db);
        var aula = await service.SolicitarAsync(scenario.AlunoId, CreateRequest(scenario), CancellationToken.None);
        var conflito = scenario.InicioUtc.AddDays(7);
        var outraAula = new Aula(scenario.AlunoId, outroProfessor, scenario.MateriaId, conflito, 60, 100m);
        db.Add(outraAula);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainException>(() => service.ReagendarAsync(aula.Id, scenario.AlunoId, new ReagendarAulaRequest(conflito, null), CancellationToken.None));
    }

    [Fact]
    public async Task Horario_passado_rejeita_reagendamento()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var service = new AulaService(db);
        var aula = await service.SolicitarAsync(scenario.AlunoId, CreateRequest(scenario), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() => service.ReagendarAsync(aula.Id, scenario.AlunoId, new ReagendarAulaRequest(DateTime.UtcNow.AddHours(-1), null), CancellationToken.None));
    }

    [Fact]
    public async Task Cancelar_e_reagendar_sao_acoes_diferentes()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db);
        var service = new AulaService(db);
        var aula = await service.SolicitarAsync(scenario.AlunoId, CreateRequest(scenario), CancellationToken.None);

        var reagendada = await service.ReagendarAsync(aula.Id, scenario.AlunoId, new ReagendarAulaRequest(scenario.InicioUtc.AddDays(7), null), CancellationToken.None);
        var cancelada = await service.CancelarAsync(aula.Id, scenario.AlunoId, "Nao poderei participar.", CancellationToken.None);

        Assert.Equal(StatusAula.Solicitada, reagendada.Status);
        Assert.Equal(StatusAula.Cancelada, cancelada.Status);
        await Assert.ThrowsAsync<DomainException>(() => service.ReagendarAsync(aula.Id, scenario.AlunoId, new ReagendarAulaRequest(scenario.InicioUtc.AddDays(14), null), CancellationToken.None));
    }

    [Fact]
    public async Task Disponibilidade_sobreposta_e_rejeitada()
    {
        await using var db = CreateDbContext();
        var scenario = SeedScenario(db, createAvailability: false);
        var service = new DisponibilidadeProfessorService(db);

        await service.CadastrarAsync(scenario.ProfessorId, new CadastrarDisponibilidadeProfessorRequest(DayOfWeek.Monday, new TimeOnly(14, 0), new TimeOnly(16, 0)), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.CadastrarAsync(scenario.ProfessorId, new CadastrarDisponibilidadeProfessorRequest(DayOfWeek.Monday, new TimeOnly(15, 0), new TimeOnly(17, 0)), CancellationToken.None));
    }

    [Fact]
    public async Task Acesso_nao_autorizado_recebe_status_apropriado()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/aulas", new SolicitarAulaRequest(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(7), 60));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static SolicitarAulaRequest CreateRequest(Scenario scenario)
    {
        return new SolicitarAulaRequest(scenario.ProfessorId, scenario.MateriaId, scenario.InicioUtc, 60);
    }

    private static Scenario SeedScenario(
        TutorClassDbContext db,
        bool approvedProfessor = true,
        bool teachesSubject = true,
        bool createAvailability = true,
        decimal precoHora = 80)
    {
        var materia = new Materia("Biologia");
        var aluno = new Aluno("Ana Clara", "ana.aula@teste.local", "hash");
        db.AddRange(materia, aluno);
        db.SaveChanges();

        var inicioUtc = NextMondayAt14Utc();
        var professorId = SeedProfessor(db, "Helena Duarte", "helena.aula@teste.local", teachesSubject ? materia.Id : Guid.NewGuid(), inicioUtc, approvedProfessor, createAvailability, precoHora);
        return new Scenario(aluno.Id, professorId, materia.Id, inicioUtc);
    }

    private static Guid SeedProfessor(
        TutorClassDbContext db,
        string nome,
        string email,
        Guid materiaId,
        DateTime inicioUtc,
        bool approved = true,
        bool createAvailability = true,
        decimal precoHora = 80)
    {
        var professor = new Professor(nome, email, "hash");
        professor.AtualizarPerfilProfissional("Professor para teste de aula.", precoHora);
        db.Add(professor);
        db.SaveChanges();

        if (materiaId != Guid.Empty && db.Materias.Any(m => m.Id == materiaId))
        {
            db.Add(new ProfessorMateria(professor.Id, materiaId));
        }

        if (createAvailability)
        {
            db.Add(new DisponibilidadeProfessor(professor.Id, inicioUtc.DayOfWeek, new TimeOnly(14, 0), new TimeOnly(18, 0)));
        }

        if (approved)
        {
            professor.Aprovar();
            var candidatura = new CandidaturaProfessor(professor.Id, "Teste");
            candidatura.IniciarAnalise(Guid.NewGuid());
            candidatura.Aprovar(Guid.NewGuid());
            db.Add(candidatura);
        }

        db.SaveChanges();
        return professor.Id;
    }

    private static DateTime NextMondayAt14Utc()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));
        while (date.DayOfWeek != DayOfWeek.Monday)
        {
            date = date.AddDays(1);
        }

        return date.ToDateTime(new TimeOnly(14, 0), DateTimeKind.Utc);
    }

    private static TutorClassDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TutorClassDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TutorClassDbContext(options);
    }

    private sealed record Scenario(Guid AlunoId, Guid ProfessorId, Guid MateriaId, DateTime InicioUtc);
}
