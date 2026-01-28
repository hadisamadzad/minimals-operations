namespace Minimals.Operations;

/// <summary>
/// Represents the result of an operation execution with status, value, error information, and optional metadata.
/// </summary>
/// <typeparam name="TResult">The type of the value returned by the operation.</typeparam>
public record OperationResult<TResult>(
    OperationStatus Status,
    TResult? Value = default,
    OperationError? Error = null,
    Dictionary<string, string>? Metadata = null)
{
    /// <summary>
    /// Gets the status of the operation execution.
    /// </summary>
    public readonly OperationStatus Status = Status;

    /// <summary>
    /// Gets the value returned by the operation if successful.
    /// </summary>
    public readonly TResult? Value = Value;

    /// <summary>
    /// Gets the error information if the operation failed.
    /// </summary>
    public readonly OperationError? Error = Error;

    /// <summary>
    /// Gets optional metadata associated with the operation result.
    /// </summary>
    public readonly Dictionary<string, string>? Metadata = Metadata;

    /// <summary>
    /// Gets a value indicating whether the operation succeeded (Completed or NoOperation status).
    /// </summary>
    public bool Succeeded => Status is OperationStatus.Completed or OperationStatus.NoOperation;

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool Failed => !Succeeded;

    // Factory methods

    /// <summary>
    /// Creates a successful operation result with the specified value.
    /// </summary>
    /// <param name="value">The value to return.</param>
    /// <returns>An operation result with Completed status.</returns>
    public static OperationResult<TResult> Success(TResult value) =>
        new(OperationStatus.Completed, value);

    /// <summary>
    /// Creates a successful operation result without a value.
    /// </summary>
    /// <returns>An operation result with Completed status.</returns>
    public static OperationResult<NoResult> Success() =>
        new(OperationStatus.Completed, new NoResult());

    /// <summary>
    /// Creates a no-operation result with the specified value.
    /// </summary>
    /// <param name="value">The value to return.</param>
    /// <returns>An operation result with NoOperation status.</returns>
    public static OperationResult<TResult> NoOperation(TResult value) =>
        new(OperationStatus.NoOperation, default);

    /// <summary>
    /// Creates a no-operation result without a value.
    /// </summary>
    /// <returns>An operation result with NoOperation status.</returns>
    public static OperationResult<NoResult> NoOperation() =>
        new(OperationStatus.NoOperation, new NoResult());

    /// <summary>
    /// Creates a validation failure result with the specified error messages.
    /// </summary>
    /// <param name="messages">The validation error messages.</param>
    /// <returns>An operation result with Invalid status.</returns>
    public static OperationResult<TResult> ValidationFailure(params string[] messages) =>
        new(OperationStatus.Invalid, Error: OperationError.Validation(messages));

    /// <summary>
    /// Creates a not found failure result with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An operation result with NotFound status.</returns>
    public static OperationResult<TResult> NotFoundFailure(string message) =>
        new(OperationStatus.NotFound, Error: OperationError.Unexpected(message));

    /// <summary>
    /// Creates an authorization failure result with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An operation result with Unauthorized status.</returns>
    public static OperationResult<TResult> AuthorizationFailure(string message) =>
        new(OperationStatus.Unauthorized, Error: OperationError.Authorization(message));

    /// <summary>
    /// Creates an unprocessable entity failure result with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An operation result with Unprocessable status.</returns>
    public static OperationResult<TResult> UnprocessableFailure(string message) =>
        new(OperationStatus.Unprocessable, Error: OperationError.Unexpected(message));

    /// <summary>
    /// Creates a general failure result with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An operation result with Failed status.</returns>
    public static OperationResult<TResult> Failure(string message) =>
        new(OperationStatus.Failed, Error: OperationError.Unexpected(message));
}

/// <summary>
/// Represents the status of an operation execution.
/// </summary>
public enum OperationStatus
{
    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// The operation determined no action was needed.
    /// </summary>
    NoOperation,

    /// <summary>
    /// The operation failed due to invalid input or validation errors.
    /// </summary>
    Invalid,

    /// <summary>
    /// The operation failed because a required resource was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The operation failed due to authorization/permission issues.
    /// </summary>
    Unauthorized,

    /// <summary>
    /// The operation failed because the request cannot be processed (semantic error).
    /// </summary>
    Unprocessable,

    /// <summary>
    /// The operation failed due to an unexpected error.
    /// </summary>
    Failed
}
