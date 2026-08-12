using FreelaHunter.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FreelaHunter.Infrastructure.Tests;

public class MigrationTests
{
    [Fact]
    public async Task MigrateAsync_AppliesSchemaSuccessfully()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<FreelaHunterDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new FreelaHunterDbContext(options);
        await context.Database.MigrateAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
        var reader = await command.ExecuteReaderAsync();
        var tables = new List<string>();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        Assert.Contains("Opportunities", tables);
        Assert.Contains("OpportunitySkills", tables);
        Assert.Contains("FreelancerProfiles", tables);
        Assert.Contains("FreelancerProfileSkills", tables);
        Assert.Contains("FreelancerHourlyRates", tables);

        await connection.CloseAsync();
    }
}
