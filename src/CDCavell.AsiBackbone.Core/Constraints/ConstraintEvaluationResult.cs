using System.Collections.ObjectModel;
using CDCavell.AsiBackbone.Core.Results;

namespace CDCavell.AsiBackbone.Core.Constraints;

/// <summary>
/// Represents the framework-neutral result of evaluating a constraint.
/// </summary>
public sealed class ConstraintEvaluationResult
{
    private const string DefaultDeniedCode = "constraint.denied";
    private const string DefaultDeniedMessage = "Constraint denied the operation.";
    private const string DefaultWarningCode = "constraint.warning";
    private const string DefaultWarningMessage = "Constraint produced a warning.";

    private static readonly IReadOnlyList<OperationReason> EmptyReasons =
        Array.AsReadOnly(Array.Empty<OperationReason>());

    private ConstraintEvaluationResult(
        ConstraintEvaluationOutcome outcome,
        IReadOnlyList<OperationReason> reasons)
    {
        Outcome = outcome;
        Reasons = reasons;
        ReasonCodes = Array.AsReadOnly([.. reasons.Select(reason => reason.Code)]);
    }

    /// <summary>
    /// Gets the constraint evaluation outcome.
    /// </summary>
    public ConstraintEvaluationOutcome Outcome { get; }

    /// <summary>
    /// Gets reasons associated with denied or warning outcomes.
    /// </summary>
    public IReadOnlyList<OperationReason> Reasons { get; }

    /// <summary>
    /// Gets machine-readable reason codes associated with denied or warning outcomes.
    /// </summary>
    public IReadOnlyList<string> ReasonCodes { get; }

    /// <summary>
    /// Gets a value indicating whether the constraint allows the operation to proceed.
    /// </summary>
    public bool CanProceed => Outcome is not ConstraintEvaluationOutcome.Denied;

    /// <summary>
    /// Gets a value indicating whether this result denies the operation.
    /// </summary>
    public bool IsDenied => Outcome is ConstraintEvaluationOutcome.Denied;

    /// <summary>
    /// Gets a value indicating whether this result contains warnings.
    /// </summary>
    public bool IsWarning => Outcome is ConstraintEvaluationOutcome.Warning;

    /// <summary>
    /// Gets a value indicating whether this result is not applicable to the supplied context.
    /// </summary>
    public bool IsNotApplicable => Outcome is ConstraintEvaluationOutcome.NotApplicable;

    /// <summary>
    /// Gets a value indicating whether the result contains reason data.
    /// </summary>
    public bool HasReasons => Reasons.Count > 0;

    /// <summary>
    /// Creates an allowed constraint result.
    /// </summary>
    /// <returns>An allowed constraint evaluation result.</returns>
    public static ConstraintEvaluationResult Allow()
    {
        return new ConstraintEvaluationResult(ConstraintEvaluationOutcome.Allowed, EmptyReasons);
    }

    /// <summary>
    /// Creates a not-applicable constraint result.
    /// </summary>
    /// <returns>A not-applicable constraint evaluation result.</returns>
    public static ConstraintEvaluationResult NotApplicable()
    {
        return new ConstraintEvaluationResult(ConstraintEvaluationOutcome.NotApplicable, EmptyReasons);
    }

    /// <summary>
    /// Creates a denied constraint result.
    /// </summary>
    /// <param name="code">The machine-readable reason code.</param>
    /// <param name="message">The human-readable reason message.</param>
    /// <returns>A denied constraint evaluation result.</returns>
    public static ConstraintEvaluationResult Deny(string code, string message)
    {
        return Deny(OperationReason.Create(code, message));
    }

    /// <summary>
    /// Creates a denied constraint result.
    /// </summary>
    /// <param name="reason">The reason associated with the denied result.</param>
    /// <returns>A denied constraint evaluation result.</returns>
    public static ConstraintEvaluationResult Deny(OperationReason reason)
    {
        ArgumentNullException.ThrowIfNull(reason);

        return new ConstraintEvaluationResult(
            ConstraintEvaluationOutcome.Denied,
            Array.AsReadOnly([reason]));
    }

    /// <summary>
    /// Creates a denied constraint result.
    /// </summary>
    /// <param name="reasons">The reasons associated with the denied result.</param>
    /// <returns>A denied constraint evaluation result.</returns>
    public static ConstraintEvaluationResult Deny(IEnumerable<OperationReason> reasons)
    {
        return new ConstraintEvaluationResult(
            ConstraintEvaluationOutcome.Denied,
            NormalizeReasons(reasons, DefaultDeniedCode, DefaultDeniedMessage));
    }

    /// <summary>
    /// Creates a warning constraint result.
    /// </summary>
    /// <param name="code">The machine-readable reason code.</param>
    /// <param name="message">The human-readable reason message.</param>
    /// <returns>A warning constraint evaluation result.</returns>
    public static ConstraintEvaluationResult Warning(string code, string message)
    {
        return Warning(OperationReason.Create(code, message));
    }

    /// <summary>
    /// Creates a warning constraint result.
    /// </summary>
    /// <param name="reason">The reason associated with the warning result.</param>
    /// <returns>A warning constraint evaluation result.</returns>
    public static ConstraintEvaluationResult Warning(OperationReason reason)
    {
        ArgumentNullException.ThrowIfNull(reason);

        return new ConstraintEvaluationResult(
            ConstraintEvaluationOutcome.Warning,
            Array.AsReadOnly([reason]));
    }

    /// <summary>
    /// Creates a warning constraint result.
    /// </summary>
    /// <param name="reasons">The reasons associated with the warning result.</param>
    /// <returns>A warning constraint evaluation result.</returns>
    public static ConstraintEvaluationResult Warning(IEnumerable<OperationReason> reasons)
    {
        return new ConstraintEvaluationResult(
            ConstraintEvaluationOutcome.Warning,
            NormalizeReasons(reasons, DefaultWarningCode, DefaultWarningMessage));
    }

    private static ReadOnlyCollection<OperationReason> NormalizeReasons(
        IEnumerable<OperationReason>? reasons,
        string fallbackCode,
        string fallbackMessage)
    {
        OperationReason[] normalizedReasons = reasons?
            .Where(reason => reason is not null)
            .ToArray() ?? [];

        return normalizedReasons.Length == 0
            ? Array.AsReadOnly([OperationReason.Create(fallbackCode, fallbackMessage)])
            : Array.AsReadOnly(normalizedReasons);
    }
}
