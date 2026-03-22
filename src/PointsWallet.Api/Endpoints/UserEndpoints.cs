using MediatR;
using PointsWallet.Api.Requests.Users;
using PointsWallet.Domain.Commands.CreateUser;
using PointsWallet.Domain.Repositories;

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
        
        group.MapGet("/", GetUsersAsync)
            .WithName("GetUsers")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
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

    private static async Task<IResult> GetUsersAsync(
        IUserRepository userRepository, 
        CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        return Results.Ok(users);
    }
}

public sealed record CreateUserResponse(string UserId);
