using MediatR;
using PointsWallet.Api.Requests.Users;
using PointsWallet.Domain.Commands.CreateUser;

namespace PointsWallet.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users")
            .WithName("Users")
            .WithOpenApi();

        group.MapPost("/", CreateUserAsync)
            .WithName("CreateUser")
            .WithOpenApi()
            .Produces<CreateUserResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequireAuthorization();
    }

    private static async Task<IResult> CreateUserAsync(
        CreateUserRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(request.Name, request.Email);
        var userId = await mediator.Send(command, cancellationToken);

        return Results.CreatedAtRoute(
            "CreateUser",
            new { id = userId },
            new CreateUserResponse(userId));
    }
}

public sealed record CreateUserResponse(string UserId);
