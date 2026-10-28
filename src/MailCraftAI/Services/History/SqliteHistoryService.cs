using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MailCraftAI.Models;
using Microsoft.Data.Sqlite;

namespace MailCraftAI.Services.History;

public class SqliteHistoryService : IHistoryService
{
    private readonly string _connectionString;
    private bool _initialized;
    private readonly object _initLock = new();

    public SqliteHistoryService(string? customDbPath = null)
    {
        string dbPath;
        if (!string.IsNullOrEmpty(customDbPath))
        {
            dbPath = customDbPath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "MailCraftAI");
            Directory.CreateDirectory(folder);
            dbPath = Path.Combine(folder, "history.db");
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string createSql = @"
            CREATE TABLE IF NOT EXISTS HistoryRecords (
                Id TEXT PRIMARY KEY,
                Timestamp TEXT NOT NULL,
                SourceType TEXT NOT NULL,
                OriginalSubject TEXT,
                OriginalSender TEXT,
                OriginalSummary TEXT,
                UserInstruction TEXT,
                Tone TEXT,
                ReplyLength TEXT,
                Language TEXT,
                ProviderName TEXT,
                ModelName TEXT,
                GeneratedSubject TEXT,
                GeneratedReply TEXT,
                IsFavorite INTEGER NOT NULL DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS IX_History_Timestamp ON HistoryRecords(Timestamp DESC);
        ";

        await using var cmd = new SqliteCommand(createSql, conn);
        await cmd.ExecuteNonQueryAsync();

        lock (_initLock)
        {
            _initialized = true;
        }
    }

    public async Task AddRecordAsync(HistoryRecord record)
    {
        await InitializeAsync();

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string insertSql = @"
            INSERT OR REPLACE INTO HistoryRecords (
                Id, Timestamp, SourceType, OriginalSubject, OriginalSender, OriginalSummary,
                UserInstruction, Tone, ReplyLength, Language, ProviderName, ModelName,
                GeneratedSubject, GeneratedReply, IsFavorite
            ) VALUES (
                @Id, @Timestamp, @SourceType, @OriginalSubject, @OriginalSender, @OriginalSummary,
                @UserInstruction, @Tone, @ReplyLength, @Language, @ProviderName, @ModelName,
                @GeneratedSubject, @GeneratedReply, @IsFavorite
            );
        ";

        await using var cmd = new SqliteCommand(insertSql, conn);
        cmd.Parameters.AddWithValue("@Id", record.Id);
        cmd.Parameters.AddWithValue("@Timestamp", record.Timestamp.ToString("o"));
        cmd.Parameters.AddWithValue("@SourceType", record.SourceType ?? "");
        cmd.Parameters.AddWithValue("@OriginalSubject", record.OriginalSubject ?? "");
        cmd.Parameters.AddWithValue("@OriginalSender", record.OriginalSender ?? "");
        cmd.Parameters.AddWithValue("@OriginalSummary", record.OriginalSummary ?? "");
        cmd.Parameters.AddWithValue("@UserInstruction", record.UserInstruction ?? "");
        cmd.Parameters.AddWithValue("@Tone", record.Tone ?? "");
        cmd.Parameters.AddWithValue("@ReplyLength", record.ReplyLength ?? "");
        cmd.Parameters.AddWithValue("@Language", record.Language ?? "");
        cmd.Parameters.AddWithValue("@ProviderName", record.ProviderName ?? "");
        cmd.Parameters.AddWithValue("@ModelName", record.ModelName ?? "");
        cmd.Parameters.AddWithValue("@GeneratedSubject", record.GeneratedSubject ?? "");
        cmd.Parameters.AddWithValue("@GeneratedReply", record.GeneratedReply ?? "");
        cmd.Parameters.AddWithValue("@IsFavorite", record.IsFavorite ? 1 : 0);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<HistoryRecord>> GetRecentRecordsAsync(int maxCount = 100, string? searchKeyword = null)
    {
        await InitializeAsync();

        var list = new List<HistoryRecord>();
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql;
        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            sql = @"
                SELECT Id, Timestamp, SourceType, OriginalSubject, OriginalSender, OriginalSummary,
                       UserInstruction, Tone, ReplyLength, Language, ProviderName, ModelName,
                       GeneratedSubject, GeneratedReply, IsFavorite
                FROM HistoryRecords
                WHERE OriginalSubject LIKE @Search 
                   OR OriginalSender LIKE @Search
                   OR UserInstruction LIKE @Search
                   OR GeneratedReply LIKE @Search
                ORDER BY IsFavorite DESC, Timestamp DESC
                LIMIT @Limit;
            ";
        }
        else
        {
            sql = @"
                SELECT Id, Timestamp, SourceType, OriginalSubject, OriginalSender, OriginalSummary,
                       UserInstruction, Tone, ReplyLength, Language, ProviderName, ModelName,
                       GeneratedSubject, GeneratedReply, IsFavorite
                FROM HistoryRecords
                ORDER BY IsFavorite DESC, Timestamp DESC
                LIMIT @Limit;
            ";
        }

        await using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Limit", maxCount);
        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            cmd.Parameters.AddWithValue("@Search", $"%{searchKeyword.Trim()}%");
        }

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var record = new HistoryRecord
            {
                Id = reader.GetString(0),
                Timestamp = DateTimeOffset.TryParse(reader.GetString(1), out var dto) ? dto : DateTimeOffset.UtcNow,
                SourceType = reader.IsDBNull(2) ? "" : reader.GetString(2),
                OriginalSubject = reader.IsDBNull(3) ? "" : reader.GetString(3),
                OriginalSender = reader.IsDBNull(4) ? "" : reader.GetString(4),
                OriginalSummary = reader.IsDBNull(5) ? "" : reader.GetString(5),
                UserInstruction = reader.IsDBNull(6) ? "" : reader.GetString(6),
                Tone = reader.IsDBNull(7) ? "" : reader.GetString(7),
                ReplyLength = reader.IsDBNull(8) ? "" : reader.GetString(8),
                Language = reader.IsDBNull(9) ? "" : reader.GetString(9),
                ProviderName = reader.IsDBNull(10) ? "" : reader.GetString(10),
                ModelName = reader.IsDBNull(11) ? "" : reader.GetString(11),
                GeneratedSubject = reader.IsDBNull(12) ? "" : reader.GetString(12),
                GeneratedReply = reader.IsDBNull(13) ? "" : reader.GetString(13),
                IsFavorite = !reader.IsDBNull(14) && reader.GetInt32(14) == 1
            };
            list.Add(record);
        }

        return list;
    }

    public async Task ToggleFavoriteAsync(string recordId)
    {
        await InitializeAsync();

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = "UPDATE HistoryRecords SET IsFavorite = CASE WHEN IsFavorite = 1 THEN 0 ELSE 1 END WHERE Id = @Id;";
        await using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", recordId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteRecordAsync(string recordId)
    {
        await InitializeAsync();

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = "DELETE FROM HistoryRecords WHERE Id = @Id;";
        await using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", recordId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ClearHistoryAsync()
    {
        await InitializeAsync();

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        string sql = "DELETE FROM HistoryRecords;";
        await using var cmd = new SqliteCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }
}
