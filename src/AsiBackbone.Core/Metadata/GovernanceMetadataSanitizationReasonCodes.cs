namespace AsiBackbone.Core.Metadata;

/// <summary>
/// Provides stable provider-neutral reason codes for governance metadata sanitation failures.
/// </summary>
public static class GovernanceMetadataSanitizationReasonCodes
{
    /// <summary>
    /// Reason code used when sanitized metadata fails the configured metadata budget.
    /// </summary>
    public const string BudgetViolation = "metadata.budget_violation";
}
