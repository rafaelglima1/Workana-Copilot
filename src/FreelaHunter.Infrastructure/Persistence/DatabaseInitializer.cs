using Microsoft.EntityFrameworkCore;

namespace FreelaHunter.Infrastructure.Persistence;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly FreelaHunterDbContext _context;

    public DatabaseInitializer(FreelaHunterDbContext context)
    {
        _context = context;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await _context.Database.MigrateAsync(cancellationToken);
    }
}
