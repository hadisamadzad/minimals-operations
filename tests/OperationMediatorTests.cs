using Microsoft.Extensions.DependencyInjection;

namespace Minimals.Operations.Tests;

public class OperationMediatorTests
{
    [Fact]
    public void GeneratedLazyDispatcher_ShouldNotConstructOperationWhenResolved()
    {
        DispatcherOperation.ConstructionCount = 0;
        var services = new ServiceCollection();
        services.AddOperations();

        using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IOperationMediator>();

        Assert.Equal(0, DispatcherOperation.ConstructionCount);
    }

    [Fact]
    public async Task GeneratedLazyDispatcher_ShouldExecuteRequest()
    {
        var services = new ServiceCollection();
        services.AddOperations();

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IOperationMediator>();

        var result = await dispatcher.ExecuteAsync(new DispatcherRequest("lazy"));

        Assert.True(result.Succeeded);
        Assert.Equal("lazy", result.Value);
    }

    [Fact]
    public async Task GeneratedMediator_ShouldExecuteCommandWithoutResultValue()
    {
        var services = new ServiceCollection();
        services.AddOperations();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IOperationMediator>();

        var result = await mediator.ExecuteAsync(new NoResultCommand());

        Assert.True(result.Succeeded);
        Assert.IsType<NoResult>(result.Value);
    }

    [Fact]
    public async Task GeneratedMediator_ShouldPublishEventToAllHandlers()
    {
        PublishedEventHandler.PublishedValues.Clear();
        var services = new ServiceCollection();
        services.AddOperations();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IOperationMediator>();

        await mediator.PublishAsync(new DispatcherEvent("published"));

        Assert.Equal(
            ["first:published", "second:published"],
            PublishedEventHandler.PublishedValues);
    }

    [Fact]
    public void GeneratedMediator_ShouldNotConstructEventOperationsWhenResolved()
    {
        FirstEventOperation.ConstructionCount = 0;
        SecondEventOperation.ConstructionCount = 0;
        var services = new ServiceCollection();
        services.AddOperations();

        using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IOperationMediator>();

        Assert.Equal(0, FirstEventOperation.ConstructionCount);
        Assert.Equal(0, SecondEventOperation.ConstructionCount);
    }

    [Fact]
    public async Task GeneratedMediator_ShouldRejectEventWithoutGeneratedOperation()
    {
        var services = new ServiceCollection();
        services.AddOperations();

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IOperationMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.PublishAsync(new UnhandledEvent()));
    }

    public sealed record DispatcherRequest(string Value) : IOperationCommand<string>;

    public sealed class DispatcherOperation : IOperation<DispatcherRequest, string>
    {
        public static int ConstructionCount;

        public DispatcherOperation()
        {
            Interlocked.Increment(ref ConstructionCount);
        }

        public Task<OperationResult<string>> ExecuteAsync(
            DispatcherRequest command,
            CancellationToken? cancellation = null)
        {
            return Task.FromResult(OperationResult<string>.Success(command.Value));
        }
    }

    public sealed record NoResultCommand : IOperationCommand<NoResult>;

    public sealed class NoResultOperation : IOperation<NoResultCommand, NoResult>
    {
        public Task<OperationResult<NoResult>> ExecuteAsync(
            NoResultCommand command,
            CancellationToken? cancellation = null)
        {
            return Task.FromResult(OperationResult<NoResult>.Success());
        }
    }

    public sealed record DispatcherEvent(string Value) : IOperationEvent;

    public sealed class FirstEventOperation : IOperation<DispatcherEvent>
    {
        public static int ConstructionCount;

        public FirstEventOperation()
        {
            Interlocked.Increment(ref ConstructionCount);
        }

        public Task ExecuteAsync(DispatcherEvent @event, CancellationToken? cancellation = null)
        {
            PublishedEventHandler.PublishedValues.Add($"first:{@event.Value}");
            return Task.CompletedTask;
        }
    }

    public sealed class SecondEventOperation : IOperation<DispatcherEvent>
    {
        public static int ConstructionCount;

        public SecondEventOperation()
        {
            Interlocked.Increment(ref ConstructionCount);
        }

        public Task ExecuteAsync(DispatcherEvent @event, CancellationToken? cancellation = null)
        {
            PublishedEventHandler.PublishedValues.Add($"second:{@event.Value}");
            return Task.CompletedTask;
        }
    }

    public sealed record UnhandledEvent : IOperationEvent;

    private static class PublishedEventHandler
    {
        public static List<string> PublishedValues { get; } = [];
    }
}
