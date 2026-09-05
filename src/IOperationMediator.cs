namespace Minimals.Operations;

/// <summary>
/// Executes typed operation commands through their generated or discovered operation.
/// </summary>
public interface IOperationMediator
{
    /// <summary>
    /// Executes a command using the operation registered for its concrete command type.
    /// </summary>
    Task<OperationResult<TResult>> ExecuteAsync<TResult>(IOperationCommand<TResult> command,
        CancellationToken? cancellation = null);

    /// <summary>
    /// Publishes an event to all generated operations registered for its concrete type.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken? cancellation = null)
        where TEvent : IOperationEvent;
}
