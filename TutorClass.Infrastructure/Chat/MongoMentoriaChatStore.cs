using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Mentoring;

namespace TutorClass.Infrastructure.Chat;

public sealed class MongoMentoriaChatStore : IMentoriaChatStore
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<ChatMentoriaDocument> _chats;
    private readonly IMongoCollection<MensagemMentoriaDocument> _mensagens;
    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private bool _indexesEnsured;

    public MongoMentoriaChatStore(IMongoDatabase database)
    {
        _database = database;
        _chats = database.GetCollection<ChatMentoriaDocument>("chat_mentoria");
        _mensagens = database.GetCollection<MensagemMentoriaDocument>("mensagens_mentoria");
    }

    public bool Configurado => true;

    public async Task<MentoriaChatResponse> ObterOuCriarChatAsync(MentoriaAlunoProfessor vinculo, CancellationToken cancellationToken)
    {
        try
        {
            await EnsureIndexesAsync(cancellationToken);

            var existing = await _chats
                .Find(c => c.VinculoMentoriaId == vinculo.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                return ToResponse(existing, !vinculo.Ativo);
            }

            var document = new ChatMentoriaDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                VinculoMentoriaId = vinculo.Id,
                AlunoId = vinculo.AlunoId,
                ProfessorId = vinculo.ProfessorId,
                DataCriacaoUtc = DateTime.UtcNow
            };

            await _chats.InsertOneAsync(document, cancellationToken: cancellationToken);
            return ToResponse(document, !vinculo.Ativo);
        }
        catch (Exception ex) when (IsMongoUnavailable(ex))
        {
            throw BuildUnavailableException(ex);
        }
    }

    public async Task<MensagemMentoriaResponse> SalvarMensagemAsync(MentoriaChatResponse chat, Guid remetenteUsuarioId, string remetenteNome, string conteudo, CancellationToken cancellationToken)
    {
        try
        {
            await EnsureIndexesAsync(cancellationToken);

            var document = new MensagemMentoriaDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                ChatMentoriaId = chat.Id,
                VinculoMentoriaId = chat.VinculoMentoriaId,
                RemetenteUsuarioId = remetenteUsuarioId,
                RemetenteNome = remetenteNome,
                Conteudo = conteudo,
                DataEnvioUtc = DateTime.UtcNow,
                Lida = false
            };

            await _mensagens.InsertOneAsync(document, cancellationToken: cancellationToken);
            return ToResponse(document);
        }
        catch (Exception ex) when (IsMongoUnavailable(ex))
        {
            throw BuildUnavailableException(ex);
        }
    }

    public async Task<IReadOnlyCollection<MensagemMentoriaResponse>> ListarMensagensAsync(Guid vinculoMentoriaId, int limit, DateTime? beforeUtc, CancellationToken cancellationToken)
    {
        try
        {
            await EnsureIndexesAsync(cancellationToken);

            var filter = Builders<MensagemMentoriaDocument>.Filter.Eq(m => m.VinculoMentoriaId, vinculoMentoriaId);
            if (beforeUtc is not null)
            {
                filter &= Builders<MensagemMentoriaDocument>.Filter.Lt(m => m.DataEnvioUtc, beforeUtc.Value);
            }

            var messages = await _mensagens
                .Find(filter)
                .SortByDescending(m => m.DataEnvioUtc)
                .Limit(limit)
                .ToListAsync(cancellationToken);

            return messages
                .OrderBy(m => m.DataEnvioUtc)
                .Select(ToResponse)
                .ToArray();
        }
        catch (Exception ex) when (IsMongoUnavailable(ex))
        {
            throw BuildUnavailableException(ex);
        }
    }

    public async Task<int> MarcarComoLidasAsync(Guid vinculoMentoriaId, Guid leitorUsuarioId, CancellationToken cancellationToken)
    {
        try
        {
            await EnsureIndexesAsync(cancellationToken);

            var filter = Builders<MensagemMentoriaDocument>.Filter.Eq(m => m.VinculoMentoriaId, vinculoMentoriaId) &
                Builders<MensagemMentoriaDocument>.Filter.Ne(m => m.RemetenteUsuarioId, leitorUsuarioId) &
                Builders<MensagemMentoriaDocument>.Filter.Eq(m => m.Lida, false);

            var update = Builders<MensagemMentoriaDocument>.Update
                .Set(m => m.Lida, true)
                .Set(m => m.DataLeituraUtc, DateTime.UtcNow);

            var result = await _mensagens.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
            return (int)result.ModifiedCount;
        }
        catch (Exception ex) when (IsMongoUnavailable(ex))
        {
            throw BuildUnavailableException(ex);
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            await EnsureIndexesAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        if (_indexesEnsured)
        {
            return;
        }

        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            if (_indexesEnsured)
            {
                return;
            }

            await _chats.Indexes.CreateOneAsync(new CreateIndexModel<ChatMentoriaDocument>(
                Builders<ChatMentoriaDocument>.IndexKeys.Ascending(c => c.VinculoMentoriaId),
                new CreateIndexOptions { Unique = true, Name = "ux_chat_mentoria_vinculo_mentoria_id" }), cancellationToken: cancellationToken);

            await _mensagens.Indexes.CreateOneAsync(new CreateIndexModel<MensagemMentoriaDocument>(
                Builders<MensagemMentoriaDocument>.IndexKeys
                    .Ascending(m => m.VinculoMentoriaId)
                    .Descending(m => m.DataEnvioUtc),
                new CreateIndexOptions { Name = "ix_mensagens_mentoria_vinculo_data_envio" }), cancellationToken: cancellationToken);

            await _mensagens.Indexes.CreateOneAsync(new CreateIndexModel<MensagemMentoriaDocument>(
                Builders<MensagemMentoriaDocument>.IndexKeys
                    .Ascending(m => m.ChatMentoriaId)
                    .Descending(m => m.DataEnvioUtc),
                new CreateIndexOptions { Name = "ix_mensagens_mentoria_chat_data_envio" }), cancellationToken: cancellationToken);

            _indexesEnsured = true;
        }
        finally
        {
            _indexLock.Release();
        }
    }

    private static bool IsMongoUnavailable(Exception ex)
    {
        return ex is MongoException or TimeoutException;
    }

    private static MentoriaChatUnavailableException BuildUnavailableException(Exception ex)
    {
        return new MentoriaChatUnavailableException(
            "Chat de Mentoria indisponivel: verifique a configuracao e a conectividade do MongoDB.",
            ex);
    }

    private static MentoriaChatResponse ToResponse(ChatMentoriaDocument document, bool somenteLeitura)
    {
        return new MentoriaChatResponse(document.Id, document.VinculoMentoriaId, document.AlunoId, document.ProfessorId, somenteLeitura, document.DataCriacaoUtc);
    }

    private static MensagemMentoriaResponse ToResponse(MensagemMentoriaDocument document)
    {
        return new MensagemMentoriaResponse(
            document.Id,
            document.ChatMentoriaId,
            document.VinculoMentoriaId,
            document.RemetenteUsuarioId,
            document.RemetenteNome,
            document.Conteudo,
            document.DataEnvioUtc,
            document.Lida,
            document.DataLeituraUtc);
    }

    private sealed class ChatMentoriaDocument
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid VinculoMentoriaId { get; set; }
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid AlunoId { get; set; }
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid ProfessorId { get; set; }
        public DateTime DataCriacaoUtc { get; set; }
    }

    private sealed class MensagemMentoriaDocument
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;
        public string ChatMentoriaId { get; set; } = string.Empty;
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid VinculoMentoriaId { get; set; }
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid RemetenteUsuarioId { get; set; }
        public string RemetenteNome { get; set; } = string.Empty;
        public string Conteudo { get; set; } = string.Empty;
        public DateTime DataEnvioUtc { get; set; }
        public bool Lida { get; set; }
        public DateTime? DataLeituraUtc { get; set; }
    }
}
