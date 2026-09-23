using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorClass.Domain.Plans;
using TutorClass.Infrastructure.Persistence;
using TutorClass.API.Controllers;
using TutorClass.API.Security;
using TutorClass.Application.DTOs;

namespace TutorClass.Tests;

public class SegurancaContratosTests
{
    [Fact]
    public async Task Moderacao_preserva_json_e_sse_e_rate_limit_nao_consume_quota()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/registrar/aluno", new RegistrarAlunoRequest("Teste", "moderacao@example.test", "Senha-ficticia123!", null));
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("moderacao@example.test", "Senha-ficticia123!"));
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
            db.Add(new AssinaturaAluno(login.Usuario.Id, login.Usuario.Id, PlanoAluno.Foco,30,20,true)); await db.SaveChangesAsync();
        }
        for (var i=0;i<12;i++)
        {
            var streaming = i % 2 == 0;
            var response = await client.PostAsJsonAsync("/api/tutoria/chat" + (streaming ? "/stream" : ""), new TutorIaChatRequest("Meu CPF é 12345678909"));
            Assert.Equal(HttpStatusCode.OK,response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("DADOS_PESSOAIS",body); Assert.DoesNotContain("12345678909",body);
            Assert.DoesNotContain("event: delta",body);
            if(streaming) Assert.Contains("event: complete",body);
        }
        var limited = await client.PostAsJsonAsync("/api/tutoria/chat",new TutorIaChatRequest("oi"));
        Assert.Equal(HttpStatusCode.TooManyRequests,limited.StatusCode);
        using var verification = factory.Services.CreateScope();
        Assert.Empty(verification.ServiceProvider.GetRequiredService<TutorClassDbContext>().UsosIaAluno);
    }

    [Fact]
    public async Task Documentos_publicos_vazios_nao_exigem_aceite()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        var result = await client.GetFromJsonAsync<PrivacidadeResponse>("/api/privacidade/documentos");
        Assert.NotNull(result); Assert.Empty(result.Documentos); Assert.False(result.AceiteObrigatorio);
        Assert.Null(result.ContatoPrivacidade); Assert.Null(result.Orientacoes);
    }

    [Fact]
    public void Documentos_so_publicam_versao_e_url_https_explicitamente_habilitadas()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        {
            ["DocumentosLegais:TermosUso:Publicado"]="true",
            ["DocumentosLegais:TermosUso:Versao"]="revisao-aprovada",
            ["DocumentosLegais:TermosUso:Url"]="https://example.test/termos",
            ["DocumentosLegais:PoliticaPrivacidade:Publicado"]="true",
            ["DocumentosLegais:PoliticaPrivacidade:Versao"]="v1",
            ["DocumentosLegais:PoliticaPrivacidade:Url"]="javascript:alert(1)"
        }).Build();
        var result = new PrivacidadeController(config).Documentos();
        Assert.Equal("TermosUso", Assert.Single(result.Documentos).Tipo); Assert.False(result.AceiteObrigatorio);
    }

    [Fact]
    public async Task Login_tem_limite_temporario_e_nao_reflete_credenciais()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        for (var i = 0; i < 20; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("ausente@example.test", "senha-ficticia"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        var blocked = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("outro@example.test", "senha-ficticia"));
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.NotNull(blocked.Headers.RetryAfter);
        var body = await blocked.Content.ReadAsStringAsync();
        Assert.Contains("LIMITE_REQUISICOES", body); Assert.DoesNotContain("senha-ficticia", body);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/privacidade/documentos")).StatusCode);
    }

    [Fact]
    public async Task Contadores_sao_atomicos_isolados_e_expiram()
    {
        var clock = new Clock(); var gate = new ControleAbuso(clock);
        var results = await Task.WhenAll(Enumerable.Range(0,100).Select(_ => Task.Run(() => gate.Tentar("mentoria","user:a",60))));
        Assert.Equal(60, results.Count(n=>n==0));
        Assert.Equal(0, gate.Tentar("mentoria","user:b",60));
        Assert.Equal(0, gate.Tentar("tutoria","user:a",12));
        clock.Now += TimeSpan.FromMinutes(1);
        Assert.Equal(0, gate.Tentar("mentoria","user:a",60));
    }
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
