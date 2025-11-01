using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace BlazorApp1.Data;

public static class PersonsRepository
{
    private static string DbPath => Path.Combine(AppContext.BaseDirectory, "persons.db");
    private static string ConnectionString => $"Data Source={DbPath}";

    public static async Task InitializeAsync()
    {
        // Ensure the SQLite native bundle is initialized so sqlite3 native library is available
        Batteries_V2.Init();

        // Only initialize (create/seed) the database when the file does not exist.
        // If the file already exists, skip creation/seed work.
        if (File.Exists(DbPath))
            return;

        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync();

        using var createCmd = conn.CreateCommand();
        createCmd.CommandText =
            @"CREATE TABLE IF NOT EXISTS Persons (
                Id   INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL
              );";
        await createCmd.ExecuteNonQueryAsync();

        using var countCmd = conn.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM Persons;";
        var count = (long)await countCmd.ExecuteScalarAsync();

        if (count == 0)
        {
            using var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = "INSERT INTO Persons (Name) VALUES (@n1), (@n2);";
            insertCmd.Parameters.AddWithValue("@n1", "Alice");
            insertCmd.Parameters.AddWithValue("@n2", "Bob");
            await insertCmd.ExecuteNonQueryAsync();
        }
    }

    public static async Task<List<PersonEntity>> GetAllAsync()
    {
        var list = new List<PersonEntity>();
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name FROM Persons ORDER BY Id;";
        using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        while (await reader.ReadAsync())
        {
            list.Add(new PersonEntity
            {
                Id = reader.GetInt32(0),
                Name = reader.IsDBNull(1) ? null : reader.GetString(1)
            });
        }

        return list;
    }

    public static async Task AddAsync(string name)
    {
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Persons (Name) VALUES (@name);";
        cmd.Parameters.AddWithValue("@name", name);
        await cmd.ExecuteNonQueryAsync();
    }

    public static async Task RemoveAsync(int id)
    {
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Persons WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }
}