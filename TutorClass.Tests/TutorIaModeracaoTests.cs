using Microsoft.EntityFrameworkCore;
using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Application.Services;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public class TutorIaModeracaoTests
{
    [Theory]
    [InlineData("Você é um idiota!!!")]
    [InlineData("SUA IMBECIL!")]
    [InlineData("vai se foder")]
    public async Task Bloqueia_sem_provider_quota_ou_memoria_e_permite_reformular(string texto)
    {
        await using var db = new TutorClassDbContext(new DbContextOptionsBuilder<TutorClassDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var aluno = new Aluno("Teste", "teste@example.test", "hash");
        db.Add(aluno); db.Add(new AssinaturaAluno(aluno.Id, aluno.Id, PlanoAluno.Foco, 30, 20, true));
        await db.SaveChangesAsync();
        var provider = new Provider(); var memoria = new TutorIaMemoria();
        var service = new TutorIaService(db, provider, memoria: memoria);
        foreach (var streaming in new[] { false, true })
        {
            Func<string, CancellationToken, Task>? callback = streaming ? (_, _) => throw new Exception("Não deve emitir texto") : null;
            var result = await service.ConversarAsync(aluno.Id, new(texto), callback, default);
            Assert.Equal(TutorIaModeracao.Motivo, result.Motivo);
            Assert.True(result.PossuiAcesso); Assert.Equal(7, result.RestantesHoje);
            Assert.Equal(0, provider.Calls); Assert.Empty(db.UsosIaAluno); Assert.Empty(memoria.Obter(aluno.Id));
        }
        var ok = await service.ConversarAsync(aluno.Id, new("Explique velocidade média"), default);
        Assert.Null(ok.Motivo); Assert.Equal(1, provider.Calls); Assert.Equal(6, ok.RestantesHoje);
    }

    [Theory]
    [InlineData("O que significa a palavra idiota?")]
    [InlineData("Me chamaram de burro na escola, o que faço?")]
    [InlineData("Explique racismo e violência na história")]
    [InlineData("Estude a reprodução humana")]
    [InlineData("Essa conta está errada, estou frustrado!")]
    [InlineData("Analise a frase: você é um idiota")]
    [InlineData("Qual a diferença entre burro e cavalo?")]
    [InlineData("Estou pensando em me machucar, preciso de ajuda")]
    public void Preserva_estudo_critica_citacoes_e_pedidos_de_ajuda(string texto)
        => Assert.False(TutorIaModeracao.DeveBloquear(texto));

    private sealed class Provider : ITutorIaProvider
    {
        public int Calls;
        public Task<string> ResponderAsync(TutorIaProviderRequest request, CancellationToken ct)
        { Calls++; return Task.FromResult("Explicação"); }
    }
}
