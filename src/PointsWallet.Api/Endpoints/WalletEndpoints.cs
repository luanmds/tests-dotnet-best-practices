using MassTransit;
using MediatR;
using PointsWallet.Api.Requests.Wallets;
using PointsWallet.Contracts.Messages;
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

        group.MapPost("/{userId}/wallets/{walletId}/points", AddPointsAsync)
            .WithName("AddPoints")
            .WithOpenApi()
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
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

    private static async Task<IResult> AddPointsAsync(
        string userId,
        string walletId,
        AddPointsRequest request,
        IPublishEndpoint publishEndpoint,
        CancellationToken cancellationToken)
    {
        if (request.Points <= 0)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Points must be greater than zero");
        }

        var message = new AddPointsMessage(
            walletId,
            userId,
            request.Points,
            Guid.NewGuid().ToString());

        await publishEndpoint.Publish(message, cancellationToken);

        return Results.Accepted();
    }
}

public sealed record CreateWalletResponse(string WalletId);
