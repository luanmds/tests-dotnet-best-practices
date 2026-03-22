
using Microsoft.EntityFrameworkCore;

namespace PointsWallet.Infrastructure.Configurations;

public static class DatabaseExtensions
{
    public static async Task MigrateDatabaseAsync(this PointsWalletDbContext dbContext, CancellationToken cancellationToken)
    {
        var connectionString = dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Connection string 'PointsWalletDb' not found.");

        var options = new DbContextOptionsBuilder<PointsWalletDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        using var context = new PointsWalletDbContext(options);
        await context.Database.EnsureCreatedAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }
}