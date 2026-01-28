namespace Minimals.Operations.Tests;

public class OperationResultExtensionsTests
{
    [Fact]
    public void Map_WhenSucceeded_ShouldTransformValue()
    {
        // Arrange
        var result = OperationResult<int>.Success(42);

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        Assert.True(mapped.Succeeded);
        Assert.Equal("42", mapped.Value);
    }

    [Fact]
    public void Map_WhenFailed_ShouldReturnFailureWithoutMapping()
    {
        // Arrange
        var result = OperationResult<int>.ValidationFailure("Error");

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        Assert.False(mapped.Succeeded);
        Assert.Equal(OperationStatus.Invalid, mapped.Status);
        Assert.Null(mapped.Value);
    }

    [Fact]
    public void Map_WhenValueIsNull_ShouldNotCallMapper()
    {
        // Arrange
        var result = new OperationResult<string>(OperationStatus.Completed, null);
        var mapperCalled = false;

        // Act
        var mapped = result.Map(x =>
        {
            mapperCalled = true;
            return x?.Length ?? 0;
        });

        // Assert
        Assert.False(mapperCalled);
        Assert.Equal(0, mapped.Value);
    }

    [Fact]
    public void Bind_WhenSucceeded_ShouldChainOperation()
    {
        // Arrange
        var result = OperationResult<int>.Success(10);

        // Act
        var bound = result.Bind(x => OperationResult<string>.Success($"Value: {x}"));

        // Assert
        Assert.True(bound.Succeeded);
        Assert.Equal("Value: 10", bound.Value);
    }

    [Fact]
    public void Bind_WhenFailed_ShouldNotExecuteBinder()
    {
        // Arrange
        var result = OperationResult<int>.NotFoundFailure("Not found");
        var binderCalled = false;

        // Act
        var bound = result.Bind(x =>
        {
            binderCalled = true;
            return OperationResult<string>.Success("Should not reach");
        });

        // Assert
        Assert.False(binderCalled);
        Assert.False(bound.Succeeded);
        Assert.Equal(OperationStatus.NotFound, bound.Status);
    }

    [Fact]
    public void Bind_WhenValueIsNull_ShouldNotExecuteBinder()
    {
        // Arrange
        var result = new OperationResult<string>(OperationStatus.Completed, null);
        var binderCalled = false;

        // Act
        var bound = result.Bind(x =>
        {
            binderCalled = true;
            return OperationResult<int>.Success(42);
        });

        // Assert
        Assert.False(binderCalled);
    }

    [Fact]
    public async Task BindAsync_WhenSucceeded_ShouldChainAsyncOperation()
    {
        // Arrange
        var result = OperationResult<int>.Success(20);

        // Act
        var bound = await result.BindAsync(async x =>
        {
            await Task.Delay(1);
            return OperationResult<string>.Success($"Async: {x}");
        });

        // Assert
        Assert.True(bound.Succeeded);
        Assert.Equal("Async: 20", bound.Value);
    }

    [Fact]
    public async Task BindAsync_WhenFailed_ShouldNotExecuteBinder()
    {
        // Arrange
        var result = OperationResult<int>.Failure("Error");
        var binderCalled = false;

        // Act
        var bound = await result.BindAsync(async x =>
        {
            binderCalled = true;
            await Task.Delay(1);
            return OperationResult<string>.Success("Should not reach");
        });

        // Assert
        Assert.False(binderCalled);
        Assert.False(bound.Succeeded);
        Assert.Equal(OperationStatus.Failed, bound.Status);
    }

    [Fact]
    public void Match_WhenSucceeded_ShouldCallOnSuccess()
    {
        // Arrange
        var result = OperationResult<string>.Success("test");

        // Act
        var output = result.Match(
            onSuccess: value => $"Success: {value}",
            onFailure: (status, error) => "Failure"
        );

        // Assert
        Assert.Equal("Success: test", output);
    }

    [Fact]
    public void Match_WhenFailed_ShouldCallOnFailure()
    {
        // Arrange
        var result = OperationResult<string>.ValidationFailure("Invalid");

        // Act
        var output = result.Match(
            onSuccess: value => "Success",
            onFailure: (status, error) => $"Failure: {status}"
        );

        // Assert
        Assert.Equal("Failure: Invalid", output);
    }

    [Fact]
    public void MatchDetailed_WithCompleted_ShouldCallOnCompleted()
    {
        // Arrange
        var result = OperationResult<string>.Success("value");

        // Act
        var output = result.MatchDetailed(
            onCompleted: v => $"Completed: {v}",
            onNoOperation: () => "NoOp",
            onInvalid: e => "Invalid",
            onNotFound: e => "NotFound",
            onUnauthorized: e => "Unauthorized",
            onUnprocessable: e => "Unprocessable",
            onFailed: e => "Failed"
        );

        // Assert
        Assert.Equal("Completed: value", output);
    }

    [Fact]
    public void MatchDetailed_WithNoOperation_ShouldCallOnNoOperation()
    {
        // Arrange
        var result = OperationResult<string>.NoOperation("ignored");

        // Act
        var output = result.MatchDetailed(
            onCompleted: v => "Completed",
            onNoOperation: () => "NoOp",
            onInvalid: e => "Invalid",
            onNotFound: e => "NotFound",
            onUnauthorized: e => "Unauthorized",
            onUnprocessable: e => "Unprocessable",
            onFailed: e => "Failed"
        );

        // Assert
        Assert.Equal("NoOp", output);
    }

    [Fact]
    public void MatchDetailed_WithInvalid_ShouldCallOnInvalid()
    {
        // Arrange
        var result = OperationResult<string>.ValidationFailure("error");

        // Act
        var output = result.MatchDetailed(
            onCompleted: v => "Completed",
            onNoOperation: () => "NoOp",
            onInvalid: e => $"Invalid: {e?.Messages[0]}",
            onNotFound: e => "NotFound",
            onUnauthorized: e => "Unauthorized",
            onUnprocessable: e => "Unprocessable",
            onFailed: e => "Failed"
        );

        // Assert
        Assert.Equal("Invalid: error", output);
    }

    [Fact]
    public void MatchDetailed_WithNotFound_ShouldCallOnNotFound()
    {
        // Arrange
        var result = OperationResult<string>.NotFoundFailure("not found");

        // Act
        var output = result.MatchDetailed(
            onCompleted: v => "Completed",
            onNoOperation: () => "NoOp",
            onInvalid: e => "Invalid",
            onNotFound: e => "NotFound",
            onUnauthorized: e => "Unauthorized",
            onUnprocessable: e => "Unprocessable",
            onFailed: e => "Failed"
        );

        // Assert
        Assert.Equal("NotFound", output);
    }

    [Fact]
    public void MatchDetailed_WithUnauthorized_ShouldCallOnUnauthorized()
    {
        // Arrange
        var result = OperationResult<string>.AuthorizationFailure("unauthorized");

        // Act
        var output = result.MatchDetailed(
            onCompleted: v => "Completed",
            onNoOperation: () => "NoOp",
            onInvalid: e => "Invalid",
            onNotFound: e => "NotFound",
            onUnauthorized: e => "Unauthorized",
            onUnprocessable: e => "Unprocessable",
            onFailed: e => "Failed"
        );

        // Assert
        Assert.Equal("Unauthorized", output);
    }

    [Fact]
    public void MatchDetailed_WithUnprocessable_ShouldCallOnUnprocessable()
    {
        // Arrange
        var result = OperationResult<string>.UnprocessableFailure("unprocessable");

        // Act
        var output = result.MatchDetailed(
            onCompleted: v => "Completed",
            onNoOperation: () => "NoOp",
            onInvalid: e => "Invalid",
            onNotFound: e => "NotFound",
            onUnauthorized: e => "Unauthorized",
            onUnprocessable: e => "Unprocessable",
            onFailed: e => "Failed"
        );

        // Assert
        Assert.Equal("Unprocessable", output);
    }

    [Fact]
    public void MatchDetailed_WithFailed_ShouldCallOnFailed()
    {
        // Arrange
        var result = OperationResult<string>.Failure("error");

        // Act
        var output = result.MatchDetailed(
            onCompleted: v => "Completed",
            onNoOperation: () => "NoOp",
            onInvalid: e => "Invalid",
            onNotFound: e => "NotFound",
            onUnauthorized: e => "Unauthorized",
            onUnprocessable: e => "Unprocessable",
            onFailed: e => "Failed"
        );

        // Assert
        Assert.Equal("Failed", output);
    }

    [Fact]
    public void MatchDetailed_WithInvalidStatus_ShouldThrowException()
    {
        // Arrange - Create a result with an invalid status (outside enum range)
        var result = new OperationResult<string>((OperationStatus)999, null);

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.MatchDetailed(
                onCompleted: v => "Completed",
                onNoOperation: () => "NoOp",
                onInvalid: e => "Invalid",
                onNotFound: e => "NotFound",
                onUnauthorized: e => "Unauthorized",
                onUnprocessable: e => "Unprocessable",
                onFailed: e => "Failed"
            )
        );

        Assert.Contains("Unknown operation status", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OnSuccess_WhenSucceeded_ShouldExecuteAction()
    {
        // Arrange
        var result = OperationResult<string>.Success("test");
        var actionCalled = false;
        string capturedValue = null!;

        // Act
        var returned = result.OnSuccess(value =>
        {
            actionCalled = true;
            capturedValue = value;
        });

        // Assert
        Assert.True(actionCalled);
        Assert.Equal("test", capturedValue);
        Assert.Same(result, returned);
    }

    [Fact]
    public void OnSuccess_WhenFailed_ShouldNotExecuteAction()
    {
        // Arrange
        var result = OperationResult<string>.Failure("error");
        var actionCalled = false;

        // Act
        var returned = result.OnSuccess(value => actionCalled = true);

        // Assert
        Assert.False(actionCalled);
        Assert.Same(result, returned);
    }

    [Fact]
    public void OnFailure_WhenFailed_ShouldExecuteAction()
    {
        // Arrange
        var result = OperationResult<string>.ValidationFailure("error");
        var actionCalled = false;
        OperationStatus capturedStatus = default;
        OperationError capturedError = null!;

        // Act
        var returned = result.OnFailure((status, error) =>
        {
            actionCalled = true;
            capturedStatus = status;
            capturedError = error;
        });

        // Assert
        Assert.True(actionCalled);
        Assert.Equal(OperationStatus.Invalid, capturedStatus);
        Assert.NotNull(capturedError);
        Assert.Same(result, returned);
    }

    [Fact]
    public void OnFailure_WhenSucceeded_ShouldNotExecuteAction()
    {
        // Arrange
        var result = OperationResult<string>.Success("test");
        var actionCalled = false;

        // Act
        var returned = result.OnFailure((status, error) => actionCalled = true);

        // Assert
        Assert.False(actionCalled);
        Assert.Same(result, returned);
    }
}
