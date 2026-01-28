namespace Minimals.Operations;

/// <summary>
/// Represents error information from a failed operation.
/// </summary>
/// <param name="Type">The type of error that occurred.</param>
/// <param name="Messages">The error messages describing what went wrong.</param>
public record OperationError(OperationErrorType Type, string[] Messages)
{
    /// <summary>
    /// Creates a validation error with the specified messages.
    /// </summary>
    /// <param name="messages">The validation error messages.</param>
    /// <returns>An operation error with ValidationError type.</returns>
    public static OperationError Validation(params string[] messages) =>
        new(OperationErrorType.ValidationError, messages);

    /// <summary>
    /// Creates an authorization error with the specified message.
    /// </summary>
    /// <param name="message">The authorization error message.</param>
    /// <returns>An operation error with AuthorizationError type.</returns>
    public static OperationError Authorization(string message) =>
        new(OperationErrorType.AuthorizationError, [message]);

    /// <summary>
    /// Creates an unexpected error with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An operation error with UnexpectedError type.</returns>
    public static OperationError Unexpected(string message) =>
        new(OperationErrorType.UnexpectedError, [message]);
}

/// <summary>
/// Specifies the type of error that occurred during operation execution.
/// </summary>
public enum OperationErrorType
{
    /// <summary>
    /// The error type was not specified.
    /// </summary>
    NotSpecified = 1,

    /// <summary>
    /// The error occurred due to validation failure.
    /// </summary>
    ValidationError,

    /// <summary>
    /// The error occurred due to authorization/permission issues.
    /// </summary>
    AuthorizationError,

    /// <summary>
    /// The error occurred due to an unexpected condition.
    /// </summary>
    UnexpectedError
}
