namespace CDCavell.ASIBackbone.Core.Results;

/// <summary>
/// Represents the framework-neutral outcome of an operation that may return a value.
/// </summary>
/// <typeparam name="TValue">The result value type.</typeparam>
public sealed class OperationResult<TValue> : OperationResult
{
    /// <summary>
    /// Initializes a new successful instance of the <see cref="OperationResult{TValue}"/> class.
    /// </summary>
    /// <param name="value">The operation value.</param>
    /// <param name="reasons">The reasons associated with the operation result.</param>
    /// <param name="warnings">The warnings associated with the operation result.</param>
    internal OperationResult(
        TValue value,
        IReadOnlyList<OperationReason> reasons,
        IReadOnlyList<string> warnings)
        : base(true, reasons, warnings)
    {
        ValueCore = value;
    }

    /// <summary>
    /// Initializes a new failed instance of the <see cref="OperationResult{TValue}"/> class.
    /// </summary>
    /// <param name="reasons">The reasons associated with the operation result.</param>
    /// <param name="warnings">The warnings associated with the operation result.</param>
    internal OperationResult(
        IReadOnlyList<OperationReason> reasons,
        IReadOnlyList<string> warnings)
        : base(false, reasons, warnings)
    {
        ValueCore = default!;
    }

    /// <summary>
    /// Gets the successful operation value.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the operation result is failed and no successful value is available.
    /// </exception>
    public TValue Value => Succeeded
        ? ValueCore
        : throw new InvalidOperationException("Cannot access the value of a failed operation result.");

    /// <summary>
    /// Gets a value indicating whether a successful result value is available.
    /// </summary>
    public bool HasValue => Succeeded;

    private TValue ValueCore { get; }
}
