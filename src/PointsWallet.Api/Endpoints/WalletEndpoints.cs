using MediatR;
using PointsWallet.Api.Requests.Wallets;
using PointsWallet.Domain.Commands.CreateWallet;

namespace PointsWallet.Api.Endpoints;

public static class WalletEndpoints
{
    public static void MapWalletEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users")
            .WithName("Wallets")
            .WithOpenApi();

        group.MapPost("/{userId}/wallets", CreateWalletAsync)
            .WithName("CreateWallet")
            .WithOpenApi()
            .Produces<CreateWalletResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization();
    }

    private static async Task<IResult> CreateWalletAsync(
        string userId,
        CreateWalletRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateWalletCommand(userId, request.SymbolicName);
            var walletId = await mediator.Send(command, cancellationToken);

            return Results.Created($"/api/users/{userId}/wallets/{walletId}", new CreateWalletResponse(walletId));
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: exception.Message);
        }
    }
}

public sealed record CreateWalletResponse(string WalletId);
