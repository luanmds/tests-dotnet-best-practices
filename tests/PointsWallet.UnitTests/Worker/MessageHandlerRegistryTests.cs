using FluentAssertions;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PointsWallet.Contracts;
using PointsWallet.Contracts.Messages;
using PointsWallet.Domain.Commands.AddPoints;
using PointsWallet.Worker.Messaging;
using PointsWallet.Worker.Messaging.Mappers;

namespace PointsWallet.UnitTests.Worker;

public sealed class MessageHandlerRegistryTests
{
    [Fact]
    public void Register_WithMessageType_AddsHandler()
    {
        var registry = new MessageHandlerRegistry();

        registry.Register<AddPointsMessage>();

        registry.Handlers.Should().ContainSingle()
            .Which.MessageTypeName.Should().Be(nameof(AddPointsMessage));
    }

    [Fact]
    public void Register_WithMultipleTypes_AddsAllHandlers()
    {
        var registry = new MessageHandlerRegistry();

        registry
            .Register<AddPointsMessage>()
            .Register<FakeMessage>();

        registry.Handlers.Should().HaveCount(2);
        registry.Handlers.Select(h => h.MessageTypeName)
            .Should().BeEquivalentTo([nameof(AddPointsMessage), nameof(FakeMessage)]);
    }

    [Fact]
    public async Task Handler_WhenMessageMatches_ReturnsTrueAndDispatches()
    {
        var registry = new MessageHandlerRegistry().Register<AddPointsMessage>();

        var mediator = Substitute.For<IMediator>();
        var services = new ServiceCollection();
        services.AddScoped<IMessageCommandMapper<AddPointsMessage>, AddPointsMessageMapper>();
        using var serviceProvider = services.BuildServiceProvider();

        object? capturedCommand = null;
        _ = mediator.Send(Arg.Do<object>(cmd => capturedCommand = cmd), Arg.Any<CancellationToken>());

        var message = new AddPointsMessage("w-1", "u-1", 50, "corr-1");
        var context = CreateConsumeContext(message);

        var handler = registry.Handlers[0];
        var result = await handler.TryHandleAsync(context, serviceProvider, mediator, CancellationToken.None);

        result.Should().BeTrue();
        capturedCommand.Should().BeOfType<AddPointsCommand>()
            .Which.Should().BeEquivalentTo(new
            {
                WalletId = "w-1",
                Points = 50L
            }, options => options.Including(c => c.WalletId).Including(c => c.Points));
    }

    [Fact]
    public async Task Handler_WhenMessageDoesNotMatch_ReturnsFalse()
    {
        var registry = new MessageHandlerRegistry().Register<AddPointsMessage>();

        var mediator = Substitute.For<IMediator>();
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();

        var unmatchedMessage = Substitute.For<IMessage>();
        unmatchedMessage.CorrelationId.Returns("corr-1");
        var context = CreateNonMatchingConsumeContext(unmatchedMessage);

        var handler = registry.Handlers[0];
        var result = await handler.TryHandleAsync(context, serviceProvider, mediator, CancellationToken.None);

        result.Should().BeFalse();
        await mediator.DidNotReceive().Send(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    private static ConsumeContext<IMessage> CreateConsumeContext<TMessage>(TMessage message)
        where TMessage : class, IMessage
    {
        var context = Substitute.For<ConsumeContext<IMessage>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);

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

    private static ConsumeContext<IMessage> CreateNonMatchingConsumeContext(IMessage message)
    {
        var context = Substitute.For<ConsumeContext<IMessage>>();
        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    /// <summary>
    /// Fake message type used only for testing multiple registrations.
    /// </summary>
    private sealed record FakeMessage(string CorrelationId) : IMessage;
}
