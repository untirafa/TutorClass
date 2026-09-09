using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Common;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Mentoring;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.AI;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public class GeminiTutorIaTests
{
    private static TutorClassDbContext Db() => new(new DbContextOptionsBuilder<TutorClassDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static Aluno Seed(TutorClassDbContext db, PlanoAluno plano = PlanoAluno.Foco)
    {
        var aluno = new Aluno("Aluno", Guid.NewGuid() + "@test.local", "segredo");
        db.Add(aluno);
        if (plano != PlanoAluno.Gratuito) db.Add(new AssinaturaAluno(aluno.Id, aluno.Id, plano, 30, 20, true));
        db.SaveChanges();
        return aluno;
    }
    [Theory]
    [InlineData(PlanoAluno.Foco, 5)]
    [InlineData(PlanoAluno.Mentoria, null)]
    [InlineData(PlanoAluno.Gratuito, 0)]
    public async Task Contexto_limita_filtra_e_calcula_quota(PlanoAluno plano, int? restantes)
    {
        await using var db = Db();
        var aluno = Seed(db, plano);
        var outro = Seed(db);
        var uso = new UsoIaAluno(aluno.Id, TutorIaService.GetHojeBrasil());
        uso.RegistrarUso(); uso.RegistrarUso(); db.Add(uso);
        for (int i = 0; i < 8; i++)
        {
            var p = new Professor("Professor " + i, i + "@test.local", "hash");
            var m = new Materia("Materia " + i);
            db.AddRange(p, m);
            var a = new Aula(aluno.Id, p.Id, m.Id, DateTime.UtcNow.AddDays(8-i), 60, 50);
            a.Confirmar(p.Id); db.Add(a);
            db.Add(new MentoriaAlunoProfessor(aluno.Id, p.Id, i % 2 + 1));
        }
        var privado = new Materia("Materia de outro aluno");
        var professor = new Professor("Professor privado", "privado@test.local", "hash");
        db.AddRange(privado, professor);
        var aulaOutro = new Aula(outro.Id, professor.Id, privado.Id, DateTime.UtcNow.AddHours(1), 60, 50);
        aulaOutro.Confirmar(professor.Id); db.Add(aulaOutro);
        db.Add(new MentoriaAlunoProfessor(outro.Id, professor.Id, 1));
        await db.SaveChangesAsync();
        var contexto = await new ContextoIaService(db).ObterContextoAsync(aluno.Id);
        Assert.Equal(plano.ToString(), contexto.Plano);
        Assert.Equal(2, contexto.MensagensUsadasHoje);
        Assert.Equal(restantes, contexto.MensagensRestantesHoje);
        Assert.Equal(5, contexto.Materias.Count);
        Assert.Equal(5, contexto.ProximasAulas.Count);
        Assert.All(contexto.ProximasAulas, a => { Assert.StartsWith("Professor ", a.Professor); Assert.StartsWith("Materia ", a.Materia); });
        Assert.Equal(2, contexto.MentoresAtivos.Count);
        Assert.Equal(contexto.ProximasAulas.OrderBy(a => a.InicioUtc), contexto.ProximasAulas);
        Assert.DoesNotContain("privado", JsonSerializer.Serialize(contexto), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(privado.Nome, contexto.Materias);
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Mensagem_vazia_nao_consume(string? mensagem)
    {
        await using var db = Db(); var aluno = Seed(db);
        await Assert.ThrowsAsync<DomainException>(() => new TutorIaService(db, new TutorIaSimuladaProvider())
            .ConversarAsync(aluno.Id, new(mensagem!), default));
        Assert.Empty(db.UsosIaAluno);
    }
    [Fact]
    public async Task Limite_de_4000_e_request_somente_mensagem()
    {
        await using var db = Db(); var aluno = Seed(db);
        var service = new TutorIaService(db, new TutorIaSimuladaProvider());
        await Assert.ThrowsAsync<DomainException>(() => service.ConversarAsync(aluno.Id, new(new string('a',4001)), default));
        Assert.True((await service.ConversarAsync(aluno.Id, new(new string('a',4000)), default)).PossuiAcesso);
        Assert.Single(typeof(TutorIaChatRequest).GetProperties());
    }
    [Theory]
    [InlineData(429, "{}", "limite_provider")]
    [InlineData(401, "{}", "autenticacao")]
    [InlineData(403, "{}", "autenticacao")]
    [InlineData(503, "{}", "servidor_provider")]
    [InlineData(200, "{}", "sem_candidates")]
    [InlineData(200, "not json", "json_inesperado")]
    [InlineData(200, "[]", "json_inesperado")]
    [InlineData(200, "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\" \"}]}}]}", "resposta_vazia")]
    [InlineData(0, "", "timeout")]
    public async Task Falhas_controladas_nao_consumem(int status, string body, string categoria)
    {
        using var client = new HttpClient(new Handler(status, body));
        var provider = new GeminiTutorIaProvider(client, new() { ApiKey="fake", Model="fake" }, NullLogger<GeminiTutorIaProvider>.Instance);
        var ex = await Assert.ThrowsAsync<TutorIaProviderException>(() => provider.ResponderAsync(new(Guid.NewGuid(), PlanoAluno.Foco,"oi",default,new()),default));
        Assert.Equal(categoria, ex.Categoria);
        await using var db = Db(); var aluno = Seed(db);
        var result = await new TutorIaService(db, provider).ConversarAsync(aluno.Id, new("oi"), default);
        Assert.Equal("ERRO_TUTORIA", result.Motivo);
        Assert.Empty(db.UsosIaAluno);
    }
    [Fact]
    public async Task Sucesso_envia_allowlist_e_registra_uso()
    {
        var handler = new Handler(200,"{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Resposta\"}]}}]}");
        using var client = new HttpClient(handler);
        await using var db = Db(); var aluno = Seed(db);
        var provider = new GeminiTutorIaProvider(client,new() { ApiKey="fake",Model="fake" },NullLogger<GeminiTutorIaProvider>.Instance);
        var result = await new TutorIaService(db,provider).ConversarAsync(aluno.Id,new("Qual meu plano?"),default);
        Assert.Equal("Resposta",result.Resposta);
        Assert.Equal(1,db.UsosIaAluno.Single().Quantidade);
        Assert.Contains("systemInstruction",handler.Body);
        Assert.Contains("Foco",handler.Body);
        Assert.DoesNotContain("segredo",handler.Body);
        Assert.DoesNotContain(aluno.Id.ToString(),handler.Body);
    }
    [Theory]
    [InlineData("Gemini")]
    [InlineData("Desconhecido")]
    public void Configuracao_invalida_falha_sem_fallback(string provider)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["TutorIA:Provider"] = provider }).Build();
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddTutorIaProvider(config));
    }
    [Fact]
    public void Simulado_selecionavel()
    {
        using var services = new ServiceCollection().AddTutorIaProvider(new ConfigurationBuilder().Build()).BuildServiceProvider();
        Assert.IsType<TutorIaSimuladaProvider>(services.GetRequiredService<ITutorIaProvider>());
    }
    [Fact]
    public async Task Contexto_forjado_no_json_nao_chega_ao_provider()
    {
        await using var db = Db(); var aluno = Seed(db);
        var request = JsonSerializer.Deserialize<TutorIaChatRequest>(
            """{"mensagem":"oi","plano":"Mentoria","contexto":{"nome":"INVASOR"},"mensagensRestantesHoje":999}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var handler = new Handler(200,"""{"candidates":[{"content":{"parts":[{"text":"ok"}]}}]}""");
        using var client = new HttpClient(handler);
        var provider = new GeminiTutorIaProvider(client, new() { ApiKey="fake", Model="fake" }, NullLogger<GeminiTutorIaProvider>.Instance);
        await new TutorIaService(db,provider).ConversarAsync(aluno.Id,request,default);
        Assert.DoesNotContain("INVASOR",handler.Body);
        Assert.DoesNotContain("999",handler.Body);
        Assert.Contains("Foco",handler.Body);
    }
    [Theory]
    [InlineData(503, 0)]
    [InlineData(0, 0)]
    [InlineData(200, 1)]
    public async Task Endpoint_Gemini_controla_falha_e_contabiliza_so_sucesso(int status, int consumo)
    {
        using var http = new HttpClient(new Handler(status,
            status == 200 ? """{"candidates":[{"content":{"parts":[{"text":"ok"}]}}]}""" :
            """{"error":{"code":503,"status":"UNAVAILABLE","message":"high demand"}}"""));
        var provider = new GeminiTutorIaProvider(http, new() { ApiKey="fake", Model="fake" }, NullLogger<GeminiTutorIaProvider>.Instance);
        await using var factory = new TutorClassApiFactory(services => services.AddScoped<ITutorIaProvider>(_ => provider));
        using var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
            var aluno = new Aluno("Aluno HTTP", "gemini.http@test.local", "pending");
            var hasher = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.IPasswordHasher<Usuario>>();
            aluno.DefinirPasswordHash(hasher.HashPassword(aluno, "TutorClass@123"));
            db.Add(aluno);
            db.Add(new AssinaturaAluno(aluno.Id, aluno.Id, PlanoAluno.Foco, 30, 20, true));
            await db.SaveChangesAsync();
        }
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("gemini.http@test.local", "TutorClass@123"));
        var auth = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var response = await client.PostAsJsonAsync("/api/tutoria/chat", new { mensagem = "Qual meu plano?" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(consumo, json.RootElement.GetProperty("usadasHoje").GetInt32());
        if (consumo == 0)
        {
            Assert.Equal("ERRO_TUTORIA", json.RootElement.GetProperty("motivo").GetString());
            Assert.Equal("A TutorIA está temporariamente indisponível. Tente novamente em alguns instantes.",
                json.RootElement.GetProperty("mensagem").GetString());
        }
        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
        Assert.Equal(consumo, verificationDb.UsosIaAluno.Sum(u => u.Quantidade));
    }

    [Fact]
    public async Task Pergunta_pessoal_recebe_plano_e_quota_reais_anteriores_ao_consumo()
    {
        await using var db = Db(); var aluno = Seed(db);
        var uso = new UsoIaAluno(aluno.Id, TutorIaService.GetHojeBrasil());
        uso.RegistrarUso(); uso.RegistrarUso(); db.Add(uso); await db.SaveChangesAsync();
        var response = await new TutorIaService(db, new ContextEchoProvider()).ConversarAsync(aluno.Id, new("Qual meu plano e quota?"), default);
        Assert.Equal("Foco: usadas 2, restantes 5", response.Resposta);
        Assert.Equal(3, response.UsadasHoje);
        Assert.Equal(4, response.RestantesHoje);
    }
    private sealed class ContextEchoProvider : ITutorIaProvider
    {
        public Task<string> ResponderAsync(TutorIaProviderRequest request, CancellationToken cancellationToken)
        {
            var c = request.Contexto!;
            Assert.Equal(request.AlunoId, c.AlunoId);
            return Task.FromResult($"{c.Plano}: usadas {c.MensagensUsadasHoje}, restantes {c.MensagensRestantesHoje}");
        }
    }
    [Fact]
    public async Task Log_de_503_contem_somente_diagnostico_sanitizado()
    {
        using var client = new HttpClient(new Handler(503,
            """{"error":{"code":503,"status":"UNAVAILABLE","message":"high demand PROMPT_PRIVADO CHAVE_PRIVADA","details":["CONTEXTO_PRIVADO"]}}"""));
        var log = new CaptureLogger();
        var provider = new GeminiTutorIaProvider(client, new() { ApiKey="CHAVE_PRIVADA", Model="modelo-teste" }, log);
        await Assert.ThrowsAsync<TutorIaProviderException>(() => provider.ResponderAsync(
            new(Guid.NewGuid(), PlanoAluno.Foco, "PROMPT_PRIVADO", default, new()), default));
        Assert.Contains("HttpStatus=503", log.Text);
        Assert.Contains("GoogleStatus=UNAVAILABLE", log.Text);
        Assert.Contains("GoogleReason=alta_demanda", log.Text);
        Assert.Contains("Model=modelo-teste", log.Text);
        Assert.DoesNotContain("PRIVAD", log.Text);
    }
    private sealed class CaptureLogger : Microsoft.Extensions.Logging.ILogger<GeminiTutorIaProvider>
    {
        public string Text = "";
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId,
            TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Text = formatter(state, exception);
    }
    private sealed class Handler(int status,string body) : HttpMessageHandler
    {
        public string Body = "";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Body = await request.Content!.ReadAsStringAsync(token);
            if(status == 0) throw new TaskCanceledException();
            return new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(body,Encoding.UTF8,"application/json") };
        }
    }
}
