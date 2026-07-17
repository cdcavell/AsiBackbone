namespace AsiBackbone.Core.HostIntegration;

/// <summary>
/// Identifies the host persistence result associated with a governed operation execution attempt.
/// </summary>
public enum GovernedOperationPersistenceOutcome
{
    /// <summary>
    /// Host persistence committed and produced a mutation batch.
    /// </summary>
    Committed = 100,

    /// <summary>
    /// Host persistence failed before a commit was established.
    /// </summary>
    Failed = 200,

    /// <summary>
    /// Host persistence was rolled back and produced no committed mutation batch.
    /// </summary>
    RolledBack = 300,

    /// <summary>
    /// The governed operation completed successfully without persisting any mutation.
    /// </summary>
    CompletedWithoutMutation = 400
}
