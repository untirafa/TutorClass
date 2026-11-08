using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Help;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class FaqTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Lista_apenas_faqs_ativos()
    {
        await using var db = CreateDbContext();
        var inactive = SeedFaqs(db).Inactive;
        var service = new FaqService(db);

        var faqs = await service.ListarAsync(null, null, CancellationToken.None);

        Assert.DoesNotContain(faqs, faq => faq.Id == inactive);
        Assert.All(faqs, faq => Assert.True(faq.Ativo));
    }

    [Fact]
    public async Task Faq_desativado_nao_aparece()
    {
        await using var db = CreateDbContext();
        var seeded = SeedFaqs(db);
        var service = new FaqService(db);

        await service.DesativarAsync(seeded.CancelLesson, CancellationToken.None);
        var faqs = await service.ListarAsync(null, null, CancellationToken.None);

        Assert.DoesNotContain(faqs, faq => faq.Id == seeded.CancelLesson);
    }

    [Fact]
    public async Task Faq_excluido_e_removido_definitivamente()
    {
        await using var db = CreateDbContext();
        var seeded = SeedFaqs(db);
        var service = new FaqService(db);

        await service.ExcluirAsync(seeded.CancelLesson, CancellationToken.None);

        Assert.False(db.PerguntasFrequentes.Any(f => f.Id == seeded.CancelLesson));
        await Assert.ThrowsAsync<TutorClass.Domain.Common.DomainException>(() => service.ExcluirAsync(seeded.CancelLesson, CancellationToken.None));
    }

    [Fact]
    public async Task Ordem_e_respeitada()
    {
        await using var db = CreateDbContext();
        SeedFaqs(db);
        var service = new FaqService(db);

        var faqs = await service.ListarAsync("Aulas", null, CancellationToken.None);

        Assert.Equal(["Como cancelar uma aula?", "Como funciona o agendamento?"], faqs.Select(f => f.Pergunta));
    }

    [Fact]
    public async Task Filtro_por_categoria_funciona()
    {
        await using var db = CreateDbContext();
        SeedFaqs(db);
        var service = new FaqService(db);

        var faqs = await service.ListarAsync("Mentoria", null, CancellationToken.None);

        Assert.All(faqs, faq => Assert.Equal(CategoriaFaq.Mentoria, faq.Categoria));
        Assert.Contains(faqs, faq => faq.Pergunta == "Quantos mentores posso ter?");
    }

    [Fact]
    public async Task Categoria_inexistente_retorna_lista_vazia()
    {
        await using var db = CreateDbContext();
        SeedFaqs(db);
        var service = new FaqService(db);

        var faqs = await service.ListarAsync("Inexistente", null, CancellationToken.None);

        Assert.Empty(faqs);
    }

    [Fact]
    public async Task Busca_por_termo_funciona()
    {
        await using var db = CreateDbContext();
        SeedFaqs(db);
        var service = new FaqService(db);

        var faqs = await service.ListarAsync(null, "cancelar", CancellationToken.None);

        Assert.Contains(faqs, faq => faq.Pergunta == "Como cancelar uma aula?");
    }

    [Fact]
    public async Task Busca_por_palavra_chave_funciona()
    {
        await using var db = CreateDbContext();
        SeedFaqs(db);
        var service = new FaqService(db);

        var faqs = await service.ListarAsync(null, "desistir", CancellationToken.None);

        Assert.Single(faqs);
        Assert.Equal("Como cancelar uma aula?", faqs.Single().Pergunta);
    }

    [Fact]
    public async Task Busca_e_case_insensitive()
    {
        await using var db = CreateDbContext();
        SeedFaqs(db);
        var service = new FaqService(db);

        var faqs = await service.ListarAsync(null, "GrAvAcOeS", CancellationToken.None);

        Assert.Contains(faqs, faq => faq.Categoria == CategoriaFaq.Gravacoes);
    }

    [Fact]
    public async Task Chat_pergunta_exata_retorna_resposta_correta()
    {
        await using var db = CreateDbContext();
        SeedFaqs(db);
        var service = new FaqService(db);

        var response = await service.ConversarAsync(new FaqChatRequest("Como cancelar uma aula?"), CancellationToken.None);

        Assert.True(response.Encontrado);
        Assert.Equal("Como cancelar uma aula?", response.Pergunta);
    }

    [Fact]
    public async Task Chat_palavra_chave_retorna_faq_correspondente()
    {
        await using var db = CreateDbContext();
        SeedFaqs(db);
        var service = new FaqService(db);

        var response = await service.ConversarAsync(new FaqChatRequest("Quero desistir da aula"), CancellationToken.None);

        Assert.True(response.Encontrado);
        Assert.Equal("Como cancelar uma aula?", response.Pergunta);
    }

    [Fact]
    public async Task Chat_termo_aproximado_retorna_resposta_coerente()
    {
        await using var db = CreateDbContext();
        SeedFaqs(db);
        var service = new FaqService(db);

        var response = await service.ConversarAsync(new FaqChatRequest("Quantos professores posso ter na Mentoria?"), CancellationToken.None);

        Assert.True(response.Encontrado);
        Assert.Equal("Quantos mentores posso ter?", response.Pergunta);
    }

    [Fact]
    public async Task Chat_sem_correspondencia_retorna_fallback()
    {
        await using var db = CreateDbContext();
        SeedFaqs(db);
        var service = new FaqService(db);

        var response = await service.ConversarAsync(new FaqChatRequest("Voces vendem computadores?"), CancellationToken.None);

        Assert.False(response.Encontrado);
        Assert.Contains("Nao encontrei", response.Mensagem);
    }

    [Fact]
    public async Task Faq_desativado_nao_pode_ser_escolhido_pelo_chatbot()
    {
        await using var db = CreateDbContext();
        var seeded = SeedFaqs(db);
        var service = new FaqService(db);
        await service.DesativarAsync(seeded.CancelLesson, CancellationToken.None);

        var response = await service.ConversarAsync(new FaqChatRequest("Como cancelar uma aula?"), CancellationToken.None);

        Assert.False(response.Encontrado);
    }

    [Fact]
    public async Task Administrador_cria_edita_e_desativa_faq()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        await SeedUserAsync(factory, new Administrador("Admin FAQ", "admin.faq@teste.local", "pending"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "admin.faq@teste.local"));

        var create = await client.PostAsJsonAsync("/api/admin/faq", CreateRequest("Como vejo minhas aulas?", 10));
        var created = await create.Content.ReadFromJsonAsync<FaqResponse>(JsonOptions);
        var update = await client.PutAsJsonAsync(
            $"/api/admin/faq/{created!.Id}",
            new AtualizarFaqRequest("Como vejo minhas aulas agendadas?", "Acesse a area Minhas aulas.", CategoriaFaq.Aulas, "minhas aulas,agenda", 11, "Ver minhas aulas", "/minhas-aulas", true));
        var updated = await update.Content.ReadFromJsonAsync<FaqResponse>(JsonOptions);
        var deactivate = await client.PostAsync($"/api/admin/faq/{created.Id}/desativar", null);

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Como vejo minhas aulas agendadas?", updated!.Pergunta);
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);
    }

    [Fact]
    public async Task Aluno_nao_pode_criar_faq()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        await SeedUserAsync(factory, new Aluno("Aluno FAQ", "aluno.faq@teste.local", "pending"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "aluno.faq@teste.local"));

        var response = await client.PostAsJsonAsync("/api/admin/faq", CreateRequest("Pergunta aluno", 10));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Professor_nao_pode_editar_faq()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        var faqId = await SeedFaqAsync(factory);
        var professor = new TutorClass.Domain.Teachers.Professor("Professor FAQ", "professor.faq@teste.local", "pending");
        await SeedUserAsync(factory, professor);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "professor.faq@teste.local"));

        var response = await client.PutAsJsonAsync(
            $"/api/admin/faq/{faqId}",
            new AtualizarFaqRequest("Editada", "Resposta editada.", CategoriaFaq.Aulas, null, 1, null, null, true));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Usuario_nao_autorizado_e_bloqueado_no_admin()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/admin/faq", CreateRequest("Pergunta sem login", 10));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Seed_de_desenvolvimento_nao_duplica_faqs()
    {
        await using var db = CreateDbContext();
        SeedSubjects(db);
        var service = new DevelopmentDataService(db, new PasswordHasher<Usuario>());

        await service.EnsureTestDataAsync(CancellationToken.None);
        var totalFaqs = db.PerguntasFrequentes.Count();
        await service.EnsureTestDataAsync(CancellationToken.None);

        Assert.Equal(totalFaqs, db.PerguntasFrequentes.Count());
        Assert.True(totalFaqs >= 10);
    }

    private static CriarFaqRequest CreateRequest(string pergunta, int ordem)
    {
        return new CriarFaqRequest(pergunta, "Resposta administrativa de teste.", CategoriaFaq.Aulas, "teste,aula", ordem, "Ver aulas", "/minhas-aulas");
    }

    private static async Task<Guid> SeedFaqAsync(TutorClassApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
        var faq = new PerguntaFrequente("FAQ para editar", "Resposta.", CategoriaFaq.Aulas, "editar", 1);
        db.Add(faq);
        await db.SaveChangesAsync();
        return faq.Id;
    }

    private static async Task SeedUserAsync(TutorClassApiFactory factory, Usuario user)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
        user.DefinirPasswordHash(hasher.HashPassword(user, "TutorClass@123"));
        db.Add(user);
        await db.SaveChangesAsync();
    }

    private static async Task<string> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "TutorClass@123"));
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return login!.AccessToken;
    }

    private static SeededFaqs SeedFaqs(TutorClassDbContext db)
    {
        var cancelLesson = new PerguntaFrequente(
            "Como cancelar uma aula?",
            "Voce pode cancelar uma aula pela area de suas aulas.",
            CategoriaFaq.Aulas,
            "cancelar,cancelamento,cancelo,desistir,aula",
            10);

        var scheduling = new PerguntaFrequente(
            "Como funciona o agendamento?",
            "O aluno escolhe professor aprovado e horario disponivel.",
            CategoriaFaq.Aulas,
            "agendar,agendamento,horario",
            20);

        var mentors = new PerguntaFrequente(
            "Quantos mentores posso ter?",
            "O plano Mentoria permite ate 2 professores mentores ativos ao mesmo tempo.",
            CategoriaFaq.Mentoria,
            "mentor,mentores,mentoria,professor,professores,quantos",
            10);

        var recordings = new PerguntaFrequente(
            "Quem pode acessar gravacoes?",
            "Alunos Mentoria podem rever gravacoes das proprias aulas concluidas.",
            CategoriaFaq.Gravacoes,
            "gravacao,gravacoes,video,rever,mentoria",
            10);

        var inactive = new PerguntaFrequente(
            "FAQ desativada",
            "Esta resposta nao deve aparecer.",
            CategoriaFaq.Conta,
            "desativada",
            1);
        inactive.Desativar();

        db.AddRange(cancelLesson, scheduling, mentors, recordings, inactive);
        db.SaveChanges();
        return new SeededFaqs(cancelLesson.Id, inactive.Id);
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

    private sealed record SeededFaqs(Guid CancelLesson, Guid Inactive);
}
