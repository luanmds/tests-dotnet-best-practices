using Microsoft.Extensions.DependencyInjection;
using PointsWallet.Domain.Repositories;
using PointsWallet.Infrastructure.Repositories;

namespace PointsWallet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<ApplicationDbContext>();
        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }
}
