namespace Minimals.Operations;

/// <summary>
/// Defines an operation that executes a command and returns a result.
/// Operations encapsulate use case logic in clean architecture applications.
/// </summary>
/// <typeparam name="TCommand">The type of command that triggers this operation.</typeparam>
/// <typeparam name="TResult">The type of result returned by this operation.</typeparam>
public interface IOperation<TCommand, TResult> where TCommand : IOperationCommand<TResult>
{
    /// <summary>
    /// Executes the operation asynchronously with the specified command.
    /// </summary>
    /// <param name="command">The command containing the operation parameters.</param>
    /// <param name="cancellation">Optional cancellation token to cancel the operation.</param>
    /// <returns>An operation result indicating success or failure with appropriate details.</returns>
    Task<OperationResult<TResult>> ExecuteAsync(TCommand command, CancellationToken? cancellation = null);
}

/// <summary>
/// Defines an operation that handles a published event.
/// </summary>
/// <typeparam name="TEvent">The event type handled by the operation.</typeparam>
public interface IOperation<TEvent> where TEvent : IOperationEvent
{
    /// <summary>
    /// Handles the specified event asynchronously.
    /// </summary>
    /// <param name="event">The event to handle.</param>
    /// <param name="cancellation">Optional cancellation token to cancel handling.</param>
    Task ExecuteAsync(TEvent @event, CancellationToken? cancellation = null);
}
