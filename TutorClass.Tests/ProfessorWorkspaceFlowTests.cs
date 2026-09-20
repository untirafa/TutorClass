using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Teachers;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class ProfessorWorkspaceFlowTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Candidato_consulta_documentos_privados_e_edita_perfil_sem_publicar_nem_mudar_aprovacao()
    {
        await using var factory = new TutorClassApiFactory();
        var seeded = await factory.SeedPrivateProfessorDataAsync();
        using var client = factory.CreateClient();
        await AuthorizeAsync(client, "professor.privado@teste.local");

        var profile = await client.GetFromJsonAsync<ProfessorPerfilPrivadoDto>("/api/professores/me/perfil", JsonOptions);
        Assert.Equal(seeded.ProfessorId, profile!.Id);
        Assert.Equal(StatusProfessor.Candidato, profile.Status);
        Assert.Equal("https://example.com/privado.pdf", Assert.Single(profile.Qualificacoes).DocumentoComprovacao);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/professores/{seeded.ProfessorId}")).StatusCode);

        var updated = await client.PutAsJsonAsync("/api/professores/me/perfil",
            new AtualizarPerfilProfessorRequest("Nova biografia para aulas particulares.", 125.50m));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var reread = await client.GetFromJsonAsync<ProfessorPerfilPrivadoDto>("/api/professores/me/perfil", JsonOptions);
        Assert.Equal("Nova biografia para aulas particulares.", reread!.Biografia);
        Assert.Equal(125.50m, reread.PrecoHora);
        Assert.Equal(StatusProfessor.Candidato, reread.Status);

        var invalid = await client.PutAsJsonAsync("/api/professores/me/perfil",
            new AtualizarPerfilProfessorRequest("Biografia invalida pelo preco.", 0));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var afterInvalid = await client.GetFromJsonAsync<ProfessorPerfilPrivadoDto>("/api/professores/me/perfil", JsonOptions);
        Assert.Equal(reread.Biografia, afterInvalid!.Biografia);
        Assert.Equal(reread.PrecoHora, afterInvalid.PrecoHora);
    }

    [Fact]
    public async Task Candidaturas_sao_recuperadas_do_banco_por_titular_e_duplicadas_ativas_sao_recusadas()
    {
        await using var factory = new TutorClassApiFactory();
        var firstId = await factory.SeedProfessorAsync("primeiro.workspace@teste.local");
        var otherId = await factory.SeedProfessorAsync("outro.workspace@teste.local");
        using var first = factory.CreateClient();
        using var other = factory.CreateClient();
        await AuthorizeAsync(first, "primeiro.workspace@teste.local");
        await AuthorizeAsync(other, "outro.workspace@teste.local");

        var sent = await first.PostAsJsonAsync($"/api/professores/{firstId}/candidaturas",
            new EnviarCandidaturaProfessorRequest("Minha candidatura original."));
        Assert.Equal(HttpStatusCode.Created, sent.StatusCode);
        var candidatureId = await sent.Content.ReadFromJsonAsync<Guid>();
        var duplicate = await first.PostAsJsonAsync($"/api/professores/{firstId}/candidaturas",
            new EnviarCandidaturaProfessorRequest("Tentativa em nova requisicao e novo contexto."));
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);

        var otherSent = await other.PostAsJsonAsync($"/api/professores/{otherId}/candidaturas",
            new EnviarCandidaturaProfessorRequest("Candidatura de outro professor."));
        Assert.Equal(HttpStatusCode.Created, otherSent.StatusCode);
        var mine = await first.GetFromJsonAsync<CandidaturaProfessorDto[]>("/api/professores/me/candidaturas", JsonOptions);
        var candidature = Assert.Single(mine!);
        Assert.Equal(candidatureId, candidature.Id);
        Assert.Equal(firstId, candidature.ProfessorId);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/candidaturas-professor/{candidatureId}")).StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
        Assert.Single(db.CandidaturasProfessor.Where(c => c.ProfessorId == firstId));
    }

    [Fact]
    public async Task Fila_administrativa_exige_administrador_e_expoe_dados_necessarios_para_analise()
    {
        await using var factory = new TutorClassApiFactory();
        await factory.SeedAdminProfessorAndQualificationAsync();
        var seeded = await factory.SeedPrivateProfessorDataAsync();
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/candidaturas-professor")).StatusCode);
        await AuthorizeAsync(client, "professor.privado@teste.local");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/candidaturas-professor")).StatusCode);

        await AuthorizeAsync(client, "admin@teste.local");
        var queue = await client.GetFromJsonAsync<CandidaturaProfessorAdminDto[]>("/api/admin/candidaturas-professor", JsonOptions);
        var candidature = Assert.Single(queue!);
        Assert.Equal(seeded.CandidaturaId, candidature.Id);
        Assert.Equal("Professor Privado", candidature.ProfessorNome);
        Assert.Equal("professor.privado@teste.local", candidature.ProfessorEmail);
        Assert.Equal(StatusCandidaturaProfessor.Pendente, candidature.Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/professores/me/perfil")).StatusCode);
    }

    private static async Task AuthorizeAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "TutorClass@123"));
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
    }
}
