using Microsoft.Extensions.DependencyInjection;

namespace Minimals.Operations.Tests;

public class OperationServiceExtensionsTests
{
    [Fact]
    public void AddOperations_ShouldRegisterAllOperations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOperations();
        var provider = services.BuildServiceProvider();

        // Assert
        var operation = provider.GetService<IOperation<TestCommand, TestResult>>();
        Assert.NotNull(operation);
        Assert.IsType<TestOperation>(operation);
    }

    [Fact]
    public void AddOperations_ShouldRegisterMultipleOperations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOperations();
        var provider = services.BuildServiceProvider();

        // Assert
        var operation1 = provider.GetService<IOperation<TestCommand, TestResult>>();
        var operation2 = provider.GetService<IOperation<AnotherCommand, AnotherResult>>();

        Assert.NotNull(operation1);
        Assert.NotNull(operation2);
    }

    [Fact]
    public void AddOperations_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddOperations();

        // Assert
        Assert.Same(services, result);
    }

    // Test types
    public record TestCommand(string Value) : IOperationCommand;
    public record TestResult(string Output);

    public class TestOperation : IOperation<TestCommand, TestResult>
    {
        public Task<OperationResult<TestResult>> ExecuteAsync(TestCommand command, CancellationToken? cancellation = null)
        {
            return Task.FromResult(OperationResult<TestResult>.Success(new TestResult(command.Value)));
        }
    }

    public record AnotherCommand(int Value) : IOperationCommand;
    public record AnotherResult(int Output);

    public class AnotherOperation : IOperation<AnotherCommand, AnotherResult>
    {
        public Task<OperationResult<AnotherResult>> ExecuteAsync(AnotherCommand command, CancellationToken? cancellation = null)
        {
            return Task.FromResult(OperationResult<AnotherResult>.Success(new AnotherResult(command.Value)));
        }
    }
}
