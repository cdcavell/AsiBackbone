namespace CDCavell.AsiBackbone.Core.Results;

/// <summary>
/// Represents the framework-neutral outcome of an operation.
/// </summary>
public class OperationResult
{
    private const string DefaultFailureCode = "operation.failed";
    private const string DefaultFailureMessage = "Operation failed.";

    private static readonly IReadOnlyList<OperationReason> EmptyReasons =
        Array.AsReadOnly(Array.Empty<OperationReason>());

    private static readonly IReadOnlyList<string> EmptyWarnings =
        Array.AsReadOnly(Array.Empty<string>());

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationResult"/> class.
    /// </summary>
    /// <param name="succeeded">A value indicating whether the operation succeeded.</param>
    /// <param name="reasons">The reasons associated with the operation result.</param>
    /// <param name="warnings">The warnings associated with the operation result.</param>
    protected OperationResult(
        bool succeeded,
        IReadOnlyList<OperationReason> reasons,
        IReadOnlyList<string> warnings)
    {
        Succeeded = succeeded;
        Reasons = reasons;
        Warnings = warnings;
        ReasonCodes = Array.AsReadOnly([.. reasons.Select(reason => reason.Code)]);
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool Failed => !Succeeded;

    /// <summary>
    /// Gets the reasons associated with the operation result.
    /// </summary>
    public IReadOnlyList<OperationReason> Reasons { get; }

    /// <summary>
    /// Gets machine-readable reason codes associated with the operation result.
    /// </summary>
    public IReadOnlyList<string> ReasonCodes { get; }

    /// <summary>
    /// Gets human-readable warning messages associated with the operation result.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>
    /// Gets a value indicating whether the operation result has warning messages.
    /// </summary>
    public bool HasWarnings => Warnings.Count > 0;

    /// <summary>
    /// Creates a successful operation result.
    /// </summary>
    /// <returns>A successful operation result.</returns>
    public static OperationResult Success()
    {
        return new OperationResult(true, EmptyReasons, EmptyWarnings);
    }

    /// <summary>
    /// Creates a successful operation result with warnings.
    /// </summary>
    /// <param name="warnings">The warnings associated with the successful result.</param>
    /// <returns>A successful operation result.</returns>
    public static OperationResult Success(IEnumerable<string> warnings)
    {
        return new OperationResult(true, EmptyReasons, NormalizeWarnings(warnings));
    }

    /// <summary>
    /// Creates a successful operation result with a value.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <param name="value">The operation value.</param>
    /// <returns>A successful operation result.</returns>
    public static OperationResult<TValue> Success<TValue>(TValue value)
    {
        return new OperationResult<TValue>(value, EmptyReasons, EmptyWarnings);
    }

    /// <summary>
    /// Creates a successful operation result with a value and warnings.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <param name="value">The operation value.</param>
    /// <param name="warnings">The warnings associated with the successful result.</param>
    /// <returns>A successful operation result.</returns>
    public static OperationResult<TValue> Success<TValue>(TValue value, IEnumerable<string> warnings)
    {
        return new OperationResult<TValue>(value, EmptyReasons, NormalizeWarnings(warnings));
    }

    /// <summary>
    /// Creates a failed operation result.
    /// </summary>
    /// <param name="code">The machine-readable reason code.</param>
    /// <param name="message">The human-readable reason message.</param>
    /// <returns>A failed operation result.</returns>
    public static OperationResult Failure(string code, string message)
    {
        return Failure(OperationReason.Create(code, message));
    }

    /// <summary>
    /// Creates a failed operation result.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <param name="code">The machine-readable reason code.</param>
    /// <param name="message">The human-readable reason message.</param>
    /// <returns>A failed operation result.</returns>
    public static OperationResult<TValue> Failure<TValue>(string code, string message)
    {
        return Failure<TValue>(OperationReason.Create(code, message));
    }

    /// <summary>
    /// Creates a failed operation result.
    /// </summary>
    /// <param name="reason">The reason associated with the failed result.</param>
    /// <returns>A failed operation result.</returns>
    public static OperationResult Failure(OperationReason reason)
    {
        ArgumentNullException.ThrowIfNull(reason);

        return new OperationResult(false, Array.AsReadOnly([reason]), EmptyWarnings);
    }

    /// <summary>
    /// Creates a failed operation result.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <param name="reason">The reason associated with the failed result.</param>
    /// <returns>A failed operation result.</returns>
    public static OperationResult<TValue> Failure<TValue>(OperationReason reason)
    {
        ArgumentNullException.ThrowIfNull(reason);

        return new OperationResult<TValue>(Array.AsReadOnly([reason]), EmptyWarnings);
    }

    /// <summary>
    /// Creates a failed operation result.
    /// </summary>
    /// <param name="reasons">The reasons associated with the failed result.</param>
    /// <returns>A failed operation result.</returns>
    public static OperationResult Failure(IEnumerable<OperationReason> reasons)
    {
        return new OperationResult(false, NormalizeReasons(reasons), EmptyWarnings);
    }

    /// <summary>
    /// Creates a failed operation result.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <param name="reasons">The reasons associated with the failed result.</param>
    /// <returns>A failed operation result.</returns>
    public static OperationResult<TValue> Failure<TValue>(IEnumerable<OperationReason> reasons)
    {
        return new OperationResult<TValue>(NormalizeReasons(reasons), EmptyWarnings);
    }

    /// <summary>
    /// Creates a failed operation result with warnings.
    /// </summary>
    /// <param name="reasons">The reasons associated with the failed result.</param>
    /// <param name="warnings">The warnings associated with the failed result.</param>
    /// <returns>A failed operation result.</returns>
    public static OperationResult Failure(
        IEnumerable<OperationReason> reasons,
        IEnumerable<string> warnings)
    {
        return new OperationResult(false, NormalizeReasons(reasons), NormalizeWarnings(warnings));
    }

    /// <summary>
    /// Creates a failed operation result with warnings.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <param name="reasons">The reasons associated with the failed result.</param>
    /// <param name="warnings">The warnings associated with the failed result.</param>
    /// <returns>A failed operation result.</returns>
    public static OperationResult<TValue> Failure<TValue>(
        IEnumerable<OperationReason> reasons,
        IEnumerable<string> warnings)
    {
        return new OperationResult<TValue>(NormalizeReasons(reasons), NormalizeWarnings(warnings));
    }

    /// <summary>
    /// Creates a failed operation result using the default failure reason.
    /// </summary>
    /// <returns>A failed operation result.</returns>
    public static OperationResult Failure()
    {
        return Failure(DefaultFailureCode, DefaultFailureMessage);
    }

    /// <summary>
    /// Creates a failed operation result using the default failure reason.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <returns>A failed operation result.</returns>
    public static OperationResult<TValue> Failure<TValue>()
    {
        return new OperationResult<TValue>(NormalizeReasons(null), EmptyWarnings);
    }

    /// <summary>
    /// Creates a failed operation result from this result's reasons and warnings.
    /// </summary>
    /// <returns>A failed operation result.</returns>
    public OperationResult ToFailure()
    {
        return Failed
            ? this
            : Failure();
    }

    /// <summary>
    /// Normalizes operation reasons and provides a default failure reason when none are supplied.
    /// </summary>
    /// <param name="reasons">The reasons to normalize.</param>
    /// <returns>A normalized reason collection.</returns>
    protected static IReadOnlyList<OperationReason> NormalizeReasons(IEnumerable<OperationReason>? reasons)
    {
        OperationReason[] normalizedReasons = reasons?
            .Where(reason => reason is not null)
            .ToArray() ?? [];

        return normalizedReasons.Length == 0
            ? Array.AsReadOnly([OperationReason.Create(DefaultFailureCode, DefaultFailureMessage)])
            : Array.AsReadOnly(normalizedReasons);
    }

    /// <summary>
    /// Normalizes warning messages.
    /// </summary>
    /// <param name="warnings">The warnings to normalize.</param>
    /// <returns>A normalized warning collection.</returns>
    protected static IReadOnlyList<string> NormalizeWarnings(IEnumerable<string>? warnings)
    {
        string[] normalizedWarnings = warnings?
            .Where(warning => !string.IsNullOrWhiteSpace(warning))
            .Select(warning => warning.Trim())
            .ToArray() ?? [];

        return normalizedWarnings.Length == 0
            ? EmptyWarnings
            : Array.AsReadOnly(normalizedWarnings);
    }
}
