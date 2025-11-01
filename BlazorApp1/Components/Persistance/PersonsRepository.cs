using System.Data;
using System.Globalization;
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
        if (File.Exists(DbPath))
            return;

        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync();

        using var createCmd = conn.CreateCommand();
        createCmd.CommandText =
            @"CREATE TABLE IF NOT EXISTS Persons (
                Id         INTEGER PRIMARY KEY AUTOINCREMENT,
                FirstName  TEXT NOT NULL,
                LastName   TEXT NOT NULL,
                Gender     TEXT,
                Birthday   TEXT,
                City       TEXT,
                Country    TEXT
              );";
        await createCmd.ExecuteNonQueryAsync();

        using var countCmd = conn.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM Persons;";
        var count = (long)await countCmd.ExecuteScalarAsync();

        if (count == 0)
        {
            using var insertCmd = conn.CreateCommand();
            insertCmd.CommandText =
                @"INSERT INTO Persons (FirstName, LastName, Gender, Birthday, City, Country) VALUES
                    (@f1, @l1, @g1, @b1, @c1, @co1),
                    (@f2, @l2, @g2, @b2, @c2, @co2);";

            // Alice example
            insertCmd.Parameters.AddWithValue("@f1", "Alice");
            insertCmd.Parameters.AddWithValue("@l1", "Johnson");
            insertCmd.Parameters.AddWithValue("@g1", "Female");
            insertCmd.Parameters.AddWithValue("@b1", "1990-05-12"); // ISO date
            insertCmd.Parameters.AddWithValue("@c1", "Seattle");
            insertCmd.Parameters.AddWithValue("@co1", "USA");

            // Bob example
            insertCmd.Parameters.AddWithValue("@f2", "Bob");
            insertCmd.Parameters.AddWithValue("@l2", "Smith");
            insertCmd.Parameters.AddWithValue("@g2", "Male");
            insertCmd.Parameters.AddWithValue("@b2", "1985-11-03");
            insertCmd.Parameters.AddWithValue("@c2", "London");
            insertCmd.Parameters.AddWithValue("@co2", "UK");

            await insertCmd.ExecuteNonQueryAsync();
        }
    }

    public static async Task<List<PersonEntity>> GetAllAsync()
    {
        var list = new List<PersonEntity>();
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, FirstName, LastName, Gender, Birthday, City, Country FROM Persons ORDER BY Id;";
        using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        while (await reader.ReadAsync())
        {
            var birthday = reader.IsDBNull(4) ? (DateTime?)null
                : DateTime.TryParseExact(reader.GetString(4), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt
                : DateTime.TryParse(reader.GetString(4), out var dt2) ? dt2
                : null;

            list.Add(new PersonEntity
            {
                Id = reader.GetInt32(0),
                FirstName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                LastName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Gender = reader.IsDBNull(3) ? null : reader.GetString(3),
                Birthday = birthday,
                City = reader.IsDBNull(5) ? null : reader.GetString(5),
                Country = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }

        return list;
    }

    public static async Task AddAsync(PersonEntity person)
    {
        using var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Persons (FirstName, LastName, Gender, Birthday, City, Country)
                            VALUES (@first, @last, @gender, @birthday, @city, @country);";
        cmd.Parameters.AddWithValue("@first", person.FirstName);
        cmd.Parameters.AddWithValue("@last", person.LastName);
        cmd.Parameters.AddWithValue("@gender", (object?)person.Gender ?? DBNull.Value);
        // Store birthday as ISO yyyy-MM-dd if present, otherwise null
        cmd.Parameters.AddWithValue("@birthday", person.Birthday.HasValue
            ? person.Birthday.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : (object?)DBNull.Value);
        cmd.Parameters.AddWithValue("@city", (object?)person.City ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@country", (object?)person.Country ?? DBNull.Value);

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