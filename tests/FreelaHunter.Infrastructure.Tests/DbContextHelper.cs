using FreelaHunter.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FreelaHunter.Infrastructure.Tests;

public static class DbContextHelper
{
    public static (FreelaHunterDbContext context, SqliteConnection connection) CreateInMemoryContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<FreelaHunterDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new FreelaHunterDbContext(options);
        context.Database.EnsureCreated();

        return (context, connection);
    }
}
