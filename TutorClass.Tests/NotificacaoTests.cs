using System.Net;
using Microsoft.EntityFrameworkCore;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Common;
using TutorClass.Domain.Financial;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Mentoring;
using TutorClass.Domain.Notifications;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class NotificacaoTests
{
    private const string VideoUrl = "https://www.w3schools.com/html/mov_bbb.mp4";

    [Fact]
    public async Task Cria_notificacao()
    {
        await using var db = CreateDbContext();
        var usuario = SeedAluno(db, "Ana", "ana.notificacao@teste.local");
        var service = new NotificacaoService(db);

        await service.CriarPersistidaAsync(usuario.Id, TipoNotificacao.AulaConfirmada, "Aula confirmada", "Sua aula foi confirmada.", "/minhas-aulas", Guid.NewGuid(), "TESTE:CRIAR", CancellationToken.None);

        var notificacao = Assert.Single(db.Notificacoes);
        Assert.Equal(usuario.Id, notificacao.UsuarioId);
        Assert.False(notificacao.Lida);
    }

    [Fact]
    public async Task Lista_apenas_notificacoes_do_usuario_e_ordena_recentes()
    {
        await using var db = CreateDbContext();
        var ana = SeedAluno(db, "Ana", "ana.lista@teste.local");
        var bruno = SeedAluno(db, "Bruno", "bruno.lista@teste.local");
        var service = new NotificacaoService(db);
        var antiga = await service.CriarPersistidaAsync(ana.Id, TipoNotificacao.AulaSolicitada, "Antiga", "Mensagem antiga.", "/minhas-aulas", Guid.NewGuid(), "LISTA:ANTIGA", CancellationToken.None);
        var recente = await service.CriarPersistidaAsync(ana.Id, TipoNotificacao.AulaConfirmada, "Recente", "Mensagem recente.", "/minhas-aulas", Guid.NewGuid(), "LISTA:RECENTE", CancellationToken.None);
        await service.CriarPersistidaAsync(bruno.Id, TipoNotificacao.AulaConfirmada, "Outro", "Mensagem outro.", "/minhas-aulas", Guid.NewGuid(), "LISTA:OUTRO", CancellationToken.None);
        SetCreatedAt(db, antiga.Id, DateTime.UtcNow.AddHours(-2));
        SetCreatedAt(db, recente.Id, DateTime.UtcNow.AddHours(-1));

        var notificacoes = await service.ListarAsync(ana.Id, somenteNaoLidas: false, page: 1, pageSize: 20, CancellationToken.None);

        Assert.Equal(["Recente", "Antiga"], notificacoes.Select(n => n.Titulo));
    }

    [Fact]
    public async Task Marcar_como_lida_funciona_e_e_idempotente()
    {
        await using var db = CreateDbContext();
        var aluno = SeedAluno(db, "Ana", "ana.lida@teste.local");
        var service = new NotificacaoService(db);
        var criada = await service.CriarPersistidaAsync(aluno.Id, TipoNotificacao.AulaConfirmada, "Aula", "Mensagem.", "/minhas-aulas", Guid.NewGuid(), "LIDA:1", CancellationToken.None);

        var primeira = await service.MarcarComoLidaAsync(aluno.Id, criada.Id, CancellationToken.None);
        var segunda = await service.MarcarComoLidaAsync(aluno.Id, criada.Id, CancellationToken.None);

        Assert.True(primeira.Lida);
        Assert.NotNull(primeira.DataLeituraUtc);
        Assert.Equal(primeira.DataLeituraUtc, segunda.DataLeituraUtc);
    }

    [Fact]
    public async Task Usuario_nao_marca_notificacao_de_outro()
    {
        await using var db = CreateDbContext();
        var ana = SeedAluno(db, "Ana", "ana.outro@teste.local");
        var bruno = SeedAluno(db, "Bruno", "bruno.outro@teste.local");
        var service = new NotificacaoService(db);
        var criada = await service.CriarPersistidaAsync(ana.Id, TipoNotificacao.AulaConfirmada, "Aula", "Mensagem.", "/minhas-aulas", Guid.NewGuid(), "OUTRO:1", CancellationToken.None);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.MarcarComoLidaAsync(bruno.Id, criada.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Resumo_conta_nao_lidas_e_marcar_todas_zera()
    {
        await using var db = CreateDbContext();
        var aluno = SeedAluno(db, "Ana", "ana.resumo@teste.local");
        var service = new NotificacaoService(db);
        var primeira = await service.CriarPersistidaAsync(aluno.Id, TipoNotificacao.AulaConfirmada, "Aula 1", "Mensagem.", "/minhas-aulas", Guid.NewGuid(), "RESUMO:1", CancellationToken.None);
        await service.CriarPersistidaAsync(aluno.Id, TipoNotificacao.AulaCancelada, "Aula 2", "Mensagem.", "/minhas-aulas", Guid.NewGuid(), "RESUMO:2", CancellationToken.None);

        Assert.Equal(2, (await service.ObterResumoAsync(aluno.Id, CancellationToken.None)).NaoLidas);
        await service.MarcarComoLidaAsync(aluno.Id, primeira.Id, CancellationToken.None);
        Assert.Equal(1, (await service.ObterResumoAsync(aluno.Id, CancellationToken.None)).NaoLidas);
        var todas = await service.MarcarTodasComoLidasAsync(aluno.Id, CancellationToken.None);

        Assert.Equal(1, todas.QuantidadeAlterada);
        Assert.Equal(0, (await service.ObterResumoAsync(aluno.Id, CancellationToken.None)).NaoLidas);
    }

    [Fact]
    public async Task Mesma_chave_evento_nao_cria_duplicidade()
    {
        await using var db = CreateDbContext();
        var aluno = SeedAluno(db, "Ana", "ana.dup@teste.local");
        var service = new NotificacaoService(db);

        await service.CriarPersistidaAsync(aluno.Id, TipoNotificacao.AulaConfirmada, "Aula", "Mensagem.", "/minhas-aulas", Guid.NewGuid(), "DUP:1", CancellationToken.None);
        await service.CriarPersistidaAsync(aluno.Id, TipoNotificacao.AulaConfirmada, "Aula repetida", "Mensagem repetida.", "/minhas-aulas", Guid.NewGuid(), "DUP:1", CancellationToken.None);

        Assert.Single(db.Notificacoes);
    }

    [Fact]
    public async Task Nova_solicitacao_de_aula_notifica_professor()
    {
        await using var db = CreateDbContext();
        var scenario = SeedLessonScenario(db);
        var service = new AulaService(db);

        var aula = await service.SolicitarAsync(scenario.AlunoId, CreateLessonRequest(scenario), CancellationToken.None);

        Assert.Contains(db.Notificacoes, n => n.UsuarioId == scenario.ProfessorId && n.Tipo == TipoNotificacao.AulaSolicitada && n.ReferenciaId == aula.Id);
    }

    [Fact]
    public async Task Confirmacao_de_aula_notifica_aluno()
    {
        await using var db = CreateDbContext();
        var scenario = SeedLessonScenario(db);
        var service = new AulaService(db);
        var aula = await service.SolicitarAsync(scenario.AlunoId, CreateLessonRequest(scenario), CancellationToken.None);

        await service.ConfirmarAsync(aula.Id, scenario.ProfessorId, CancellationToken.None);

        Assert.Contains(db.Notificacoes, n => n.UsuarioId == scenario.AlunoId && n.Tipo == TipoNotificacao.AulaConfirmada && n.ReferenciaId == aula.Id);
    }

    [Fact]
    public async Task Cancelamento_de_aula_notifica_outra_parte()
    {
        await using var db = CreateDbContext();
        var scenario = SeedLessonScenario(db);
        var service = new AulaService(db);
        var aula = await service.SolicitarAsync(scenario.AlunoId, CreateLessonRequest(scenario), CancellationToken.None);

        await service.CancelarAsync(aula.Id, scenario.AlunoId, "Imprevisto.", CancellationToken.None);

        Assert.Contains(db.Notificacoes, n => n.UsuarioId == scenario.ProfessorId && n.Tipo == TipoNotificacao.AulaCancelada && n.ReferenciaId == aula.Id);
        Assert.DoesNotContain(db.Notificacoes, n => n.UsuarioId == scenario.AlunoId && n.Tipo == TipoNotificacao.AulaCancelada && n.ReferenciaId == aula.Id);
    }

    [Fact]
    public async Task Reagendamento_de_aula_notifica_outra_parte()
    {
        await using var db = CreateDbContext();
        var scenario = SeedLessonScenario(db);
        var service = new AulaService(db);
        var aula = await service.SolicitarAsync(scenario.AlunoId, CreateLessonRequest(scenario), CancellationToken.None);

        await service.ReagendarAsync(aula.Id, scenario.AlunoId, new ReagendarAulaRequest(scenario.InicioUtc.AddDays(7), null), CancellationToken.None);

        Assert.Contains(db.Notificacoes, n => n.UsuarioId == scenario.ProfessorId && n.Tipo == TipoNotificacao.AulaReagendada && n.ReferenciaId == aula.Id);
    }

    [Fact]
    public async Task Novo_vinculo_de_mentoria_notifica_professor()
    {
        await using var db = CreateDbContext();
        var scenario = SeedMentoringScenario(db);
        await new PlanoAlunoService(db).AssinarAsync(scenario.AlunoId, new AssinarPlanoRequest(PlanoAluno.Mentoria), CancellationToken.None);
        var service = new MentoriaService(db);

        var mentoria = await service.SelecionarMentorAsync(scenario.AlunoId, new SelecionarMentorRequest(scenario.ProfessorId, null), CancellationToken.None);

        Assert.Contains(db.Notificacoes, n => n.UsuarioId == scenario.ProfessorId && n.Tipo == TipoNotificacao.MentoriaIniciada && n.ReferenciaId == mentoria.Id);
    }

    [Fact]
    public async Task Troca_de_mentor_notifica_professores_e_aluno()
    {
        await using var db = CreateDbContext();
        var scenario = SeedMentoringScenario(db);
        await new PlanoAlunoService(db).AssinarAsync(scenario.AlunoId, new AssinarPlanoRequest(PlanoAluno.Mentoria), CancellationToken.None);
        var service = new MentoriaService(db);
        var original = await service.SelecionarMentorAsync(scenario.AlunoId, new SelecionarMentorRequest(scenario.ProfessorId, 1), CancellationToken.None);
        SetLastExchange(db, original.Id, DateTime.UtcNow.AddDays(-31));

        var novo = await service.TrocarMentorAsync(scenario.AlunoId, 1, new TrocarMentorRequest(scenario.OutroProfessorId), CancellationToken.None);

        Assert.Contains(db.Notificacoes, n => n.UsuarioId == scenario.ProfessorId && n.Tipo == TipoNotificacao.MentoriaEncerrada && n.ReferenciaId == original.Id);
        Assert.Contains(db.Notificacoes, n => n.UsuarioId == scenario.OutroProfessorId && n.Tipo == TipoNotificacao.MentoriaIniciada && n.ReferenciaId == novo.Id);
        Assert.Contains(db.Notificacoes, n => n.UsuarioId == scenario.AlunoId && n.Tipo == TipoNotificacao.MentorAlterado && n.ReferenciaId == novo.Id);
    }

    [Fact]
    public async Task Acompanhamento_concluido_notifica_aluno_e_pendente_nao_notifica()
    {
        await using var db = CreateDbContext();
        var scenario = SeedMentoringScenario(db);
        await new PlanoAlunoService(db).AssinarAsync(scenario.AlunoId, new AssinarPlanoRequest(PlanoAluno.Mentoria), CancellationToken.None);
        var service = new MentoriaService(db);
        await service.SelecionarMentorAsync(scenario.AlunoId, new SelecionarMentorRequest(scenario.ProfessorId, null), CancellationToken.None);
        var aula = SeedConfirmedLesson(db, scenario.AlunoId, scenario.ProfessorId, scenario.MateriaId);
        var acompanhamentoId = await service.CriarAcompanhamentoPendenteParaAulaAsync(aula, CancellationToken.None);

        Assert.DoesNotContain(db.Notificacoes, n => n.Tipo == TipoNotificacao.AcompanhamentoMentoriaConcluido && n.ReferenciaId == acompanhamentoId);

        await service.ConcluirAcompanhamentoAsync(
            acompanhamentoId!.Value,
            scenario.ProfessorId,
            new AtualizarAcompanhamentoMentoriaRequest("Resumo da mentoria.", null, null, null, null, null),
            CancellationToken.None);

        Assert.Contains(db.Notificacoes, n => n.UsuarioId == scenario.AlunoId && n.Tipo == TipoNotificacao.AcompanhamentoMentoriaConcluido && n.ReferenciaId == acompanhamentoId);
    }

    [Fact]
    public async Task Gravacao_disponivel_notifica_apenas_aluno_mentoria_dono_da_aula()
    {
        await using var db = CreateDbContext();
        var scenario = SeedRecordingScenario(db);
        var service = new GravacaoAulaService(db);

        var gravacaoMentoria = await service.CriarAsync(scenario.AulaMentoriaId, Guid.NewGuid(), administrador: true, CreateRecordingRequest(), CancellationToken.None);
        await service.CriarAsync(scenario.AulaGratuitoId, Guid.NewGuid(), administrador: true, CreateRecordingRequest("Gratuito"), CancellationToken.None);
        await service.CriarAsync(scenario.AulaFocoId, Guid.NewGuid(), administrador: true, CreateRecordingRequest("Foco"), CancellationToken.None);

        Assert.Contains(db.Notificacoes, n => n.UsuarioId == scenario.AlunoMentoriaId && n.Tipo == TipoNotificacao.GravacaoDisponivel && n.ReferenciaId == gravacaoMentoria.Id);
        Assert.DoesNotContain(db.Notificacoes, n => n.UsuarioId == scenario.AlunoGratuitoId && n.Tipo == TipoNotificacao.GravacaoDisponivel);
        Assert.DoesNotContain(db.Notificacoes, n => n.UsuarioId == scenario.AlunoFocoId && n.Tipo == TipoNotificacao.GravacaoDisponivel);
    }

    [Fact]
    public async Task Responsavel_vinculado_recebe_pagamento_confirmado()
    {
        await using var db = CreateDbContext();
        var scenario = SeedLessonScenario(db);
        var responsavel = new Responsavel("Responsavel", "responsavel.financeiro@teste.local", "hash", "11999999999");
        db.Add(responsavel);
        db.Add(new ResponsavelAluno(responsavel.Id, scenario.AlunoId, "Mae"));
        await db.SaveChangesAsync();
        var aula = await new AulaService(db).SolicitarAsync(scenario.AlunoId, CreateLessonRequest(scenario), CancellationToken.None);
        var financeiro = new FinanceiroService(db);

        var pagamento = await financeiro.PagarAulaAsync(aula.Id, responsavel.Id, FormaPagamento.Pix, CancellationToken.None);

        Assert.Contains(db.Notificacoes, n => n.UsuarioId == responsavel.Id && n.Tipo == TipoNotificacao.PagamentoConfirmado && n.ReferenciaId == pagamento.Id);
    }

    [Fact]
    public async Task Responsavel_nao_vinculado_nao_recebe_notificacao_financeira()
    {
        await using var db = CreateDbContext();
        var scenario = SeedLessonScenario(db);
        var responsavel = new Responsavel("Responsavel", "responsavel.sem.vinculo@teste.local", "hash", "11999999999");
        db.Add(responsavel);
        await db.SaveChangesAsync();
        var aula = await new AulaService(db).SolicitarAsync(scenario.AlunoId, CreateLessonRequest(scenario), CancellationToken.None);
        var financeiro = new FinanceiroService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => financeiro.PagarAulaAsync(aula.Id, responsavel.Id, FormaPagamento.Pix, CancellationToken.None));
        Assert.DoesNotContain(db.Notificacoes, n => n.UsuarioId == responsavel.Id && n.Tipo == TipoNotificacao.PagamentoConfirmado);
    }

    [Fact]
    public async Task Endpoint_sem_jwt_retorna_unauthorized()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/notificacoes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static SolicitarAulaRequest CreateLessonRequest(LessonScenario scenario)
    {
        return new SolicitarAulaRequest(scenario.ProfessorId, scenario.MateriaId, scenario.InicioUtc, 60);
    }

    private static CriarGravacaoAulaRequest CreateRecordingRequest(string suffix = "Mentoria")
    {
        return new CriarGravacaoAulaRequest($"Gravacao - {suffix}", VideoUrl, 1800);
    }

    private static Aluno SeedAluno(TutorClassDbContext db, string nome, string email)
    {
        var aluno = new Aluno(nome, email, "hash");
        db.Add(aluno);
        db.SaveChanges();
        return aluno;
    }

    private static LessonScenario SeedLessonScenario(TutorClassDbContext db)
    {
        var materia = new Materia("Biologia");
        var aluno = new Aluno("Camila", "camila.notificacao@teste.local", "hash");
        var inicioUtc = NextMondayAt14Utc();
        var professor = CreateProfessor("Helena", "helena.notificacao@teste.local", ofereceMentoria: true);
        db.AddRange(materia, aluno, professor);
        db.SaveChanges();
        db.Add(new ProfessorMateria(professor.Id, materia.Id));
        db.Add(new DisponibilidadeProfessor(professor.Id, inicioUtc.DayOfWeek, new TimeOnly(14, 0), new TimeOnly(18, 0)));
        db.Add(CreateApprovedApplication(professor.Id));
        db.SaveChanges();
        return new LessonScenario(aluno.Id, professor.Id, materia.Id, inicioUtc);
    }

    private static MentoringScenario SeedMentoringScenario(TutorClassDbContext db)
    {
        var materia = new Materia("Biologia");
        var aluno = new Aluno("Camila", "camila.mentoria.notificacao@teste.local", "hash");
        var professor = CreateProfessor("Helena", "helena.mentoria.notificacao@teste.local", ofereceMentoria: true);
        var outroProfessor = CreateProfessor("Rafael", "rafael.mentoria.notificacao@teste.local", ofereceMentoria: true);
        db.AddRange(materia, aluno, professor, outroProfessor);
        db.SaveChanges();
        db.AddRange(new ProfessorMateria(professor.Id, materia.Id), new ProfessorMateria(outroProfessor.Id, materia.Id));
        db.SaveChanges();
        return new MentoringScenario(aluno.Id, professor.Id, outroProfessor.Id, materia.Id);
    }

    private static RecordingScenario SeedRecordingScenario(TutorClassDbContext db)
    {
        var materia = new Materia("Biologia");
        var gratuito = new Aluno("Ana", "ana.gravacao.notificacao@teste.local", "hash");
        var foco = new Aluno("Bruno", "bruno.gravacao.notificacao@teste.local", "hash");
        var mentoria = new Aluno("Camila", "camila.gravacao.notificacao@teste.local", "hash");
        var professor = CreateProfessor("Helena", "helena.gravacao.notificacao@teste.local", ofereceMentoria: true);
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
        return new RecordingScenario(gratuito.Id, foco.Id, mentoria.Id, aulaGratuito.Id, aulaFoco.Id, aulaMentoria.Id);
    }

    private static Professor CreateProfessor(string nome, string email, bool ofereceMentoria)
    {
        var professor = new Professor(nome, email, "hash");
        professor.AtualizarPerfilProfissional("Professor para teste de notificacao.", 80m);
        professor.Aprovar();
        professor.ConfigurarMentoria(ofereceMentoria, 4);
        return professor;
    }

    private static CandidaturaProfessor CreateApprovedApplication(Guid professorId)
    {
        var candidatura = new CandidaturaProfessor(professorId, "Teste");
        candidatura.IniciarAnalise(Guid.NewGuid());
        candidatura.Aprovar(Guid.NewGuid());
        return candidatura;
    }

    private static Aula SeedConfirmedLesson(TutorClassDbContext db, Guid alunoId, Guid professorId, Guid materiaId)
    {
        var aula = new Aula(alunoId, professorId, materiaId, DateTime.UtcNow.AddDays(1).AddMinutes(db.Aulas.Count() * 90), 60, 80m);
        db.Add(aula);
        db.SaveChanges();
        aula.Confirmar(professorId);
        db.SaveChanges();
        return aula;
    }

    private static Aula SeedCompletedLesson(TutorClassDbContext db, Guid alunoId, Guid professorId, Guid materiaId)
    {
        var aula = SeedConfirmedLesson(db, alunoId, professorId, materiaId);
        aula.Concluir(professorId);
        db.SaveChanges();
        return aula;
    }

    private static void SetCreatedAt(TutorClassDbContext db, Guid notificacaoId, DateTime value)
    {
        var notificacao = db.Notificacoes.Single(n => n.Id == notificacaoId);
        db.Entry(notificacao).Property(nameof(Notificacao.CreatedAtUtc)).CurrentValue = value;
        db.SaveChanges();
    }

    private static void SetLastExchange(TutorClassDbContext db, Guid mentoriaId, DateTime value)
    {
        var mentoria = db.MentoriasAlunoProfessor.Single(m => m.Id == mentoriaId);
        db.Entry(mentoria).Property(nameof(MentoriaAlunoProfessor.DataUltimaTrocaUtc)).CurrentValue = value;
        db.SaveChanges();
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

    private sealed record LessonScenario(Guid AlunoId, Guid ProfessorId, Guid MateriaId, DateTime InicioUtc);
    private sealed record MentoringScenario(Guid AlunoId, Guid ProfessorId, Guid OutroProfessorId, Guid MateriaId);
    private sealed record RecordingScenario(Guid AlunoGratuitoId, Guid AlunoFocoId, Guid AlunoMentoriaId, Guid AulaGratuitoId, Guid AulaFocoId, Guid AulaMentoriaId);
}
