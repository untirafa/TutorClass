using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Common;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class TutorIaTests
{
    [Fact]
    public async Task Gratuito_nao_possui_acesso_nao_chama_provider_nem_incrementa_uso()
    {
        await using var db = CreateDbContext();
        var aluno = SeedAluno(db);
        var provider = new FakeTutorIaProvider();
        var service = new TutorIaService(db, provider);

        var status = await service.ObterStatusAsync(aluno.Id, CancellationToken.None);
        var chat = await service.ConversarAsync(aluno.Id, new TutorIaChatRequest("Explique fotossintese"), CancellationToken.None);

        Assert.False(status.PossuiAcesso);
        Assert.Equal("PLANO_SEM_TUTORIA", status.Motivo);
        Assert.False(chat.PossuiAcesso);
        Assert.Equal("PLANO_SEM_TUTORIA", chat.Motivo);
        Assert.Equal(0, provider.Chamadas);
        Assert.Empty(db.UsosIaAluno);
    }

    [Fact]
    public async Task Foco_permite_sete_mensagens_e_bloqueia_oitava_sem_chamar_provider()
    {
        await using var db = CreateDbContext();
        var aluno = SeedAluno(db);
        SeedAssinatura(db, aluno.Id, PlanoAluno.Foco);
        var provider = new FakeTutorIaProvider();
        var service = new TutorIaService(db, provider);

        for (var i = 1; i <= 7; i++)
        {
            var response = await service.ConversarAsync(aluno.Id, new TutorIaChatRequest($"Duvida {i}"), CancellationToken.None);
            Assert.True(response.PossuiAcesso);
            Assert.Equal(i, response.UsadasHoje);
            Assert.Equal(7 - i, response.RestantesHoje);
        }

        var oitava = await service.ConversarAsync(aluno.Id, new TutorIaChatRequest("Duvida 8"), CancellationToken.None);

        Assert.False(oitava.PossuiAcesso);
        Assert.Equal("LIMITE_DIARIO_ATINGIDO", oitava.Motivo);
        Assert.Equal(7, oitava.UsadasHoje);
        Assert.Equal(0, oitava.RestantesHoje);
        Assert.Equal(7, provider.Chamadas);
        Assert.Equal(7, db.UsosIaAluno.Single().Quantidade);
    }

    [Theory]
    [InlineData(0, true, 7, false)]
    [InlineData(4, true, 3, false)]
    [InlineData(7, false, 0, true)]
    public async Task Status_do_foco_calcula_restantes_corretamente(int usadasHoje, bool possuiAcesso, int restantes, bool limiteAtingido)
    {
        await using var db = CreateDbContext();
        var aluno = SeedAluno(db);
        SeedAssinatura(db, aluno.Id, PlanoAluno.Foco);
        if (usadasHoje > 0)
        {
            SeedUso(db, aluno.Id, TutorIaService.GetHojeBrasil(), usadasHoje);
        }

        var status = await new TutorIaService(db, new FakeTutorIaProvider()).ObterStatusAsync(aluno.Id, CancellationToken.None);

        Assert.Equal(possuiAcesso, status.PossuiAcesso);
        Assert.Equal(7, status.LimiteDiario);
        Assert.Equal(usadasHoje, status.UsadasHoje);
        Assert.Equal(restantes, status.RestantesHoje);
        Assert.Equal(limiteAtingido, status.LimiteAtingido);
    }

    [Fact]
    public async Task Uso_de_ontem_nao_bloqueia_foco_hoje()
    {
        await using var db = CreateDbContext();
        var aluno = SeedAluno(db);
        SeedAssinatura(db, aluno.Id, PlanoAluno.Foco);
        SeedUso(db, aluno.Id, TutorIaService.GetHojeBrasil().AddDays(-1), 7);

        var status = await new TutorIaService(db, new FakeTutorIaProvider()).ObterStatusAsync(aluno.Id, CancellationToken.None);

        Assert.True(status.PossuiAcesso);
        Assert.Equal(0, status.UsadasHoje);
        Assert.Equal(7, status.RestantesHoje);
    }

    [Fact]
    public async Task Mentoria_pode_ultrapassar_sete_mensagens_sem_limite_diario_do_plano()
    {
        await using var db = CreateDbContext();
        var aluno = SeedAluno(db);
        SeedAssinatura(db, aluno.Id, PlanoAluno.Mentoria);
        var provider = new FakeTutorIaProvider();
        var service = new TutorIaService(db, provider);

        for (var i = 0; i < 10; i++)
        {
            var response = await service.ConversarAsync(aluno.Id, new TutorIaChatRequest($"Duvida mentoria {i}"), CancellationToken.None);
            Assert.True(response.PossuiAcesso);
            Assert.Null(response.LimiteDiario);
        }

        var status = await service.ObterStatusAsync(aluno.Id, CancellationToken.None);
        Assert.True(status.PossuiAcesso);
        Assert.Null(status.LimiteDiario);
        Assert.Equal(10, status.UsadasHoje);
        Assert.Equal(10, provider.Chamadas);
    }

    [Fact]
    public async Task Erro_do_provider_nao_consume_mensagem()
    {
        await using var db = CreateDbContext();
        var aluno = SeedAluno(db);
        SeedAssinatura(db, aluno.Id, PlanoAluno.Foco);
        var provider = new FakeTutorIaProvider { ThrowOnCall = true };
        var service = new TutorIaService(db, provider);

        var response = await service.ConversarAsync(aluno.Id, new TutorIaChatRequest("Explique equacoes"), CancellationToken.None);

        Assert.False(response.PossuiAcesso);
        Assert.Equal("ERRO_TUTORIA", response.Motivo);
        Assert.Empty(db.UsosIaAluno);
    }

    [Fact]
    public async Task Chamadas_simultaneas_no_foco_nao_ultrapassam_limite()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = CreateOptions(databaseName);

        await using (var seedDb = new TutorClassDbContext(options))
        {
            var aluno = SeedAluno(seedDb);
            SeedAssinatura(seedDb, aluno.Id, PlanoAluno.Foco);
            SeedUso(seedDb, aluno.Id, TutorIaService.GetHojeBrasil(), 6);
            await seedDb.SaveChangesAsync();
        }

        await using var db1 = new TutorClassDbContext(options);
        await using var db2 = new TutorClassDbContext(options);
        var alunoId = db1.Usuarios.OfType<Aluno>().Single().Id;
        var provider = new FakeTutorIaProvider { Delay = TimeSpan.FromMilliseconds(50) };
        var service1 = new TutorIaService(db1, provider);
        var service2 = new TutorIaService(db2, provider);

        var results = await Task.WhenAll(
            service1.ConversarAsync(alunoId, new TutorIaChatRequest("Primeira chamada simultanea"), CancellationToken.None),
            service2.ConversarAsync(alunoId, new TutorIaChatRequest("Segunda chamada simultanea"), CancellationToken.None));

        await using var assertDb = new TutorClassDbContext(options);
        Assert.Single(results, r => r.PossuiAcesso);
        Assert.Single(results, r => !r.PossuiAcesso && r.Motivo == "LIMITE_DIARIO_ATINGIDO");
        Assert.Equal(1, provider.Chamadas);
        Assert.Equal(7, assertDb.UsosIaAluno.Single().Quantidade);
    }

    [Fact]
    public async Task Usuario_nao_autenticado_nao_acessa_endpoint_tutoria()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/tutoria/status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static Aluno SeedAluno(TutorClassDbContext db)
    {
        var aluno = new Aluno("Aluno TutorIA", $"aluno.tutoria.{Guid.NewGuid()}@teste.local", "hash");
        db.Add(aluno);
        db.SaveChanges();
        return aluno;
    }

    private static void SeedAssinatura(TutorClassDbContext db, Guid alunoId, PlanoAluno plano)
    {
        db.Add(new AssinaturaAluno(alunoId, alunoId, plano, plano == PlanoAluno.Foco ? 29.99m : 79.99m, plano == PlanoAluno.Foco ? 19.99m : 59.99m, true));
        db.SaveChanges();
    }

    private static void SeedUso(TutorClassDbContext db, Guid alunoId, DateOnly data, int quantidade)
    {
        var uso = new UsoIaAluno(alunoId, data);
        for (var i = 0; i < quantidade; i++)
        {
            uso.RegistrarUso();
        }

        db.Add(uso);
        db.SaveChanges();
    }

    private static TutorClassDbContext CreateDbContext()
    {
        return new TutorClassDbContext(CreateOptions(Guid.NewGuid().ToString()));
    }

    private static DbContextOptions<TutorClassDbContext> CreateOptions(string databaseName)
    {
        return new DbContextOptionsBuilder<TutorClassDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
    }

    private sealed class FakeTutorIaProvider : ITutorIaProvider
    {
        public int Chamadas { get; private set; }
        public bool ThrowOnCall { get; init; }
        public TimeSpan Delay { get; init; }

        public async Task<string> ResponderAsync(TutorIaProviderRequest request, CancellationToken cancellationToken)
        {
            Chamadas++;
            if (Delay > TimeSpan.Zero)
            {
                await Task.Delay(Delay, cancellationToken);
            }

            if (ThrowOnCall)
            {
                throw new InvalidOperationException("Falha simulada.");
            }

            return "Resposta simulada.";
        }
    }
}
