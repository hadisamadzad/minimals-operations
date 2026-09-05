namespace Minimals.Operations;

/// <summary>
/// Identifies a command and the result type produced by its operation.
/// Commands represent the input parameters for an operation and should be immutable records.
/// </summary>
/// <typeparam name="TResult">The operation result value type.</typeparam>
public interface IOperationCommand<TResult>;
