namespace AsiBackbone.Core.Evaluation;

/// <summary>
/// Provides host-configurable options for the default AsiBackbone policy evaluator.
/// </summary>
public sealed class AsiBackbonePolicyEvaluatorOptions
{
    private const string DefaultNoConstraintsReasonMessage =
        "No policy constraints were registered or supplied for evaluation.";

    /// <summary>
    /// Gets the default machine-readable reason code used when strict empty-policy evaluation denies execution.
    /// </summary>
    public const string DefaultNoConstraintsReasonCode = "asibackbone.policy.no_constraints";

    /// <summary>
    /// Gets or sets a value indicating whether evaluation should deny when no constraints are registered or supplied.
    /// </summary>
    /// <remarks>
    /// The default value is <see langword="false" /> for backward compatibility with hosts that intentionally run
    /// permissive or explicitly unconstrained evaluation flows.
    /// </remarks>
    public bool DenyWhenNoConstraints { get; set; }

    /// <summary>
    /// Gets or sets the machine-readable reason code used when <see cref="DenyWhenNoConstraints" /> denies an empty policy.
    /// </summary>
    public string NoConstraintsReasonCode { get; set; } = DefaultNoConstraintsReasonCode;

    /// <summary>
    /// Gets or sets the reason message used when <see cref="DenyWhenNoConstraints" /> denies an empty policy.
    /// </summary>
    public string NoConstraintsReasonMessage { get; set; } = DefaultNoConstraintsReasonMessage;

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
    }
}
