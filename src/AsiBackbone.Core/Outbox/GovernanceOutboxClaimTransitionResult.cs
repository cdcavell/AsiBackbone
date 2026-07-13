namespace AsiBackbone.Core.Outbox;

/// <summary>
/// Represents the caller-visible result of a claimed outbox transition attempt.
/// </summary>
public sealed class GovernanceOutboxClaimTransitionResult
{
    private GovernanceOutboxClaimTransitionResult(
        GovernanceOutboxEntry entry,
        GovernanceOutboxClaimTransitionOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (!Enum.IsDefined(outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Claim transition outcome must be defined.");
        }

        Entry = entry;
        Outcome = outcome;
    }

    /// <summary>
    /// Gets the durable entry observed after the transition attempt, or the caller's last safe snapshot when the row is missing.
    /// </summary>
    public GovernanceOutboxEntry Entry { get; }

    /// <summary>
    /// Gets the explicit transition outcome.
    /// </summary>
    public GovernanceOutboxClaimTransitionOutcome Outcome { get; }

    /// <summary>
    /// Gets a value indicating whether this invocation persisted the requested transition.
    /// </summary>
    public bool IsApplied => Outcome is GovernanceOutboxClaimTransitionOutcome.Applied;

    /// <summary>
    /// Creates a claimed transition result.
    /// </summary>
    public static GovernanceOutboxClaimTransitionResult Create(
        GovernanceOutboxEntry entry,
        GovernanceOutboxClaimTransitionOutcome outcome)
    {
        return new GovernanceOutboxClaimTransitionResult(entry, outcome);
    }
}
