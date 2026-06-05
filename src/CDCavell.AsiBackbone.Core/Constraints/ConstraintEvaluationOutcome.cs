namespace CDCavell.AsiBackbone.Core.Constraints;

/// <summary>
/// Represents the outcome produced by evaluating a constraint.
/// </summary>
public enum ConstraintEvaluationOutcome
{
    /// <summary>
    /// The constraint does not apply to the supplied context.
    /// </summary>
    NotApplicable = 0,

    /// <summary>
    /// The constraint allows the operation to proceed.
    /// </summary>
    Allowed = 1,

    /// <summary>
    /// The constraint allows the operation to proceed but produced audit-worthy warnings.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// The constraint denies the operation.
    /// </summary>
    Denied = 3
}
