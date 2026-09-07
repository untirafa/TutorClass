using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TutorClass.Application.Services;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class DevelopmentDataTests
{
    [Fact]
    public async Task Execucao_repetida_nao_duplica_usuarios()
    {
        await using var db = CreateDbContext();
        SeedSubjects(db);
        var service = CreateService(db);

        await service.EnsureTestDataAsync(CancellationToken.None);
        var totalUsuarios = db.Usuarios.Count();
        await service.EnsureTestDataAsync(CancellationToken.None);

        Assert.Equal(totalUsuarios, db.Usuarios.Count());
        Assert.Equal(13, totalUsuarios);
        Assert.DoesNotContain(db.Usuarios, usuario => usuario.Email == "professor.dev.20260824002024929@tutorclass.local");
    }

    [Fact]
    public async Task Massa_de_desenvolvimento_contem_apenas_professores_oficiais()
    {
        await using var db = CreateDbContext();
        SeedSubjects(db);
        await CreateService(db).EnsureTestDataAsync(CancellationToken.None);

        var teacherNames = db.Professores.Select(professor => professor.Nome).OrderBy(nome => nome).ToArray();

        Assert.Equal(
            ["Helena Duarte", "Marcos Vieira", "Patricia Azevedo", "Rafael Moreira", "Sofia Almeida"],
            teacherNames);
    }

    [Fact]
    public async Task Professores_aprovados_aparecem_e_pendentes_nao_aparecem_na_busca_publica()
    {
        await using var db = CreateDbContext();
        SeedSubjects(db);
        var devService = CreateService(db);
        var response = await devService.EnsureTestDataAsync(CancellationToken.None);
        var professorService = new ProfessorService(db);

        var publicTeachers = await professorService.ListarAprovadosAsync(null, CancellationToken.None);

        Assert.Equal(3, publicTeachers.Count);
        Assert.All(response.ProfessoresAprovados, approved =>
            Assert.Contains(publicTeachers, publicTeacher => publicTeacher.Id == approved.Id));
        Assert.All(response.ProfessoresPendentes, pending =>
            Assert.DoesNotContain(publicTeachers, publicTeacher => publicTeacher.Id == pending.Id));
    }

    [Fact]
    public async Task Relacionamento_responsavel_aluno_e_criado_corretamente()
    {
        await using var db = CreateDbContext();
        SeedSubjects(db);
        await CreateService(db).EnsureTestDataAsync(CancellationToken.None);

        Assert.Equal(5, db.ResponsaveisAlunos.Count());
        Assert.Contains(db.ResponsaveisAlunos, link => link.Parentesco == "Responsavel adicional");
    }

    [Fact]
    public async Task Senhas_de_desenvolvimento_sao_armazenadas_como_hash()
    {
        await using var db = CreateDbContext();
        SeedSubjects(db);
        await CreateService(db).EnsureTestDataAsync(CancellationToken.None);

        Assert.All(db.Usuarios, usuario =>
        {
            Assert.NotEqual(DevelopmentDataService.DevelopmentPassword, usuario.PasswordHash);
            Assert.StartsWith("AQAAAA", usuario.PasswordHash);
        });
    }

    private static DevelopmentDataService CreateService(TutorClassDbContext db)
    {
        return new DevelopmentDataService(db, new PasswordHasher<Usuario>());
    }

    private static TutorClassDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TutorClassDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TutorClassDbContext(options);
    }

    private static void SeedSubjects(TutorClassDbContext db)
    {
        var subjects = new[]
        {
            "Portugues",
            "Literatura",
            "Ingles",
            "Espanhol",
            "Artes",
            "Historia",
            "Geografia",
            "Filosofia",
            "Sociologia",
            "Fisica",
            "Quimica",
            "Biologia"
        };

        db.AddRange(subjects.Select(name => new Materia(name)));
        db.SaveChanges();
    }
}
