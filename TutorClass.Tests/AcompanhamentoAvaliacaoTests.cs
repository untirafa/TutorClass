using Microsoft.EntityFrameworkCore;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Common;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class AcompanhamentoAvaliacaoTests
{
    [Fact]
    public async Task Professor_registra_acompanhamento_apos_aula_concluida()
    {
        await using var db = CreateDbContext();
        var cenario = CriarAulaConcluida(db);
        var service = new AcompanhamentoAulaService(db);

        var result = await service.RegistrarOuAtualizarAsync(
            cenario.AulaId,
            cenario.ProfessorId,
            new RegistrarAcompanhamentoAulaRequest("Aluno acompanhou bem a aula.", "Boa participacao", "Revisar fracoes", "Resolver lista 2"),
            CancellationToken.None);

        Assert.Equal(cenario.AulaId, result.AulaId);
        Assert.Equal("Aluno acompanhou bem a aula.", result.Resumo);
    }

    [Fact]
    public async Task Acompanhamento_antes_da_conclusao_e_rejeitado()
    {
        await using var db = CreateDbContext();
        var cenario = CriarCenario(db, concluir: false);
        var service = new AcompanhamentoAulaService(db);

        await Assert.ThrowsAsync<DomainException>(() => service.RegistrarOuAtualizarAsync(
            cenario.AulaId,
            cenario.ProfessorId,
            new RegistrarAcompanhamentoAulaRequest("Resumo", null, null, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task Aluno_avalia_professor_uma_vez_apos_aula_concluida()
    {
        await using var db = CreateDbContext();
        var cenario = CriarAulaConcluida(db);
        var service = new AvaliacaoProfessorService(db);

        var result = await service.AvaliarAsync(cenario.AulaId, cenario.AlunoId, new CriarAvaliacaoProfessorRequest(5, "Otima aula"), CancellationToken.None);

        Assert.Equal(5, result.Nota);
        var professor = await db.Professores.FindAsync(cenario.ProfessorId);
        Assert.Equal(1, professor!.QuantidadeAvaliacoes);
        Assert.Equal(5m, professor.MediaAvaliacoes);
        Assert.Equal(NivelReputacaoProfessor.Verificado, professor.NivelReputacao);
    }

    [Fact]
    public async Task Segunda_avaliacao_da_mesma_aula_e_rejeitada()
    {
        await using var db = CreateDbContext();
        var cenario = CriarAulaConcluida(db);
        var service = new AvaliacaoProfessorService(db);

        await service.AvaliarAsync(cenario.AulaId, cenario.AlunoId, new CriarAvaliacaoProfessorRequest(5, null), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() => service.AvaliarAsync(
            cenario.AulaId,
            cenario.AlunoId,
            new CriarAvaliacaoProfessorRequest(4, null),
            CancellationToken.None));
    }

    [Fact]
    public void Nota_fora_de_um_a_cinco_e_rejeitada()
    {
        Assert.Throws<DomainException>(() => new AvaliacaoProfessor(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 6, null));
    }

    private static Cenario CriarAulaConcluida(TutorClassDbContext db) => CriarCenario(db, concluir: true);

    private static Cenario CriarCenario(TutorClassDbContext db, bool concluir)
    {
        var aluno = new Aluno("Aluno Teste", "aluno.acomp@teste.local", "hash");
        var professor = new Professor("Professor Teste", "prof.acomp@teste.local", "hash");
        var materia = new Materia("Matematica Teste");
        professor.AtualizarPerfilProfissional("Professor de teste", 80m);
        professor.Aprovar();
        db.AddRange(aluno, professor, materia);
        db.SaveChanges();

        var aula = new Aula(aluno.Id, professor.Id, materia.Id, DateTime.UtcNow.AddDays(1), 60, 80m);
        db.Add(aula);
        db.SaveChanges();
        aula.Confirmar(professor.Id);
        if (concluir)
        {
            aula.Concluir(professor.Id);
            professor.RegistrarAulaConcluida();
        }
        db.SaveChanges();

        return new Cenario(aluno.Id, professor.Id, aula.Id);
    }

    private static TutorClassDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TutorClassDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TutorClassDbContext(options);
    }

    private sealed record Cenario(Guid AlunoId, Guid ProfessorId, Guid AulaId);
}
