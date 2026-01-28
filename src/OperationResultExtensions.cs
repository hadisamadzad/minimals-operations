namespace Minimals.Operations;

/// <summary>
/// Extension methods for working with OperationResult in a functional style.
/// </summary>
public static class OperationResultExtensions
{
    /// <summary>
    /// Transforms the value of a successful operation result using the specified function.
    /// If the operation failed, returns a new failed result with the same error.
    /// </summary>
    /// <typeparam name="TSource">The type of the source value.</typeparam>
    /// <typeparam name="TResult">The type of the transformed value.</typeparam>
    /// <param name="result">The source operation result.</param>
    /// <param name="mapper">The function to transform the value.</param>
    /// <returns>A new operation result with the transformed value or the original error.</returns>
    public static OperationResult<TResult> Map<TSource, TResult>(
        this OperationResult<TSource> result,
        Func<TSource, TResult> mapper)
    {
        if (!result.Succeeded || result.Value is null)
        {
            return new OperationResult<TResult>(result.Status, default, result.Error, result.Metadata);
        }

        var mappedValue = mapper(result.Value);
        return new OperationResult<TResult>(result.Status, mappedValue, result.Error, result.Metadata);
    }

    /// <summary>
    /// Chains another operation that returns an OperationResult. If the current operation failed,
    /// returns a new failed result without executing the binder function.
    /// </summary>
    /// <typeparam name="TSource">The type of the source value.</typeparam>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <param name="result">The source operation result.</param>
    /// <param name="binder">The function that returns the next operation result.</param>
    /// <returns>The result of the binder function or the original error.</returns>
    public static OperationResult<TResult> Bind<TSource, TResult>(
        this OperationResult<TSource> result,
        Func<TSource, OperationResult<TResult>> binder)
    {
        if (!result.Succeeded || result.Value is null)
        {
            return new OperationResult<TResult>(result.Status, default, result.Error, result.Metadata);
        }

        return binder(result.Value);
    }

    /// <summary>
    /// Asynchronously chains another operation that returns an OperationResult. If the current operation failed,
    /// returns a new failed result without executing the binder function.
    /// </summary>
    /// <typeparam name="TSource">The type of the source value.</typeparam>
    /// <typeparam name="TResult">The type of the result value.</typeparam>
    /// <param name="result">The source operation result.</param>
    /// <param name="binder">The async function that returns the next operation result.</param>
    /// <returns>The result of the binder function or the original error.</returns>
    public static async Task<OperationResult<TResult>> BindAsync<TSource, TResult>(
        this OperationResult<TSource> result,
        Func<TSource, Task<OperationResult<TResult>>> binder)
    {
        if (!result.Succeeded || result.Value is null)
        {
            return new OperationResult<TResult>(result.Status, default, result.Error, result.Metadata);
        }

        return await binder(result.Value);
    }

    /// <summary>
    /// Pattern matches on the operation result status and executes the corresponding handler.
    /// </summary>
    /// <typeparam name="TResult">The type of the operation result value.</typeparam>
    /// <typeparam name="TOutput">The type of the output from the match handlers.</typeparam>
    /// <param name="result">The operation result to match on.</param>
    /// <param name="onSuccess">Handler called when the operation succeeded.</param>
    /// <param name="onFailure">Handler called when the operation failed.</param>
    /// <returns>The output from the matched handler.</returns>
    public static TOutput Match<TResult, TOutput>(
        this OperationResult<TResult> result,
        Func<TResult?, TOutput> onSuccess,
        Func<OperationStatus, OperationError?, TOutput> onFailure)
    {
        return result.Succeeded
            ? onSuccess(result.Value)
            : onFailure(result.Status, result.Error);
    }

    /// <summary>
    /// Pattern matches on the operation result status with detailed handlers for each status type.
    /// </summary>
    /// <typeparam name="TResult">The type of the operation result value.</typeparam>
    /// <typeparam name="TOutput">The type of the output from the match handlers.</typeparam>
    /// <param name="result">The operation result to match on.</param>
    /// <param name="onCompleted">Handler called when status is Completed.</param>
    /// <param name="onNoOperation">Handler called when status is NoOperation.</param>
    /// <param name="onInvalid">Handler called when status is Invalid.</param>
    /// <param name="onNotFound">Handler called when status is NotFound.</param>
    /// <param name="onUnauthorized">Handler called when status is Unauthorized.</param>
    /// <param name="onUnprocessable">Handler called when status is Unprocessable.</param>
    /// <param name="onFailed">Handler called when status is Failed.</param>
    /// <returns>The output from the matched handler.</returns>
    public static TOutput MatchDetailed<TResult, TOutput>(
        this OperationResult<TResult> result,
        Func<TResult?, TOutput> onCompleted,
        Func<TOutput> onNoOperation,
        Func<OperationError?, TOutput> onInvalid,
        Func<OperationError?, TOutput> onNotFound,
        Func<OperationError?, TOutput> onUnauthorized,
        Func<OperationError?, TOutput> onUnprocessable,
        Func<OperationError?, TOutput> onFailed)
    {
        return result.Status switch
        {
            OperationStatus.Completed => onCompleted(result.Value),
            OperationStatus.NoOperation => onNoOperation(),
            OperationStatus.Invalid => onInvalid(result.Error),
            OperationStatus.NotFound => onNotFound(result.Error),
            OperationStatus.Unauthorized => onUnauthorized(result.Error),
            OperationStatus.Unprocessable => onUnprocessable(result.Error),
            OperationStatus.Failed => onFailed(result.Error),
            _ => throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status, "Unknown operation status")
        };
    }

    /// <summary>
    /// Executes an action if the operation succeeded.
    /// </summary>
    /// <typeparam name="TResult">The type of the operation result value.</typeparam>
    /// <param name="result">The operation result.</param>
    /// <param name="action">The action to execute on success.</param>
    /// <returns>The original operation result for method chaining.</returns>
    public static OperationResult<TResult> OnSuccess<TResult>(
        this OperationResult<TResult> result,
        Action<TResult?> action)
    {
        if (result.Succeeded)
        {
            action(result.Value);
        }
        return result;
    }

    /// <summary>
    /// Executes an action if the operation failed.
    /// </summary>
    /// <typeparam name="TResult">The type of the operation result value.</typeparam>
    /// <param name="result">The operation result.</param>
    /// <param name="action">The action to execute on failure.</param>
    /// <returns>The original operation result for method chaining.</returns>
    public static OperationResult<TResult> OnFailure<TResult>(
        this OperationResult<TResult> result,
        Action<OperationStatus, OperationError?> action)
    {
        if (result.Failed)
        {
            action(result.Status, result.Error);
        }
        return result;
    }
}
