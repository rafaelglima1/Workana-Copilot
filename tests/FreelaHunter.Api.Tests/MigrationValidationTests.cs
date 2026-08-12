using FreelaHunter.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FreelaHunter.Api.Tests;

[Collection("Integration")]
public class MigrationValidationTests
{
    [Fact]
    public async Task ApiTests_UseMigrateAsync_NotEnsureCreated()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        // Force the host to start so DatabaseInitializer runs MigrateAsync
        var response = await client.GetAsync("/health");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        // Open the temp SQLite database directly and verify __EFMigrationsHistory
        var field = typeof(TestWebApplicationFactory)
            .GetField("_dbPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var dbPath = (string)field.GetValue(factory)!;

        var migrations = new List<string>();
        await using (var connection = new SqliteConnection($"Data Source={dbPath};Pooling=False"))
        {
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;";
            var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                migrations.Add(reader.GetString(0));
            }
        }

        Assert.Contains("20260811180709_InitialLocalPersistence", migrations);
    }
}
