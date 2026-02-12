using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PointsWallet.Domain.Commands.CreateUser;

namespace PointsWallet.Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateUserCommandValidator>();
        return services;
    }
}
