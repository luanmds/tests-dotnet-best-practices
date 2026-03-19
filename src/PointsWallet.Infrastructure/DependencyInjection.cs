using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PointsWallet.Domain.Repositories;
using PointsWallet.Infrastructure.Repositories;

namespace PointsWallet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("pointswalletdb")
            ?? throw new InvalidOperationException("Connection string 'pointswalletdb' not found.");

        services.AddDbContext<PointsWalletDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        
        return services;
    }
}
