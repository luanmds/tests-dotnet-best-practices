using PointsWallet.Contracts;

namespace PointsWallet.Worker.Messaging;

/// <summary>
/// Maps a message received from the broker into a MediatR command.
/// Implement this interface for each message type to define how it translates into a domain command.
/// </summary>
/// <typeparam name="TMessage">The message type consumed from the broker.</typeparam>
public interface IMessageCommandMapper<in TMessage> where TMessage : class, IMessage
{
    /// <summary>
    /// Converts the broker message into a MediatR command (IRequest).
    /// </summary>
    /// <param name="message">The incoming broker message.</param>
    /// <returns>A MediatR request object to be dispatched via <c>ISender.Send</c>.</returns>
    object ToCommand(TMessage message);
}
