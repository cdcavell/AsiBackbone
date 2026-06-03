namespace CDCavell.ASIBackbone.Core.Results;

/// <summary>
/// Represents the framework-neutral outcome of an operation that may return a value.
/// </summary>
/// <typeparam name="TValue">The result value type.</typeparam>
public sealed class OperationResult<TValue> : OperationResult
{
    private readonly TValue? value;

    private OperationResult(
        bool succeeded,
        TValue? value,
        IReadOnlyList<OperationReason> reasons,
        IReadOnlyList<string> warnings)
        : base(succeeded, reasons, warnings)
    {
        this.value = value;
    }

    /// <summary>
    /// Gets the successful operation value.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the operation result is failed and no successful value is available.
    /// </exception>
    public TValue Value => Succeeded
        ? value!
        : throw new InvalidOperationException("Cannot access the value of a failed operation result.");

    /// <summary>
    /// Gets a value indicating whether a successful result value is available.
    /// </summary>
    public bool HasValue => Succeeded;

    /// <summary>
    /// Creates a successful operation result with a value.
    /// </summary>
    /// <param name="value">The operation value.</param>
    /// <returns>A successful operation result.</returns>
    public static OperationResult<TValue> Success(TValue value)
    {
        return new OperationResult<TValue>(true, value, EmptyReasons(), EmptyWarnings());
    }

    /// <summary>
    /// Creates a successful operation result with a value and warnings.
    /// </summary>
    /// <param name="value">The operation value.</param>
    /// <param name="warnings">The warnings associated with the successful result.</param>
    /// <returns>A successful operation result.</returns>
    public static OperationResult<TValue> Success(TValue value, IEnumerable<string> warnings)
    {
        return new OperationResult<TValue>(true, value, EmptyReasons(), NormalizeWarnings(warnings));
    }

    /// <summary>
    /// Creates a failed operation result.
    /// </summary>
    /// <param name="code">The machine-readable reason code.</param>
    /// <param name="message">The human-readable reason message.</param>
    /// <returns>A failed operation result.</returns>
    public static new OperationResult<TValue> Failure(string code, string message)
    {
        return Failure(OperationReason.Create(code, message));
    }

    /// <summary>
    /// Creates a failed operation result.
    /// </summary>
    /// <param name="reason">The reason associated with the failed result.</param>
    /// <returns>A failed operation result.</returns>
    public static OperationResult<TValue> Failure(OperationReason reason)
    {
        ArgumentNullException.ThrowIfNull(reason);

        return new OperationResult<TValue>(false, default, Array.AsReadOnly([reason]), EmptyWarnings());
    }

    /// <summary>
    /// Creates a failed operation result.
    /// </summary>
    /// <param name="reasons">The reasons associated with the failed result.</param>
    /// <returns>A failed operation result.</returns>
    public static new OperationResult<TValue> Failure(IEnumerable<OperationReason> reasons)
    {
        return new OperationResult<TValue>(false, default, NormalizeReasons(reasons), EmptyWarnings());
    }

    /// <summary>
    /// Creates a failed operation result with warnings.
    /// </summary>
    /// <param name="reasons">The reasons associated with the failed result.</param>
    /// <param name="warnings">The warnings associated with the failed result.</param>
    /// <returns>A failed operation result.</returns>
    public static new OperationResult<TValue> Failure(
        IEnumerable<OperationReason> reasons,
        IEnumerable<string> warnings)
    {
        return new OperationResult<TValue>(false, default, NormalizeReasons(reasons), NormalizeWarnings(warnings));
    }

    /// <summary>
    /// Creates a failed operation result using the default failure reason.
    /// </summary>
    /// <returns>A failed operation result.</returns>
    public static new OperationResult<TValue> Failure()
    {
        return new OperationResult<TValue>(false, default, NormalizeReasons(null), EmptyWarnings());
    }

    private static IReadOnlyList<OperationReason> EmptyReasons()
    {
        return Array.AsReadOnly(Array.Empty<OperationReason>());
    }

    private static IReadOnlyList<string> EmptyWarnings()
    {
        return Array.AsReadOnly(Array.Empty<string>());
    }
}
