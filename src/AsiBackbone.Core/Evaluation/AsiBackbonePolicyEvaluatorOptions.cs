namespace AsiBackbone.Core.Evaluation;

/// <summary>
/// Provides host-configurable options for the default AsiBackbone policy evaluator.
/// </summary>
public sealed class AsiBackbonePolicyEvaluatorOptions
{
    private const string DefaultNoConstraintsReasonMessage =
        "No policy constraints were registered or supplied for evaluation.";

    private const string DefaultConstraintExceptionReasonMessage =
        "A policy constraint failed during evaluation. The operation was denied by the evaluator failure policy.";

    private const string DefaultThreatContributorExceptionReasonMessage =
        "A threat model contributor failed during evaluation. The operation was denied by the evaluator failure policy.";

    /// <summary>
    /// Gets the default machine-readable reason code used when strict empty-policy evaluation denies execution.
    /// </summary>
    public const string DefaultNoConstraintsReasonCode = "asibackbone.policy.no_constraints";

    /// <summary>
    /// Gets the default machine-readable reason code used when constraint exception evaluation denies execution.
    /// </summary>
    public const string DefaultConstraintExceptionReasonCode = "asibackbone.policy.constraint_exception";

    /// <summary>
    /// Gets the default machine-readable reason code used when threat contributor exception evaluation denies execution.
    /// </summary>
    public const string DefaultThreatContributorExceptionReasonCode = "asibackbone.threat.contributor_exception";

    /// <summary>
    /// Gets or sets a value indicating whether evaluation should deny when no constraints are registered or supplied.
    /// </summary>
    /// <remarks>
    /// The default value is <see langword="false" /> for backward compatibility with hosts that intentionally run
    /// permissive or explicitly unconstrained evaluation flows.
    /// </remarks>
    public bool DenyWhenNoConstraints { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether evaluation should stop after the first denied constraint result.
    /// </summary>
    /// <remarks>
    /// The default value is <see langword="false" /> so the evaluator continues to collect the complete set of
    /// warning and denial reasons for audit visibility. Set this to <see langword="true" /> only when the host
    /// intentionally prefers latency-optimized fast-abort behavior over full reason-code aggregation.
    /// </remarks>
    public bool ShortCircuitOnFirstDenial { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether constraint exceptions should be converted into denied governance decisions.
    /// </summary>
    /// <remarks>
    /// The default value is <see langword="false" /> so constraint exceptions continue to propagate to the host. Set this
    /// to <see langword="true" /> when the host prefers fail-closed denied decisions with stable reason codes that can be
    /// audited by downstream governance sinks.
    /// </remarks>
    public bool TreatConstraintExceptionAsDenial { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether threat contributor exceptions should be converted into denied governance decisions.
    /// </summary>
    /// <remarks>
    /// The default value is <see langword="true" /> because threat-modeling extensions are expected to fail closed when
    /// they are explicitly registered. Set this to <see langword="false" /> only when the host intentionally wants contributor
    /// exceptions to propagate instead of producing a stable denied decision.
    /// </remarks>
    public bool TreatThreatContributorExceptionAsDenial { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether threat assessment outcomes should be protected from being downgraded to pure allow decisions.
    /// </summary>
    /// <remarks>
    /// When enabled, an actionable threat assessment that produced a warning or blocking decision remains traceable even if a custom
    /// decision policy would otherwise return <c>Allow</c>.
    /// </remarks>
    public bool PreventThreatAssessmentAllowDowngrade { get; set; } = true;

    /// <summary>
    /// Gets or sets the machine-readable reason code used when <see cref="DenyWhenNoConstraints" /> denies an empty policy.
    /// </summary>
    public string NoConstraintsReasonCode { get; set; } = DefaultNoConstraintsReasonCode;

    /// <summary>
    /// Gets or sets the reason message used when <see cref="DenyWhenNoConstraints" /> denies an empty policy.
    /// </summary>
    public string NoConstraintsReasonMessage { get; set; } = DefaultNoConstraintsReasonMessage;

    /// <summary>
    /// Gets or sets the machine-readable reason code used when <see cref="TreatConstraintExceptionAsDenial" /> denies a constraint exception.
    /// </summary>
    public string ConstraintExceptionReasonCode { get; set; } = DefaultConstraintExceptionReasonCode;

    /// <summary>
    /// Gets or sets the reason message used when <see cref="TreatConstraintExceptionAsDenial" /> denies a constraint exception.
    /// </summary>
    public string ConstraintExceptionReasonMessage { get; set; } = DefaultConstraintExceptionReasonMessage;

    /// <summary>
    /// Gets or sets the machine-readable reason code used when <see cref="TreatThreatContributorExceptionAsDenial" /> denies a contributor exception.
    /// </summary>
    public string ThreatContributorExceptionReasonCode { get; set; } = DefaultThreatContributorExceptionReasonCode;

    /// <summary>
    /// Gets or sets the reason message used when <see cref="TreatThreatContributorExceptionAsDenial" /> denies a contributor exception.
    /// </summary>
    public string ThreatContributorExceptionReasonMessage { get; set; } = DefaultThreatContributorExceptionReasonMessage;

    /// <summary>
    /// Validates evaluator options.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(NoConstraintsReasonCode))
        {
            throw new InvalidOperationException($"{nameof(NoConstraintsReasonCode)} must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(NoConstraintsReasonMessage))
        {
            throw new InvalidOperationException($"{nameof(NoConstraintsReasonMessage)} must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(ConstraintExceptionReasonCode))
        {
            throw new InvalidOperationException($"{nameof(ConstraintExceptionReasonCode)} must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(ConstraintExceptionReasonMessage))
        {
            throw new InvalidOperationException($"{nameof(ConstraintExceptionReasonMessage)} must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(ThreatContributorExceptionReasonCode))
        {
            throw new InvalidOperationException($"{nameof(ThreatContributorExceptionReasonCode)} must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(ThreatContributorExceptionReasonMessage))
        {
            throw new InvalidOperationException($"{nameof(ThreatContributorExceptionReasonMessage)} must not be empty.");
        }
    }
}
