using Microsoft.Data.Sqlite;
using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Mentoring;

namespace TutorClass.Infrastructure.Chat;

/// <summary>Persistent chat for the local development database; production keeps its MongoDB store.</summary>
public sealed class SqliteMentoriaChatStore : IMentoriaChatStore, IDisposable
{
    // Todas as instâncias que apontam para o mesmo arquivo compartilham o mesmo bloqueio: o SQLite aceita um único
    // escritor por vez e "database is locked" (SQLITE_BUSY) é devolvido na hora quando duas conexões disputam a escrita.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> WriteLocks = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _connectionString;
    private readonly SemaphoreSlim _writeLock;
    private readonly SemaphoreSlim _schemaLock = new(1, 1);
    private bool _schemaCreated;

    public SqliteMentoriaChatStore(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var fullPath = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            ForeignKeys = true,
            Pooling = false,
            DefaultTimeout = 30
        }.ToString();
        _writeLock = WriteLocks.GetOrAdd(fullPath, _ => new SemaphoreSlim(1, 1));
    }

    public bool Configurado => true;

    public Task<MentoriaChatResponse> ObterOuCriarChatAsync(MentoriaAlunoProfessor vinculo, CancellationToken cancellationToken)
        => ExecuteAsync(async connection =>
        {
            using (var insert = connection.CreateCommand())
            {
                insert.CommandText = """
                    INSERT INTO chats (id, vinculo_id, aluno_id, professor_id, criacao_utc)
                    VALUES ($id, $vinculo, $aluno, $professor, $criacao)
                    ON CONFLICT(vinculo_id) DO NOTHING;
                    """;
                insert.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("N"));
                insert.Parameters.AddWithValue("$vinculo", vinculo.Id.ToString());
                insert.Parameters.AddWithValue("$aluno", vinculo.AlunoId.ToString());
                insert.Parameters.AddWithValue("$professor", vinculo.ProfessorId.ToString());
                insert.Parameters.AddWithValue("$criacao", DateTime.UtcNow.Ticks);
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }

            using var select = connection.CreateCommand();
            select.CommandText = "SELECT id, aluno_id, professor_id, criacao_utc FROM chats WHERE vinculo_id = $vinculo;";
            select.Parameters.AddWithValue("$vinculo", vinculo.Id.ToString());
            await using var reader = await select.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            return new MentoriaChatResponse(reader.GetString(0), vinculo.Id,
                Guid.Parse(reader.GetString(1)), Guid.Parse(reader.GetString(2)), !vinculo.Ativo,
                FromTicks(reader.GetInt64(3)));
        }, cancellationToken);

    public Task<MensagemMentoriaResponse> SalvarMensagemAsync(MentoriaChatResponse chat, Guid remetenteUsuarioId,
        string remetenteNome, string conteudo, CancellationToken cancellationToken)
        => ExecuteAsync(async connection =>
        {
            var id = Guid.NewGuid().ToString("N");
            using var command = connection.CreateCommand();
            // The timestamp is strictly increasing within each chat, including concurrent writes,
            // so the API's timestamp-only pagination cursor cannot skip tied messages.
            command.CommandText = """
                INSERT INTO mensagens (id, chat_id, vinculo_id, remetente_id, remetente_nome, conteudo, envio_utc, lida)
                VALUES ($id, $chat, $vinculo, $remetente, $nome, $conteudo,
                    MAX($envio, COALESCE((SELECT MAX(envio_utc) + 1 FROM mensagens WHERE vinculo_id = $vinculo), 0)), 0)
                RETURNING envio_utc;
                """;
            command.Parameters.AddWithValue("$id", id);
            command.Parameters.AddWithValue("$chat", chat.Id);
            command.Parameters.AddWithValue("$vinculo", chat.VinculoMentoriaId.ToString());
            command.Parameters.AddWithValue("$remetente", remetenteUsuarioId.ToString());
            command.Parameters.AddWithValue("$nome", remetenteNome);
            command.Parameters.AddWithValue("$conteudo", conteudo);
            command.Parameters.AddWithValue("$envio", DateTime.UtcNow.Ticks);
            var sentTicks = (long)(await command.ExecuteScalarAsync(cancellationToken))!;
            return new MensagemMentoriaResponse(id, chat.Id, chat.VinculoMentoriaId, remetenteUsuarioId,
                remetenteNome, conteudo, FromTicks(sentTicks), false, null);
        }, cancellationToken);

    public Task<IReadOnlyCollection<MensagemMentoriaResponse>> ListarMensagensAsync(Guid vinculoMentoriaId,
        int limit, DateTime? beforeUtc, CancellationToken cancellationToken)
        => ExecuteAsync<IReadOnlyCollection<MensagemMentoriaResponse>>(async connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT id, chat_id, remetente_id, remetente_nome, conteudo, envio_utc, lida, leitura_utc
                FROM mensagens
                WHERE vinculo_id = $vinculo AND ($before IS NULL OR envio_utc < $before)
                ORDER BY envio_utc DESC LIMIT $limit;
                """;
            command.Parameters.AddWithValue("$vinculo", vinculoMentoriaId.ToString());
            var beforeTicks = beforeUtc?.Kind == DateTimeKind.Local ? beforeUtc.Value.ToUniversalTime().Ticks : beforeUtc?.Ticks;
            command.Parameters.AddWithValue("$before", (object?)beforeTicks ?? DBNull.Value);
            command.Parameters.AddWithValue("$limit", Math.Clamp(limit, 1, 100));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var messages = new List<MensagemMentoriaResponse>();
            while (await reader.ReadAsync(cancellationToken))
            {
                messages.Add(new MensagemMentoriaResponse(reader.GetString(0), reader.GetString(1), vinculoMentoriaId,
                    Guid.Parse(reader.GetString(2)), reader.GetString(3), reader.GetString(4), FromTicks(reader.GetInt64(5)),
                    reader.GetBoolean(6), reader.IsDBNull(7) ? null : FromTicks(reader.GetInt64(7))));
            }
            messages.Reverse();
            return messages;
        }, cancellationToken);

    public Task<int> MarcarComoLidasAsync(Guid vinculoMentoriaId, Guid leitorUsuarioId, CancellationToken cancellationToken)
        => ExecuteAsync(async connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE mensagens SET lida = 1, leitura_utc = $leitura
                WHERE vinculo_id = $vinculo AND remetente_id <> $leitor AND lida = 0;
                """;
            command.Parameters.AddWithValue("$leitura", DateTime.UtcNow.Ticks);
            command.Parameters.AddWithValue("$vinculo", vinculoMentoriaId.ToString());
            command.Parameters.AddWithValue("$leitor", leitorUsuarioId.ToString());
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }, cancellationToken);

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await ExecuteAsync(async connection =>
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM chats;";
                await command.ExecuteScalarAsync(cancellationToken);
                return true;
            }, cancellationToken);
        }
        catch (MentoriaChatUnavailableException) { return false; }
    }

    private async Task<T> ExecuteAsync<T>(Func<SqliteConnection, Task<T>> operation, CancellationToken cancellationToken)
    {
        try
        {
            await EnsureSchemaAsync(cancellationToken);
            await _writeLock.WaitAsync(cancellationToken);
            try
            {
                await using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                using (var pragma = connection.CreateCommand())
                {
                    pragma.CommandText = "PRAGMA busy_timeout = 10000;";
                    await pragma.ExecuteNonQueryAsync(cancellationToken);
                }
                return await operation(connection);
            }
            finally { _writeLock.Release(); }
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            throw new MentoriaChatUnavailableException("Chat de Mentoria indisponivel: nao foi possivel acessar o banco local de mensagens.", ex);
        }
    }

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        if (_schemaCreated) return;
        await _schemaLock.WaitAsync(cancellationToken);
        try
        {
            if (_schemaCreated) return;
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS chats (
                    id TEXT PRIMARY KEY, vinculo_id TEXT NOT NULL UNIQUE,
                    aluno_id TEXT NOT NULL, professor_id TEXT NOT NULL, criacao_utc INTEGER NOT NULL,
                    UNIQUE(id, vinculo_id)
                );
                CREATE TABLE IF NOT EXISTS mensagens (
                    id TEXT PRIMARY KEY, chat_id TEXT NOT NULL, vinculo_id TEXT NOT NULL,
                    remetente_id TEXT NOT NULL, remetente_nome TEXT NOT NULL, conteudo TEXT NOT NULL,
                    envio_utc INTEGER NOT NULL, lida INTEGER NOT NULL DEFAULT 0, leitura_utc INTEGER NULL,
                    FOREIGN KEY(chat_id, vinculo_id) REFERENCES chats(id, vinculo_id)
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ix_mensagens_vinculo_envio ON mensagens(vinculo_id, envio_utc DESC);
                """;
            await command.ExecuteNonQueryAsync(cancellationToken);
            _schemaCreated = true;
        }
        finally { _schemaLock.Release(); }
    }

    private static DateTime FromTicks(long ticks) => new(ticks, DateTimeKind.Utc);

    public void Dispose() => _schemaLock.Dispose();
}
