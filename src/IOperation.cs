namespace Minimals.Operations;

/// <summary>
/// Defines an operation that executes a command and returns a result.
/// Operations encapsulate use case logic in clean architecture applications.
/// </summary>
/// <typeparam name="TCommand">The type of command that triggers this operation.</typeparam>
/// <typeparam name="TResult">The type of result returned by this operation.</typeparam>
public interface IOperation<TCommand, TResult> where TCommand : IOperationCommand
{
    /// <summary>
    /// Executes the operation asynchronously with the specified command.
    /// </summary>
    /// <param name="command">The command containing the operation parameters.</param>
    /// <param name="cancellation">Optional cancellation token to cancel the operation.</param>
    /// <returns>An operation result indicating success or failure with appropriate details.</returns>
    Task<OperationResult<TResult>> ExecuteAsync(TCommand command,
        CancellationToken? cancellation = null);
}
