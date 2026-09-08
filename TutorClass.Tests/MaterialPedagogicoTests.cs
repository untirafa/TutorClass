using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Materials;
using TutorClass.Domain.Notifications;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class MaterialPedagogicoTests
{
    [Fact]
    public async Task Professor_correto_cria_material()
    {
        await using var factory = new TutorClassApiFactory();
        var cenario = await SeedCenarioAsync(factory);
        var client = factory.CreateClient();
        await AuthorizeAsync(client, cenario.ProfessorEmail);

        var response = await client.PostAsJsonAsync("/api/materiais", CriarRequest(cenario));
        var material = await response.Content.ReadFromJsonAsync<MaterialResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(cenario.ProfessorId, material!.ProfessorId);
        Assert.Equal(cenario.AlunoId, material.AlunoId);
        Assert.Equal(cenario.AulaId, material.AulaId);
    }

    [Fact]
    public async Task Material_criado_para_aluno_gera_notificacao_nao_lida_somente_para_aluno_correto()
    {
        await using var factory = new TutorClassApiFactory();
        var cenario = await SeedCenarioAsync(factory);
        var client = factory.CreateClient();
        await AuthorizeAsync(client, cenario.ProfessorEmail);

        var response = await client.PostAsJsonAsync("/api/materiais", CriarRequest(cenario));
        var material = await response.Content.ReadFromJsonAsync<MaterialResponse>();

        response.EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
        var notificacao = Assert.Single(db.Notificacoes.Where(n =>
            n.UsuarioId == cenario.AlunoId &&
            n.Tipo == TipoNotificacao.NovoMaterial &&
            n.ReferenciaId == material!.Id));

        Assert.False(notificacao.Lida);
        Assert.Equal("/materiais", notificacao.RotaAcao);
        var chaveEvento = $"NOVO_MATERIAL:{material!.Id}:{cenario.AlunoId}".ToUpperInvariant();
        Assert.Equal(chaveEvento, notificacao.ChaveEvento);
        Assert.DoesNotContain(db.Notificacoes, n => n.UsuarioId == cenario.OutroAlunoId && n.Tipo == TipoNotificacao.NovoMaterial);
        Assert.DoesNotContain(db.Notificacoes, n => n.UsuarioId == cenario.ResponsavelId && n.Tipo == TipoNotificacao.NovoMaterial);

        var alunoClient = factory.CreateClient();
        await AuthorizeAsync(alunoClient, cenario.AlunoEmail);
        var resumo = await alunoClient.GetFromJsonAsync<NotificacaoResumoResponse>("/api/notificacoes/resumo");

        Assert.Equal(1, resumo!.NaoLidas);
    }

    [Fact]
    public async Task Chave_evento_de_novo_material_e_idempotente()
    {
        await using var factory = new TutorClassApiFactory();
        var cenario = await SeedCenarioAsync(factory);
        var client = factory.CreateClient();
        await AuthorizeAsync(client, cenario.ProfessorEmail);

        var response = await client.PostAsJsonAsync("/api/materiais", CriarRequest(cenario));
        var material = await response.Content.ReadFromJsonAsync<MaterialResponse>();
        response.EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
        var service = new NotificacaoService(db);
        var chave = $"NOVO_MATERIAL:{material!.Id}:{cenario.AlunoId}";
        var chaveNormalizada = chave.ToUpperInvariant();

        service.CriarSeNaoExistir(
            cenario.AlunoId,
            TipoNotificacao.NovoMaterial,
            "Novo material disponivel",
            "Mensagem repetida.",
            "/materiais",
            material.Id,
            chave);

        await db.SaveChangesAsync();

        Assert.Single(db.Notificacoes.Where(n => n.ChaveEvento == chaveNormalizada));
    }

    [Fact]
    public async Task Material_com_criacao_falha_nao_gera_notificacao()
    {
        await using var factory = new TutorClassApiFactory();
        var cenario = await SeedCenarioAsync(factory);
        var client = factory.CreateClient();
        await AuthorizeAsync(client, cenario.OutroProfessorEmail);

        var response = await client.PostAsJsonAsync("/api/materiais", CriarRequest(cenario));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
        Assert.DoesNotContain(db.Notificacoes, n => n.Tipo == TipoNotificacao.NovoMaterial);
    }

    [Fact]
    public async Task Professor_sem_relacao_com_aluno_nao_cria_material()
    {
        await using var factory = new TutorClassApiFactory();
        var cenario = await SeedCenarioAsync(factory);
        var client = factory.CreateClient();
        await AuthorizeAsync(client, cenario.OutroProfessorEmail);

        var response = await client.PostAsJsonAsync("/api/materiais", CriarRequest(cenario) with { AulaId = null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Outro_professor_nao_edita_material()
    {
        await using var factory = new TutorClassApiFactory();
        var cenario = await SeedCenarioAsync(factory);
        var materialId = await SeedMaterialAsync(factory, cenario);
        var client = factory.CreateClient();
        await AuthorizeAsync(client, cenario.OutroProfessorEmail);

        var response = await client.PutAsJsonAsync(
            $"/api/materiais/{materialId}",
            new AtualizarMaterialRequest("Lista atualizada", null, "PDF", "lista.pdf", "https://example.com/lista.pdf"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Outro_professor_nao_exclui_material()
    {
        await using var factory = new TutorClassApiFactory();
        var cenario = await SeedCenarioAsync(factory);
        var materialId = await SeedMaterialAsync(factory, cenario);
        var client = factory.CreateClient();
        await AuthorizeAsync(client, cenario.OutroProfessorEmail);

        var response = await client.DeleteAsync($"/api/materiais/{materialId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Aluno_correto_consulta_material()
    {
        await using var factory = new TutorClassApiFactory();
        var cenario = await SeedCenarioAsync(factory);
        var materialId = await SeedMaterialAsync(factory, cenario);
        var client = factory.CreateClient();
        await AuthorizeAsync(client, cenario.AlunoEmail);

        var response = await client.GetAsync($"/api/materiais/{materialId}");
        var material = await response.Content.ReadFromJsonAsync<MaterialResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(cenario.AlunoId, material!.AlunoId);
    }

    [Fact]
    public async Task Outro_aluno_nao_consulta_material()
    {
        await using var factory = new TutorClassApiFactory();
        var cenario = await SeedCenarioAsync(factory);
        var materialId = await SeedMaterialAsync(factory, cenario);
        var client = factory.CreateClient();
        await AuthorizeAsync(client, cenario.OutroAlunoEmail);

        var response = await client.GetAsync($"/api/materiais/{materialId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AulaId_invalido_ou_de_outra_relacao_e_rejeitado()
    {
        await using var factory = new TutorClassApiFactory();
        var cenario = await SeedCenarioAsync(factory);
        var client = factory.CreateClient();
        await AuthorizeAsync(client, cenario.OutroProfessorEmail);

        var response = await client.PostAsJsonAsync("/api/materiais", CriarRequest(cenario));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Titulo_obrigatorio_e_rejeitado()
    {
        await using var factory = new TutorClassApiFactory();
        var cenario = await SeedCenarioAsync(factory);
        var client = factory.CreateClient();
        await AuthorizeAsync(client, cenario.ProfessorEmail);

        var response = await client.PostAsJsonAsync("/api/materiais", CriarRequest(cenario) with { Titulo = " " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Autorizacao_dos_endpoints_de_material_funciona()
    {
        await using var factory = new TutorClassApiFactory();
        var cenario = await SeedCenarioAsync(factory);

        var anonymousClient = factory.CreateClient();
        var anonymousResponse = await anonymousClient.PostAsJsonAsync("/api/materiais", CriarRequest(cenario));

        var alunoClient = factory.CreateClient();
        await AuthorizeAsync(alunoClient, cenario.AlunoEmail);
        var alunoResponse = await alunoClient.PostAsJsonAsync("/api/materiais", CriarRequest(cenario));

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, alunoResponse.StatusCode);
    }

    private static CriarMaterialRequest CriarRequest(CenarioMaterial cenario) => new(
        cenario.AlunoId,
        cenario.AulaId,
        cenario.MateriaId,
        "Lista de exercicios",
        "Exercicios de fixacao.",
        "PDF",
        "lista.pdf",
        "https://example.com/materiais/lista.pdf");

    private static async Task<Guid> SeedMaterialAsync(TutorClassApiFactory factory, CenarioMaterial cenario)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
        var material = new MaterialPedagogico(
            cenario.ProfessorId,
            cenario.AlunoId,
            cenario.MateriaId,
            cenario.AulaId,
            "Material inicial",
            null,
            "PDF",
            "material.pdf",
            "https://example.com/material.pdf");

        db.Add(material);
        await db.SaveChangesAsync();
        return material.Id;
    }

    private static async Task<CenarioMaterial> SeedCenarioAsync(TutorClassApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();

        var professor = CreateProfessor("Professor Material", "prof.material@teste.local", hasher);
        var outroProfessor = CreateProfessor("Outro Professor", "outro.prof.material@teste.local", hasher);
        var aluno = CreateAluno("Aluno Material", "aluno.material@teste.local", hasher);
        var outroAluno = CreateAluno("Outro Aluno", "outro.aluno.material@teste.local", hasher);
        var responsavel = CreateResponsavel("Responsavel Material", "responsavel.material@teste.local", hasher);
        var materia = new Materia("Matematica Material");

        db.AddRange(professor, outroProfessor, aluno, outroAluno, responsavel, materia);
        await db.SaveChangesAsync();

        db.Add(new ResponsavelAluno(responsavel.Id, aluno.Id, "Responsavel financeiro"));
        await db.SaveChangesAsync();

        var aula = new Aula(aluno.Id, professor.Id, materia.Id, DateTime.UtcNow.AddDays(1), 60, 80m);
        db.Add(aula);
        await db.SaveChangesAsync();

        return new CenarioMaterial(
            professor.Id,
            professor.Email,
            outroProfessor.Id,
            outroProfessor.Email,
            aluno.Id,
            aluno.Email,
            outroAluno.Id,
            outroAluno.Email,
            responsavel.Id,
            materia.Id,
            aula.Id);
    }

    private static Professor CreateProfessor(string nome, string email, IPasswordHasher<Usuario> hasher)
    {
        var professor = new Professor(nome, email, "pending");
        professor.DefinirPasswordHash(hasher.HashPassword(professor, "TutorClass@123"));
        professor.AtualizarPerfilProfissional("Professor para testes de materiais.", 80);
        professor.Aprovar();
        return professor;
    }

    private static Aluno CreateAluno(string nome, string email, IPasswordHasher<Usuario> hasher)
    {
        var aluno = new Aluno(nome, email, "pending");
        aluno.DefinirPasswordHash(hasher.HashPassword(aluno, "TutorClass@123"));
        return aluno;
    }

    private static Responsavel CreateResponsavel(string nome, string email, IPasswordHasher<Usuario> hasher)
    {
        var responsavel = new Responsavel(nome, email, "pending");
        responsavel.DefinirPasswordHash(hasher.HashPassword(responsavel, "TutorClass@123"));
        return responsavel;
    }

    private static async Task AuthorizeAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "TutorClass@123"));
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
    }

    private sealed record CenarioMaterial(
        Guid ProfessorId,
        string ProfessorEmail,
        Guid OutroProfessorId,
        string OutroProfessorEmail,
        Guid AlunoId,
        string AlunoEmail,
        Guid OutroAlunoId,
        string OutroAlunoEmail,
        Guid ResponsavelId,
        Guid MateriaId,
        Guid AulaId);
}
