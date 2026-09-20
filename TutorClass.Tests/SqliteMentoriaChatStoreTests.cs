using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorClass.Application.Abstractions;
using TutorClass.Domain.Mentoring;
using TutorClass.Infrastructure;
using TutorClass.Infrastructure.Chat;

namespace TutorClass.Tests;

public sealed class SqliteMentoriaChatStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "TutorClassChatTests", Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_directory, "chat.db");

    [Fact]
    public async Task Conversa_e_mensagens_persistem_apos_reabrir_banco_local()
    {
        var vinculo = NewVinculo();
        string chatId;
        string messageId;
        using (var original = new SqliteMentoriaChatStore(DatabasePath))
        {
            var chat = await original.ObterOuCriarChatAsync(vinculo, CancellationToken.None);
            var message = await original.SalvarMensagemAsync(chat, vinculo.ProfessorId, "Professora Ana",
                "Atividade: resolva os exercícios 1 e 2. ' < >", CancellationToken.None);
            chatId = chat.Id;
            messageId = message.Id;
        }

        using var reopened = new SqliteMentoriaChatStore(DatabasePath);
        var persistedChat = await reopened.ObterOuCriarChatAsync(vinculo, CancellationToken.None);
        var messages = await reopened.ListarMensagensAsync(vinculo.Id, 30, null, CancellationToken.None);
        var persistedMessage = Assert.Single(messages);
        Assert.Equal(chatId, persistedChat.Id);
        Assert.Equal(messageId, persistedMessage.Id);
        Assert.Equal("Atividade: resolva os exercícios 1 e 2. ' < >", persistedMessage.Conteudo);
        Assert.Equal(vinculo.ProfessorId, persistedMessage.RemetenteUsuarioId);
        Assert.Equal(DateTimeKind.Utc, persistedMessage.DataEnvioUtc.Kind);
        Assert.True(await reopened.IsHealthyAsync(CancellationToken.None));

        vinculo.Encerrar(MotivoEncerramentoMentoria.TrocaSolicitadaPeloAluno);
        var closedChat = await reopened.ObterOuCriarChatAsync(vinculo, CancellationToken.None);
        Assert.True(closedChat.SomenteLeitura);
        Assert.Equal(chatId, closedChat.Id);
    }

    [Fact]
    public async Task Paginacao_recupera_todas_as_mensagens_em_ordem_sem_duplicar_ou_misturar_conversas()
    {
        using var store = new SqliteMentoriaChatStore(DatabasePath);
        var vinculo = NewVinculo();
        var chat = await store.ObterOuCriarChatAsync(vinculo, CancellationToken.None);
        var sent = new List<string>();
        for (var i = 0; i < 11; i++)
        {
            sent.Add((await store.SalvarMensagemAsync(chat, vinculo.AlunoId, "Aluno", $"Mensagem {i}", CancellationToken.None)).Id);
        }
        var other = NewVinculo();
        var otherChat = await store.ObterOuCriarChatAsync(other, CancellationToken.None);
        await store.SalvarMensagemAsync(otherChat, other.AlunoId, "Outro aluno", "Conversa privada", CancellationToken.None);

        DateTime? before = null;
        var received = new List<string>();
        do
        {
            var page = (await store.ListarMensagensAsync(vinculo.Id, 3, before, CancellationToken.None)).ToArray();
            if (page.Length == 0) break;
            Assert.InRange(page.Length, 1, 3);
            Assert.Equal(page.OrderBy(m => m.DataEnvioUtc).Select(m => m.Id), page.Select(m => m.Id));
            Assert.All(page, message => Assert.Equal(vinculo.Id, message.VinculoMentoriaId));
            received.InsertRange(0, page.Select(m => m.Id));
            before = page[0].DataEnvioUtc;
        } while (true);

        Assert.Equal(sent, received);
        Assert.Equal(received.Count, received.Distinct().Count());
    }

    [Fact]
    public async Task Leitura_persiste_apenas_para_mensagens_recebidas_no_vinculo_selecionado()
    {
        var vinculo = NewVinculo();
        var other = NewVinculo();
        string receivedId;
        using (var store = new SqliteMentoriaChatStore(DatabasePath))
        {
            var chat = await store.ObterOuCriarChatAsync(vinculo, CancellationToken.None);
            receivedId = (await store.SalvarMensagemAsync(chat, vinculo.AlunoId, "Aluno", "Pode me ajudar?", CancellationToken.None)).Id;
            await store.SalvarMensagemAsync(chat, vinculo.ProfessorId, "Professor", "Sim", CancellationToken.None);
            var otherChat = await store.ObterOuCriarChatAsync(other, CancellationToken.None);
            await store.SalvarMensagemAsync(otherChat, other.AlunoId, "Outro", "Outra conversa", CancellationToken.None);

            Assert.Equal(1, await store.MarcarComoLidasAsync(vinculo.Id, vinculo.ProfessorId, CancellationToken.None));
            Assert.Equal(0, await store.MarcarComoLidasAsync(vinculo.Id, vinculo.ProfessorId, CancellationToken.None));
        }

        using var reopened = new SqliteMentoriaChatStore(DatabasePath);
        var messages = await reopened.ListarMensagensAsync(vinculo.Id, 30, null, CancellationToken.None);
        var received = messages.Single(m => m.Id == receivedId);
        Assert.True(received.Lida);
        Assert.NotNull(received.DataLeituraUtc);
        Assert.False(messages.Single(m => m.RemetenteUsuarioId == vinculo.ProfessorId).Lida);
        Assert.False(Assert.Single(await reopened.ListarMensagensAsync(other.Id, 30, null, CancellationToken.None)).Lida);
    }

    [Fact]
    public async Task Requisicoes_concorrentes_criam_uma_conversa_e_preservam_todas_as_mensagens()
    {
        using var first = new SqliteMentoriaChatStore(DatabasePath);
        using var second = new SqliteMentoriaChatStore(DatabasePath);
        var vinculo = NewVinculo();
        var chats = await Task.WhenAll(Enumerable.Range(0, 8).Select(index => Task.Run(() =>
            (index % 2 == 0 ? first : second).ObterOuCriarChatAsync(vinculo, CancellationToken.None))));
        Assert.Single(chats.Select(chat => chat.Id).Distinct());

        var messages = await Task.WhenAll(Enumerable.Range(0, 16).Select(index => Task.Run(() =>
            (index % 2 == 0 ? first : second).SalvarMensagemAsync(chats[0], vinculo.AlunoId, "Aluno", $"Mensagem {index}", CancellationToken.None))));
        var history = await first.ListarMensagensAsync(vinculo.Id, 100, null, CancellationToken.None);
        Assert.Equal(16, history.Count);
        Assert.Equal(16, history.Select(message => message.DataEnvioUtc).Distinct().Count());
        Assert.Equal(messages.Select(message => message.Id).Order(), history.Select(message => message.Id).Order());
    }

    [Fact]
    public void Modo_local_sem_mongo_utiliza_chat_sqlite()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:TutorClassDb"] = $"Data Source={Path.Combine(_directory, "tutorclass.db")}"
        }).Build();
        using var provider = new ServiceCollection().AddInfrastructure(configuration, useLocalDatabase: true).BuildServiceProvider();
        Assert.IsType<SqliteMentoriaChatStore>(provider.GetRequiredService<IMentoriaChatStore>());
    }

    [Fact]
    public void Mongo_configurado_tem_prioridade_sobre_chat_local()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:TutorClassDb"] = $"Data Source={Path.Combine(_directory, "tutorclass.db")}",
            ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
            ["MongoDb:DatabaseName"] = "chat-tests"
        }).Build();
        var services = new ServiceCollection().AddInfrastructure(configuration, useLocalDatabase: true);
        Assert.Equal(typeof(MongoMentoriaChatStore), services.Last(s => s.ServiceType == typeof(IMentoriaChatStore)).ImplementationType);
    }

    [Fact]
    public void Fora_do_modo_local_nao_substitui_mongo_ausente_por_sqlite()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:TutorClassDb"] = "Host=localhost;Database=tutorclass"
        }).Build();
        var services = new ServiceCollection().AddInfrastructure(configuration);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IMentoriaChatStore));
    }

    private static MentoriaAlunoProfessor NewVinculo() => new(Guid.NewGuid(), Guid.NewGuid(), 1);

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
