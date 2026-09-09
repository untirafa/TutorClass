using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.AI;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public class TutorIaMemoriaTests
{
    [Fact]
    public async Task Continuacao_envia_papeis_corretos_e_instrucao_sem_saudacao()
    {
        await using var db = Db(); var aluno = Seed(db);
        var memoria = new TutorIaMemoria(); var handler = new ConversationHandler();
        using var http = new HttpClient(handler);
        var provider = new GeminiTutorIaProvider(http, new() { ApiKey="fake", Model="fake" }, NullLogger<GeminiTutorIaProvider>.Instance);
        var primeira = await new TutorIaService(db, provider, memoria: memoria).ConversarAsync(aluno.Id,new("Explique polimorfismo"),default);
        Assert.StartsWith("Olá", primeira.Resposta);
        // New service scope, same DI singleton memory.
        var segunda = await new TutorIaService(db, provider, memoria: memoria).ConversarAsync(aluno.Id,new("Dê um exemplo disso"),default);
        Assert.Equal("Um exemplo de polimorfismo: formas diferentes implementam Desenhar.", segunda.Resposta);
        Assert.Equal(2, db.UsosIaAluno.Single().Quantidade);
        Assert.Equal(4, memoria.Obter(aluno.Id).Count);
    }

    [Fact]
    public void Historico_isola_alunos_limita_dez_mensagens_e_preserva_pares()
    {
        var memoria = new TutorIaMemoria(); var a = Guid.NewGuid(); var b = Guid.NewGuid();
        memoria.Registrar(b,"Pergunta privada de B","Resposta de B");
        for(var i=0;i<8;i++) memoria.Registrar(a,$"Pergunta {i}",$"Resposta {i}");
        var historico = memoria.Obter(a);
        Assert.Equal(10,historico.Count);
        Assert.Equal("Pergunta 3",historico[0].Texto);
        Assert.Equal("Resposta 7",historico[^1].Texto);
        Assert.DoesNotContain(historico,m=>m.Texto.Contains("de B"));
        Assert.Equal(new[]{"user","assistant","user","assistant","user","assistant","user","assistant","user","assistant"},historico.Select(m=>m.Papel));
    }

    [Fact]
    public void Janela_limita_caracteres_sem_cortar_mensagens()
    {
        var memoria=new TutorIaMemoria();var id=Guid.NewGuid();
        for(var i=0;i<8;i++) memoria.Registrar(id,new string('a',4000),new string('b',4000));
        Assert.Equal(2,memoria.Obter(id).Count);
        Assert.True(memoria.Obter(id).Sum(m=>m.Texto.Length)<=12000);
        Assert.All(memoria.Obter(id),m=>Assert.Equal(4000,m.Texto.Length));
    }

    [Theory]
    [InlineData("Minha senha é confidencial")]
    [InlineData("Bearer eyJhbGci.abcdef.assinatura")]
    [InlineData("Meu email: teste@exemplo.com")]
    [InlineData("CPF 123.456.789-00")]
    public void Dados_sensiveis_nao_entram_no_historico(string sensivel)
    {
        var memoria=new TutorIaMemoria();var id=Guid.NewGuid();
        memoria.Registrar(id,sensivel,"Entendido");
        memoria.Registrar(id,"Oi",sensivel);
        Assert.Empty(memoria.Obter(id));
    }

    [Fact]
    public void Expira_apos_trinta_minutos()
    {
        var clock=new Clock();var memoria=new TutorIaMemoria(clock);var id=Guid.NewGuid();
        memoria.Registrar(id,"oi","resposta");clock.Now=clock.Now.AddMinutes(31);
        Assert.Empty(memoria.Obter(id));
    }

    [Theory]
    [InlineData("MAX_TOKENS","limite_saida")]
    [InlineData("TIMEOUT","timeout")]
    public async Task Resposta_incompleta_ou_timeout_nao_consume_nem_entra_na_memoria(string finish,string categoria)
    {
        await using var db=Db();var aluno=Seed(db);var memoria=new TutorIaMemoria();
        using var http=new HttpClient(new CutHandler(finish));
        var provider=new GeminiTutorIaProvider(http,new(){ApiKey="fake",Model="fake"},NullLogger<GeminiTutorIaProvider>.Instance);
        var ex=await Assert.ThrowsAsync<TutorIaProviderException>(()=>provider.ResponderAsync(new(aluno.Id,PlanoAluno.Foco,"oi",default,new()),default));
        Assert.Equal(categoria,ex.Categoria);
        var r=await new TutorIaService(db,provider,memoria:memoria).ConversarAsync(aluno.Id,new("oi"),default);
        Assert.Equal("ERRO_TUTORIA",r.Motivo);Assert.Null(r.Resposta);
        Assert.Empty(db.UsosIaAluno);Assert.Empty(memoria.Obter(aluno.Id));
    }

    private class Clock:TimeProvider { public DateTimeOffset Now=DateTimeOffset.UtcNow;public override DateTimeOffset GetUtcNow()=>Now; }
    private class CutHandler(string finish):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            if(finish=="TIMEOUT") throw new TaskCanceledException();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("""{"candidates":[{"finishReason":"MAX_TOKENS","content":{"parts":[{"text":"texto incompleto"}]}}]}""")});
        }
    }
    private class ConversationHandler:HttpMessageHandler
    {
        private int calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            using var json=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));var root=json.RootElement;
            Assert.Equal(2048,root.GetProperty("generationConfig").GetProperty("maxOutputTokens").GetInt32());
            var contents=root.GetProperty("contents");var instruction=root.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text").GetString()!;
            string answer;
            if(calls++==0)
            {
                Assert.Equal(1,contents.GetArrayLength());Assert.Contains("Primeira interacao: saudacao opcional",instruction);
                answer="Olá! Polimorfismo permite diferentes implementações da mesma operação.";
            }
            else
            {
                Assert.Equal(3,contents.GetArrayLength());
                Assert.Equal("user",contents[0].GetProperty("role").GetString());
                Assert.Equal("Explique polimorfismo",contents[0].GetProperty("parts")[0].GetProperty("text").GetString());
                Assert.Equal("model",contents[1].GetProperty("role").GetString());
                Assert.Contains("Polimorfismo",contents[1].GetProperty("parts")[0].GetProperty("text").GetString());
                Assert.Equal("user",contents[2].GetProperty("role").GetString());
                Assert.Contains("Continuacao: responda diretamente, sem saudacao",instruction);
                answer="Um exemplo de polimorfismo: formas diferentes implementam Desenhar.";
            }
            return new(HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(new {candidates=new[]{new{finishReason="STOP",content=new{parts=new[]{new{text=answer}}}}}}))};
        }
    }
    private static TutorClassDbContext Db()=>new(new DbContextOptionsBuilder<TutorClassDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static Aluno Seed(TutorClassDbContext db){var a=new Aluno("Aluno",Guid.NewGuid()+"@test.local","hash");db.Add(a);db.Add(new AssinaturaAluno(a.Id,a.Id,PlanoAluno.Foco,30,20,true));db.SaveChanges();return a;}
}
