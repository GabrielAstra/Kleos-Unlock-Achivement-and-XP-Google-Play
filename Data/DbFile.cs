using Microsoft.Data.Sqlite;
using Kleos.Models;
using System.IO;

namespace Kleos.Data;

public class DbFile : IDisposable
{
    private readonly SqliteConnection _connection;

    public DbFile(string path)
    {
        var walPath = path + "-wal";
        var shmPath = path + "-shm";

        if (File.Exists(walPath) && new FileInfo(walPath).Length > 0)
        {
            var connStr = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadWrite
            }.ToString();

            using (var mergeConn = new SqliteConnection(connStr))
            {
                mergeConn.Open();
                using var cmd = mergeConn.CreateCommand();
                cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE)";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "PRAGMA journal_mode=DELETE";
                cmd.ExecuteNonQuery();
            }

            if (File.Exists(walPath)) File.Delete(walPath);
            if (File.Exists(shmPath)) File.Delete(shmPath);
        }

        _connection = new SqliteConnection($"Data Source={path}");
        _connection.Open();
    }

    private SqliteCommand Cmd(string sql)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        return cmd;
    }

    public List<Game> GetGames()
    {
        var games = new List<Game>();
        using var cmd = Cmd("SELECT _id, external_game_id, display_name, developer_name, achievement_total_count FROM games ORDER BY _id");
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            games.Add(new Game
            {
                Id = reader.GetInt32(0),
                ExternalGameId = reader.IsDBNull(1) ? "" : reader.GetString(1),
                DisplayName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                DeveloperName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                AchievementTotalCount = reader.IsDBNull(4) ? 0 : reader.GetInt32(4)
            });
        return games;
    }

    public GameInstance? GetGameInstance(int gameId)
    {
        using var cmd = Cmd($"SELECT _id, instance_game_id, package_name, installed FROM game_instances WHERE instance_game_id={gameId} AND installed=1 LIMIT 1");
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new GameInstance
        {
            Id = reader.GetInt32(0),
            InstanceGameId = reader.GetInt32(1),
            PackageName = reader.IsDBNull(2) ? "" : reader.GetString(2),
            Installed = reader.GetInt32(3)
        };
    }

    public List<AchievementDefinition> GetAchievementDefinitions(int gameId)
    {
        var list = new List<AchievementDefinition>();
        using var cmd = Cmd($"SELECT _id, game_id, external_achievement_id, type, name, description, total_steps, initial_state, definition_xp_value, rarity_percent FROM achievement_definitions WHERE game_id={gameId} ORDER BY _id");
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(new AchievementDefinition
            {
                Id = reader.GetInt32(0),
                GameId = reader.GetInt32(1),
                ExternalAchievementId = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Type = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                Name = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Description = reader.IsDBNull(5) ? "" : reader.GetString(5),
                TotalSteps = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                InitialState = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                DefinitionXpValue = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                RarityPercent = reader.IsDBNull(9) ? 0 : reader.GetDouble(9)
            });
        return list;
    }

    public AchievementDefinition? GetAchievementDefinitionByExternalId(string externalId)
    {
        using var cmd = Cmd($"SELECT _id, game_id, external_achievement_id, type, name, description, total_steps, initial_state, definition_xp_value, rarity_percent FROM achievement_definitions WHERE external_achievement_id='{externalId}' LIMIT 1");
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new AchievementDefinition
        {
            Id = reader.GetInt32(0),
            GameId = reader.GetInt32(1),
            ExternalAchievementId = reader.IsDBNull(2) ? "" : reader.GetString(2),
            Type = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
            Name = reader.IsDBNull(4) ? "" : reader.GetString(4),
            Description = reader.IsDBNull(5) ? "" : reader.GetString(5),
            TotalSteps = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
            InitialState = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
            DefinitionXpValue = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
            RarityPercent = reader.IsDBNull(9) ? 0 : reader.GetDouble(9)
        };
    }

    public AchievementInstance? GetAchievementInstance(int definitionId)
    {
        using var cmd = Cmd($"SELECT _id, definition_id, player_id, state, current_steps, formatted_current_steps, last_updated_timestamp, instance_xp_value FROM achievement_instances WHERE definition_id={definitionId} LIMIT 1");
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new AchievementInstance
        {
            Id = reader.GetInt32(0),
            DefinitionId = reader.GetInt32(1),
            PlayerId = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
            State = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
            CurrentSteps = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
            FormattedCurrentSteps = reader.IsDBNull(5) ? "" : reader.GetString(5),
            LastUpdatedTimestamp = reader.IsDBNull(6) ? 0 : reader.GetInt64(6),
            InstanceXpValue = reader.IsDBNull(7) ? 0 : reader.GetInt32(7)
        };
    }

    public List<Player> GetPlayers()
    {
        var list = new List<Player>();
        using var cmd = Cmd("SELECT _id, external_player_id, profile_name, current_level, current_xp_total, total_unlocked_achievements FROM players ORDER BY _id");
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(new Player
            {
                Id = reader.GetInt32(0),
                ExternalPlayerId = reader.IsDBNull(1) ? "" : reader.GetString(1),
                ProfileName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                CurrentLevel = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                CurrentXpTotal = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                TotalUnlockedAchievements = reader.IsDBNull(5) ? 0 : reader.GetInt32(5)
            });
        return list;
    }

    public List<ClientContext> GetClientContexts()
    {
        var list = new List<ClientContext>();
        using var cmd = Cmd("SELECT _id, package_name, package_uid, account_name, account_type, is_games_lite FROM client_contexts ORDER BY _id");
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(new ClientContext
            {
                Id = reader.GetInt32(0),
                PackageName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                PackageUid = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                AccountName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                AccountType = reader.IsDBNull(4) ? "" : reader.GetString(4),
                IsGamesLite = reader.IsDBNull(5) ? 0 : reader.GetInt32(5)
            });
        return list;
    }

    public ClientContext? GetClientContextByPackageName(string packageName)
    {
        using var cmd = Cmd($"SELECT _id, package_name, package_uid, account_name, account_type, is_games_lite FROM client_contexts WHERE package_name='{packageName}' LIMIT 1");
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new ClientContext
        {
            Id = reader.GetInt32(0),
            PackageName = reader.IsDBNull(1) ? "" : reader.GetString(1),
            PackageUid = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
            AccountName = reader.IsDBNull(3) ? "" : reader.GetString(3),
            AccountType = reader.IsDBNull(4) ? "" : reader.GetString(4),
            IsGamesLite = reader.IsDBNull(5) ? 0 : reader.GetInt32(5)
        };
    }

    public List<AchievementPendingOp> GetPendingOps()
    {
        var list = new List<AchievementPendingOp>();
        using var cmd = Cmd("SELECT _id, client_context_id, external_achievement_id, achievement_type, new_state, steps_to_increment, min_steps_to_set, external_game_id, external_player_id FROM achievement_pending_ops ORDER BY _id");
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(new AchievementPendingOp
            {
                Id = reader.GetInt32(0),
                ClientContextId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                ExternalAchievementId = reader.IsDBNull(2) ? "" : reader.GetString(2),
                AchievementType = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                NewState = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                StepsToIncrement = reader.IsDBNull(5) ? "" : reader.GetString(5),
                MinStepsToSet = reader.IsDBNull(6) ? "" : reader.GetString(6),
                ExternalGameId = reader.IsDBNull(7) ? "" : reader.GetString(7),
                ExternalPlayerId = reader.IsDBNull(8) ? "" : reader.GetString(8)
            });
        return list;
    }

    public int GetNextPendingOpId()
    {
        using var cmd = Cmd("SELECT MAX(_id) FROM achievement_pending_ops");
        var result = cmd.ExecuteScalar();
        return result == DBNull.Value || result == null ? 0 : Convert.ToInt32(result) + 1;
    }

    public void AddPendingOp(AchievementPendingOp op)
    {
        using var cmd = Cmd("INSERT INTO achievement_pending_ops VALUES (@id,@ctx,@extId,@type,@state,@steps,@minSteps,@gameId,@playerId)");
        cmd.Parameters.AddWithValue("@id", op.Id);
        cmd.Parameters.AddWithValue("@ctx", op.ClientContextId);
        cmd.Parameters.AddWithValue("@extId", op.ExternalAchievementId);
        cmd.Parameters.AddWithValue("@type", op.AchievementType);
        cmd.Parameters.AddWithValue("@state", op.NewState);
        cmd.Parameters.AddWithValue("@steps", op.StepsToIncrement);
        cmd.Parameters.AddWithValue("@minSteps", op.MinStepsToSet);
        cmd.Parameters.AddWithValue("@gameId", op.ExternalGameId);
        cmd.Parameters.AddWithValue("@playerId", op.ExternalPlayerId);
        cmd.ExecuteNonQuery();
    }

    public void EmptyPendingOps()
    {
        using var cmd = Cmd("DELETE FROM achievement_pending_ops");
        cmd.ExecuteNonQuery();
    }

    public int RemoveDuplicatePendingOps()
    {
        var ops = GetPendingOps();
        var seen = new HashSet<string>();
        int removed = 0;
        foreach (var op in ops)
        {
            if (!seen.Add(op.ExternalAchievementId))
            {
                using var cmd = Cmd($"DELETE FROM achievement_pending_ops WHERE _id={op.Id}");
                cmd.ExecuteNonQuery();
                removed++;
            }
        }
        return removed;
    }

    public void EnsureClientContextsForAllGames()
    {
        var contexts = GetClientContexts();
        var existingPackages = new HashSet<string>(contexts.Select(c => c.PackageName));

        var baseCtx = contexts.FirstOrDefault();
        if (baseCtx == null) return;

        using var maxCmd = Cmd("SELECT MAX(_id) FROM client_contexts");
        var maxResult = maxCmd.ExecuteScalar();
        int nextId = maxResult == DBNull.Value || maxResult == null ? 1 : Convert.ToInt32(maxResult) + 1;

        using var maxUidCmd = Cmd("SELECT MAX(package_uid) FROM client_contexts");
        var maxUidResult = maxUidCmd.ExecuteScalar();
        int nextUid = maxUidResult == DBNull.Value || maxUidResult == null ? 10200 : Convert.ToInt32(maxUidResult) + 1;

        using var cmd = Cmd("SELECT DISTINCT package_name FROM game_instances WHERE installed=1 AND package_name IS NOT NULL AND package_name != ''");
        using var reader = cmd.ExecuteReader();
        var packagesToAdd = new List<string>();
        while (reader.Read())
        {
            var pkg = reader.GetString(0);
            if (!existingPackages.Contains(pkg))
                packagesToAdd.Add(pkg);
        }
        reader.Close();

        foreach (var pkg in packagesToAdd)
        {
            using var insertCmd = Cmd("INSERT INTO client_contexts (_id, package_name, package_uid, account_name, account_type, is_games_lite) VALUES (@id, @pkg, @uid, @acc, @type, 0)");
            insertCmd.Parameters.AddWithValue("@id", nextId++);
            insertCmd.Parameters.AddWithValue("@pkg", pkg);
            insertCmd.Parameters.AddWithValue("@uid", nextUid++);
            insertCmd.Parameters.AddWithValue("@acc", baseCtx.AccountName);
            insertCmd.Parameters.AddWithValue("@type", baseCtx.AccountType);
            insertCmd.ExecuteNonQuery();
        }

        var knownMissingPackages = new Dictionary<string, int>
        {
            { "com.rovio.baba", 10155 },
            { "com.playgendary.tom", 10154 },
        };

        foreach (var kvp in knownMissingPackages)
        {
            if (!existingPackages.Contains(kvp.Key))
            {
                using var insertCmd = Cmd("INSERT OR IGNORE INTO client_contexts (_id, package_name, package_uid, account_name, account_type, is_games_lite) VALUES (@id, @pkg, @uid, @acc, @type, 0)");
                insertCmd.Parameters.AddWithValue("@id", nextId++);
                insertCmd.Parameters.AddWithValue("@pkg", kvp.Key);
                insertCmd.Parameters.AddWithValue("@uid", kvp.Value);
                insertCmd.Parameters.AddWithValue("@acc", baseCtx.AccountName);
                insertCmd.Parameters.AddWithValue("@type", baseCtx.AccountType);
                insertCmd.ExecuteNonQuery();
            }
        }
    }

    public void FlushAndClose()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "PRAGMA journal_mode=DELETE";
        cmd.ExecuteNonQuery();
        _connection.Close();
    }

    public void Dispose() => _connection.Dispose();
}