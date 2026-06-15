namespace CDCavell.AsiBackbone.Core.Classification;

/// <summary>
/// Defines provider-neutral policy options for resolving DLP or classification failure behavior.
/// </summary>
public sealed class DlpFailurePolicyOptions
{
    /// <summary>
    /// Gets or sets the default behavior for low-risk intents.
    /// </summary>
    public DlpFailureBehavior LowRiskBehavior { get; set; } = DlpFailureBehavior.WarnAndAllow;

    /// <summary>
    /// Gets or sets the default behavior for medium-risk intents.
    /// </summary>
    public DlpFailureBehavior MediumRiskBehavior { get; set; } = DlpFailureBehavior.RequireAcknowledgment;

    /// <summary>
    /// Gets or sets the default behavior for high-risk or regulated intents.
    /// </summary>
    public DlpFailureBehavior HighRiskBehavior { get; set; } = DlpFailureBehavior.Deny;

    /// <summary>
    /// Gets risk- and failure-specific behavior overrides.
    /// </summary>
    public IDictionary<DlpFailurePolicyKey, DlpFailureBehavior> BehaviorOverrides { get; } =
        new Dictionary<DlpFailurePolicyKey, DlpFailureBehavior>();

    /// <summary>
    /// Resolves the configured behavior for the supplied failure context.
    /// </summary>
    /// <param name="context">The DLP failure policy context.</param>
    /// <returns>The configured behavior.</returns>
    public DlpFailureBehavior GetBehavior(DlpFailurePolicyContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var key = new DlpFailurePolicyKey(context.RiskLevel, context.FailureKind);

        if (BehaviorOverrides.TryGetValue(key, out DlpFailureBehavior overrideBehavior))
        {
            return ValidateBehavior(overrideBehavior, nameof(BehaviorOverrides));
        }

        DlpFailureBehavior behavior = context.RiskLevel switch
        {
            DlpIntentRiskLevel.Low => LowRiskBehavior,
            DlpIntentRiskLevel.Medium => MediumRiskBehavior,
            DlpIntentRiskLevel.High => HighRiskBehavior,
            _ => throw new ArgumentOutOfRangeException(nameof(context), context.RiskLevel, "DLP intent risk level must be defined.")
        };

        return ValidateBehavior(behavior, nameof(context));
    }

    private static DlpFailureBehavior ValidateBehavior(
        DlpFailureBehavior behavior,
        string parameterName)
    {
        return Enum.IsDefined(behavior)
            ? behavior
            : throw new ArgumentOutOfRangeException(parameterName, behavior, "DLP failure behavior must be defined.");
    }
}
