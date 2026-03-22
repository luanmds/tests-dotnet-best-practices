using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PointsWallet.Infrastructure;

public class PointsWalletDbContextFactory : IDesignTimeDbContextFactory<PointsWalletDbContext>
{
    public PointsWalletDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PointsWalletDbContext>();
        // Ajuste a connection string conforme necessário para seu ambiente local
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=pointswalletdb;Username=postgres;Password=postgres");
        return new PointsWalletDbContext(optionsBuilder.Options);
    }
}
