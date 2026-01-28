namespace Minimals.Operations.Tests;

public class OperationResultTests
{
    [Fact]
    public void Success_WithValue_ShouldReturnCompletedStatus()
    {
        // Act
        var result = OperationResult<string>.Success("test-value");

        // Assert
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.Equal("test-value", result.Value);
        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Success_WithoutValue_ShouldReturnCompletedStatusWithNoResult()
    {
        // Act
        var result = OperationResult<NoResult>.Success();

        // Assert
        Assert.Equal(OperationStatus.Completed, result.Status);
        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
    }

    [Fact]
    public void NoOperation_WithValue_ShouldReturnNoOperationStatus()
    {
        // Act
        var result = OperationResult<string>.NoOperation("ignored");

        // Assert
        Assert.Equal(OperationStatus.NoOperation, result.Status);
        Assert.Null(result.Value);
        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
    }

    [Fact]
    public void NoOperation_WithoutValue_ShouldReturnNoOperationStatus()
    {
        // Act
        var result = OperationResult<NoResult>.NoOperation();

        // Assert
        Assert.Equal(OperationStatus.NoOperation, result.Status);
        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ValidationFailure_ShouldReturnInvalidStatusWithError()
    {
        // Arrange
        var messages = new[] { "Field is required", "Field must be valid" };

        // Act
        var result = OperationResult<string>.ValidationFailure(messages);

        // Assert
        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.Null(result.Value);
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Equal(OperationErrorType.ValidationError, result.Error.Type);
        Assert.Equal(messages, result.Error.Messages);
    }

    [Fact]
    public void NotFoundFailure_ShouldReturnNotFoundStatusWithError()
    {
        // Arrange
        var message = "Resource not found";

        // Act
        var result = OperationResult<string>.NotFoundFailure(message);

        // Assert
        Assert.Equal(OperationStatus.NotFound, result.Status);
        Assert.Null(result.Value);
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Equal(OperationErrorType.UnexpectedError, result.Error.Type);
        Assert.Single(result.Error.Messages);
        Assert.Equal(message, result.Error.Messages[0]);
    }

    [Fact]
    public void AuthorizationFailure_ShouldReturnUnauthorizedStatusWithError()
    {
        // Arrange
        var message = "Access denied";

        // Act
        var result = OperationResult<string>.AuthorizationFailure(message);

        // Assert
        Assert.Equal(OperationStatus.Unauthorized, result.Status);
        Assert.Null(result.Value);
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Equal(OperationErrorType.AuthorizationError, result.Error.Type);
        Assert.Single(result.Error.Messages);
        Assert.Equal(message, result.Error.Messages[0]);
    }

    [Fact]
    public void Failure_ShouldReturnFailedStatusWithError()
    {
        // Arrange
        var message = "Operation failed";

        // Act
        var result = OperationResult<string>.Failure(message);

        // Assert
        Assert.Equal(OperationStatus.Failed, result.Status);
        Assert.Null(result.Value);
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Equal(OperationErrorType.UnexpectedError, result.Error.Type);
        Assert.Single(result.Error.Messages);
        Assert.Equal(message, result.Error.Messages[0]);
    }

    [Fact]
    public void UnprocessableFailure_ShouldReturnUnprocessableStatusWithError()
    {
        // Arrange
        var message = "Cannot process";

        // Act
        var result = OperationResult<string>.UnprocessableFailure(message);

        // Assert
        Assert.Equal(OperationStatus.Unprocessable, result.Status);
        Assert.Null(result.Value);
        Assert.False(result.Succeeded);
        Assert.True(result.Failed);
        Assert.NotNull(result.Error);
        Assert.Equal(OperationErrorType.UnexpectedError, result.Error.Type);
        Assert.Single(result.Error.Messages);
        Assert.Equal(message, result.Error.Messages[0]);
    }

    [Fact]
    public void Failed_Property_ShouldReturnTrueForFailures()
    {
        // Arrange
        var successResult = OperationResult<string>.Success("value");
        var failedResult = OperationResult<string>.Failure("error");

        // Assert
        Assert.False(successResult.Failed);
        Assert.True(failedResult.Failed);
    }

    [Fact]
    public void Succeeded_ShouldReturnTrueForCompletedAndNoOperation()
    {
        // Arrange
        var completedResult = OperationResult<string>.Success("value");
        var noOperationResult = OperationResult<string>.NoOperation("value");
        var failedResult = OperationResult<string>.Failure("error");

        // Assert
        Assert.True(completedResult.Succeeded);
        Assert.True(noOperationResult.Succeeded);
        Assert.False(failedResult.Succeeded);
    }

    [Fact]
    public void OperationResult_WithMetadata_ShouldStoreMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        // Act
        var result = new OperationResult<string>(
            OperationStatus.Completed,
            "test",
            null,
            metadata);

        // Assert
        Assert.Equal(metadata, result.Metadata);
        Assert.Equal("value1", result.Metadata["key1"]);
        Assert.Equal("value2", result.Metadata["key2"]);
    }
}
