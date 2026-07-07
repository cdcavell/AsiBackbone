using AsiBackbone.Core.Constraints;

namespace AsiBackbone.Core.ThreatModeling;

/// <summary>
/// Contributes host-defined threat-aware assessment data to policy evaluation.
/// </summary>
/// <typeparam name="TContext">The framework-neutral context type evaluated by the contributor.</typeparam>
/// <remarks>
/// Contributors are extension points. AsiBackbone does not claim to detect threats by itself; hosts provide the checks that are appropriate for their domain.
/// </remarks>
public interface IThreatModelContributor<in TContext>
    where TContext : IAsiBackboneConstraintEvaluationContext
{
    /// <summary>
    /// Gets the stable name of the contributor for audit metadata.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Assesses the supplied context and returns a threat assessment.
    /// </summary>
    /// <param name="context">The context to assess.</param>
    /// <param name="cancellationToken">A token that can cancel asynchronous assessment.</param>
    /// <returns>The threat assessment reported by the contributor.</returns>
    ValueTask<ThreatAssessment> AssessAsync(
        TContext context,
        CancellationToken cancellationToken = default);
}
