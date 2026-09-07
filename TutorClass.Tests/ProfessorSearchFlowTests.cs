using Microsoft.EntityFrameworkCore;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class ProfessorSearchFlowTests
{
    [Fact]
    public async Task Professor_pendente_nao_aparece_na_busca_publica()
    {
        await using var db = CreateDbContext();
        var materia = new Materia("Biologia");
        var professor = CreateProfessor("pendente@teste.local");
        professor.VincularMateria(materia.Id);
        professor.SolicitarCandidatura("Solicitacao pendente.");
        db.AddRange(materia, professor);
        await db.SaveChangesAsync();

        var service = new ProfessorService(db);

        var professores = await service.ListarAprovadosAsync(null, CancellationToken.None);

        Assert.Empty(professores);
    }

    [Fact]
    public async Task Professor_aprovado_aparece_na_busca_publica()
    {
        await using var db = CreateDbContext();
        var materia = new Materia("Biologia");
        var admin = new Administrador("Admin", "admin@teste.local", "hash");
        var professor = CreateProfessor("aprovado@teste.local");
        professor.VincularMateria(materia.Id);
        var qualificacao = professor.AdicionarQualificacao(
            TipoQualificacaoProfessor.Certificacao,
            "Certificacao verificada",
            "Instituicao Teste",
            null,
            null,
            null);
        qualificacao.Verificar();
        var candidatura = professor.SolicitarCandidatura("Solicitacao valida.");
        candidatura.IniciarAnalise(admin.Id);
        candidatura.Aprovar(admin.Id);
        professor.Aprovar();
        db.AddRange(materia, admin, professor);
        await db.SaveChangesAsync();

        var service = new ProfessorService(db);

        var professores = await service.ListarAprovadosAsync(null, CancellationToken.None);

        Assert.Single(professores);
        Assert.Equal(professor.Id, professores.Single().Id);
    }

    [Fact]
    public async Task Professor_publico_retorna_materias_qualificacoes_e_disponibilidades()
    {
        await using var db = CreateDbContext();
        var materia = new Materia("Biologia");
        var admin = new Administrador("Admin", "admin.perfil.publico@teste.local", "hash");
        var professor = CreateProfessor("perfil.publico@teste.local");
        professor.VincularMateria(materia.Id);
        professor.CadastrarDisponibilidade(DayOfWeek.Monday, new TimeOnly(14, 0), new TimeOnly(16, 0));
        var qualificacao = professor.AdicionarQualificacao(
            TipoQualificacaoProfessor.Certificacao,
            "Licenciatura verificada",
            "Instituicao Teste",
            null,
            null,
            "https://example.com/documento-privado.pdf");
        qualificacao.Verificar();
        var candidatura = professor.SolicitarCandidatura("Solicitacao valida.");
        candidatura.IniciarAnalise(admin.Id);
        candidatura.Aprovar(admin.Id);
        professor.Aprovar();
        db.AddRange(materia, admin, professor);
        await db.SaveChangesAsync();

        var service = new ProfessorService(db);

        var resumo = (await service.ListarAprovadosAsync(null, CancellationToken.None)).Single();
        var detalhes = await service.BuscarPublicoPorIdAsync(professor.Id, CancellationToken.None);

        Assert.Contains("Biologia", resumo.Materias);
        Assert.NotNull(detalhes);
        Assert.Contains("Biologia", detalhes.Materias);
        Assert.Single(detalhes.Qualificacoes);
        Assert.Null(detalhes.Qualificacoes.Single().DocumentoComprovacao);
        Assert.Single(detalhes.Disponibilidades);
    }

    [Fact]
    public async Task Qualificacao_fica_vinculada_ao_professor_correto()
    {
        await using var db = CreateDbContext();
        var professor = CreateProfessor("qualificacao@teste.local");
        db.Add(professor);
        await db.SaveChangesAsync();
        var service = new ProfessorService(db);

        var qualificacao = await service.AdicionarQualificacaoAsync(
            professor.Id,
            new AdicionarQualificacaoProfessorRequest(
                TipoQualificacaoProfessor.Certificacao,
                "Certificacao de idioma",
                "Instituicao Teste",
                "Evidencia temporaria para teste.",
                new DateOnly(2025, 5, 10),
                "https://example.com/certificado.pdf"),
            CancellationToken.None);

        Assert.Equal(professor.Id, qualificacao.ProfessorId);
        Assert.Equal(StatusVerificacaoQualificacao.Pendente, qualificacao.StatusVerificacao);
    }

    [Fact]
    public async Task Candidatura_nao_pode_ser_aprovada_sem_qualificacao_verificada()
    {
        await using var db = CreateDbContext();
        var materia = new Materia("Biologia");
        var admin = new Administrador("Admin", "admin.sem.qualificacao@teste.local", "hash");
        var professor = CreateProfessor("sem.qualificacao.verificada@teste.local");
        db.AddRange(materia, admin, professor);
        await db.SaveChangesAsync();

        var professorService = new ProfessorService(db);
        await professorService.VincularMateriaAsync(professor.Id, new VincularMateriaProfessorRequest(materia.Id), CancellationToken.None);
        await professorService.AdicionarQualificacaoAsync(
            professor.Id,
            new AdicionarQualificacaoProfessorRequest(
                TipoQualificacaoProfessor.Certificacao,
                "Certificacao pendente",
                "Instituicao Teste",
                null,
                null,
                null),
            CancellationToken.None);
        var candidaturaService = new CandidaturaProfessorService(db);
        var candidaturaId = await candidaturaService.EnviarAsync(professor.Id, new EnviarCandidaturaProfessorRequest("Solicitacao valida."), CancellationToken.None);
        await candidaturaService.IniciarAnaliseAsync(candidaturaId, admin.Id, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<TutorClass.Domain.Common.DomainException>(() =>
            candidaturaService.AprovarAsync(candidaturaId, admin.Id, CancellationToken.None));

        Assert.Equal("Professor precisa possuir pelo menos uma qualificacao verificada para ser aprovado.", exception.Message);
    }

    [Fact]
    public async Task Candidatura_pode_ser_aprovada_com_materia_e_qualificacao_verificada()
    {
        await using var db = CreateDbContext();
        var materia = new Materia("Biologia");
        var admin = new Administrador("Admin", "admin.com.qualificacao@teste.local", "hash");
        var professor = CreateProfessor("com.qualificacao.verificada@teste.local");
        db.AddRange(materia, admin, professor);
        await db.SaveChangesAsync();

        var professorService = new ProfessorService(db);
        await professorService.VincularMateriaAsync(professor.Id, new VincularMateriaProfessorRequest(materia.Id), CancellationToken.None);
        var qualificacao = await professorService.AdicionarQualificacaoAsync(
            professor.Id,
            new AdicionarQualificacaoProfessorRequest(
                TipoQualificacaoProfessor.Certificacao,
                "Certificacao verificada",
                "Instituicao Teste",
                null,
                null,
                null),
            CancellationToken.None);
        var qualificacaoService = new QualificacaoProfessorService(db);
        await qualificacaoService.VerificarAsync(qualificacao.Id, admin.Id, CancellationToken.None);
        var candidaturaService = new CandidaturaProfessorService(db);
        var candidaturaId = await candidaturaService.EnviarAsync(professor.Id, new EnviarCandidaturaProfessorRequest("Solicitacao valida."), CancellationToken.None);
        await candidaturaService.IniciarAnaliseAsync(candidaturaId, admin.Id, CancellationToken.None);

        await candidaturaService.AprovarAsync(candidaturaId, admin.Id, CancellationToken.None);

        var professores = await professorService.ListarAprovadosAsync(null, CancellationToken.None);
        Assert.Single(professores);
        Assert.Equal(professor.Id, professores.Single().Id);
    }

    private static TutorClassDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TutorClassDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TutorClassDbContext(options);
    }

    private static Professor CreateProfessor(string email)
    {
        var professor = new Professor("Professor Teste", email, "hash");
        professor.AtualizarPerfilProfissional("Professor usado em teste automatizado.", 80);
        return professor;
    }
}
