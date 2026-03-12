using FluentAssertions;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PointsWallet.Contracts;
using PointsWallet.Contracts.Messages;
using PointsWallet.Domain.Commands.AddPoints;
using PointsWallet.Worker.Messaging;
using PointsWallet.Worker.Messaging.Mappers;

namespace PointsWallet.UnitTests.Worker;

public sealed class MessageConsumerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogger<MessageConsumer> _logger = Substitute.For<ILogger<MessageConsumer>>();

    [Fact]
    public async Task Consume_WithRegisteredMessageType_DispatchesCommand()
    {
        var message = new AddPointsMessage("wallet-1", "user-1", 100, "corr-1");
        var registry = new MessageHandlerRegistry().Register<AddPointsMessage>();

        var services = new ServiceCollection();
        services.AddScoped<IMessageCommandMapper<AddPointsMessage>, AddPointsMessageMapper>();
        using var serviceProvider = services.BuildServiceProvider();

        object? capturedCommand = null;
        _ = _mediator.Send(Arg.Do<object>(cmd => capturedCommand = cmd), Arg.Any<CancellationToken>());

        var consumer = new MessageConsumer(registry, serviceProvider, _mediator, _logger);
        var context = CreateConsumeContext(message);

        await consumer.Consume(context);

        capturedCommand.Should().BeOfType<AddPointsCommand>()
            .Which.Should().BeEquivalentTo(new
            {
                WalletId = "wallet-1",
                UserId = "user-1",
                Points = 100L,
                CorrelationId = "corr-1"
            });
    }

    [Fact]
    public async Task Consume_WithNoMatchingHandler_DoesNotDispatchCommand()
    {
        var registry = new MessageHandlerRegistry();

        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var consumer = new MessageConsumer(registry, serviceProvider, _mediator, _logger);

        var message = Substitute.For<IMessage>();
        message.CorrelationId.Returns("corr-1");
        var context = CreateConsumeContext(message);

        await consumer.Consume(context);

        await _mediator.DidNotReceive().Send(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WhenMapperThrows_PropagatesException()
    {
        var message = new AddPointsMessage("wallet-1", "user-1", 100, "corr-1");
        var registry = new MessageHandlerRegistry().Register<AddPointsMessage>();

        var failingMapper = Substitute.For<IMessageCommandMapper<AddPointsMessage>>();
        failingMapper.ToCommand(Arg.Any<AddPointsMessage>())
            .Throws(new InvalidOperationException("Mapping failed"));

        var services = new ServiceCollection();
        services.AddScoped<IMessageCommandMapper<AddPointsMessage>>(_ => failingMapper);
        using var serviceProvider = services.BuildServiceProvider();

        var consumer = new MessageConsumer(registry, serviceProvider, _mediator, _logger);
        var context = CreateConsumeContext(message);

        var act = () => consumer.Consume(context);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Mapping failed*");
    }

    [Fact]
    public async Task Consume_WhenMediatorThrows_PropagatesException()
    {
        var message = new AddPointsMessage("wallet-1", "user-1", 100, "corr-1");
        var registry = new MessageHandlerRegistry().Register<AddPointsMessage>();

        var services = new ServiceCollection();
        services.AddScoped<IMessageCommandMapper<AddPointsMessage>, AddPointsMessageMapper>();
        using var serviceProvider = services.BuildServiceProvider();

        _mediator.Send(Arg.Any<object>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Send failed"));

        var consumer = new MessageConsumer(registry, serviceProvider, _mediator, _logger);
        var context = CreateConsumeContext(message);

        var act = () => consumer.Consume(context);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*Send failed*");
    }

    /// <summary>
    /// Creates a mock <see cref="ConsumeContext{T}"/> that supports
    /// <c>TryGetMessage&lt;TMessage&gt;()</c> for the given concrete message.
    /// </summary>
    private static ConsumeContext<IMessage> CreateConsumeContext<TMessage>(TMessage message)
        where TMessage : class, IMessage
    {
        var context = Substitute.For<ConsumeContext<IMessage>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);
        context.SupportedMessageTypes.Returns(
            [MessageUrn.ForTypeString<TMessage>()]);

        context.TryGetMessage(out Arg.Any<ConsumeContext<TMessage>>()!)
            .Returns(callInfo =>
            {
                var typedContext = Substitute.For<ConsumeContext<TMessage>>();
                typedContext.Message.Returns(message);
                typedContext.CancellationToken.Returns(CancellationToken.None);
                callInfo[0] = typedContext;
                return true;
            });

        return context;
    }
}
