using System.Collections.ObjectModel;
using CDCavell.ASIBackbone.Core.Results;

namespace CDCavell.ASIBackbone.Core.Decisions;

/// <summary>
/// Represents a framework-neutral governance decision produced by an ASIBackbone evaluation flow.
/// </summary>
public sealed class GovernanceDecision
{
    private const string DefaultDeniedCode = "decision.denied";
    private const string DefaultDeniedMessage = "Decision denied the operation.";
    private const string DefaultWarningCode = "decision.warning";
    private const string DefaultWarningMessage = "Decision produced a warning.";

    private static readonly ReadOnlyCollection<OperationReason> EmptyReasons =
        Array.AsReadOnly(Array.Empty<OperationReason>());

    private GovernanceDecision(
        GovernanceDecisionOutcome outcome,
        IReadOnlyList<OperationReason> reasons,
        string? correlationId,
        string? traceId,
        string? policyVersion,
        string? policyHash)
    {
        Outcome = outcome;
        Reasons = reasons;
        ReasonCodes = Array.AsReadOnly([.. reasons.Select(reason => reason.Code)]);
        CorrelationId = NormalizeOptional(correlationId);
        TraceId = NormalizeOptional(traceId);
        PolicyVersion = NormalizeOptional(policyVersion);
        PolicyHash = NormalizeOptional(policyHash);
    }

    /// <summary>
    /// Gets the selected governance decision outcome.
    /// </summary>
    public GovernanceDecisionOutcome Outcome { get; }

    /// <summary>
    /// Gets reasons associated with the governance decision.
    /// </summary>
    public IReadOnlyList<OperationReason> Reasons { get; }

    /// <summary>
    /// Gets machine-readable reason codes associated with the governance decision.
    /// </summary>
    public IReadOnlyList<string> ReasonCodes { get; }

    /// <summary>
    /// Gets the correlation identifier associated with the decision, when supplied by the host.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets the trace identifier associated with the decision, when supplied by the host.
    /// </summary>
    public string? TraceId { get; }

    /// <summary>
    /// Gets the policy version associated with the decision, when supplied by the host.
    /// </summary>
    public string? PolicyVersion { get; }

    /// <summary>
    /// Gets the policy hash associated with the decision, when supplied by the host.
    /// </summary>
    public string? PolicyHash { get; }

    /// <summary>
    /// Gets a value indicating whether the decision allows immediate execution.
    /// </summary>
    public bool CanProceed => Outcome is GovernanceDecisionOutcome.Allowed or GovernanceDecisionOutcome.Warning;

    /// <summary>
    /// Gets a value indicating whether the decision allows the operation.
    /// </summary>
    public bool IsAllowed => Outcome is GovernanceDecisionOutcome.Allowed;

    /// <summary>
    /// Gets a value indicating whether the decision allows the operation with warnings.
    /// </summary>
    public bool IsWarning => Outcome is GovernanceDecisionOutcome.Warning;

    /// <summary>
    /// Gets a value indicating whether the decision denies the operation.
    /// </summary>
    public bool IsDenied => Outcome is GovernanceDecisionOutcome.Denied;

    /// <summary>
    /// Gets a value indicating whether the decision defers the operation.
    /// </summary>
    public bool IsDeferred => Outcome is GovernanceDecisionOutcome.Deferred;

    /// <summary>
    /// Gets a value indicating whether the decision requires acknowledgment before execution.
    /// </summary>
    public bool RequiresAcknowledgment => Outcome is GovernanceDecisionOutcome.AcknowledgmentRequired;

    /// <summary>
    /// Gets a value indicating whether the decision recommends escalation.
    /// </summary>
    public bool EscalationRecommended => Outcome is GovernanceDecisionOutcome.EscalationRecommended;

    /// <summary>
    /// Gets a value indicating whether the decision includes reason data.
    /// </summary>
    public bool HasReasons => Reasons.Count > 0;

    /// <summary>
    /// Creates an allowed governance decision.
    /// </summary>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <param name="traceId">Optional trace identifier.</param>
    /// <param name="policyVersion">Optional policy version.</param>
    /// <param name="policyHash">Optional policy hash.</param>
    /// <returns>An allowed governance decision.</returns>
    public static GovernanceDecision Allow(
        string? correlationId = null,
        string? traceId = null,
        string? policyVersion = null,
        string? policyHash = null)
    {
        return new GovernanceDecision(
            GovernanceDecisionOutcome.Allowed,
            EmptyReasons,
            correlationId,
            traceId,
            policyVersion,
            policyHash);
    }

    /// <summary>
    /// Creates a denied governance decision.
    /// </summary>
    /// <param name="code">The machine-readable reason code.</param>
    /// <param name="message">The human-readable reason message.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <param name="traceId">Optional trace identifier.</param>
    /// <param name="policyVersion">Optional policy version.</param>
    /// <param name="policyHash">Optional policy hash.</param>
    /// <returns>A denied governance decision.</returns>
    public static GovernanceDecision Deny(
        string code,
        string message,
        string? correlationId = null,
        string? traceId = null,
        string? policyVersion = null,
        string? policyHash = null)
    {
        return Deny(
            OperationReason.Create(code, message),
            correlationId,
            traceId,
            policyVersion,
            policyHash);
    }

    /// <summary>
    /// Creates a denied governance decision.
    /// </summary>
    /// <param name="reason">The reason associated with the denied decision.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <param name="traceId">Optional trace identifier.</param>
    /// <param name="policyVersion">Optional policy version.</param>
    /// <param name="policyHash">Optional policy hash.</param>
    /// <returns>A denied governance decision.</returns>
    public static GovernanceDecision Deny(
        OperationReason reason,
        string? correlationId = null,
        string? traceId = null,
        string? policyVersion = null,
        string? policyHash = null)
    {
        ArgumentNullException.ThrowIfNull(reason);

        return new GovernanceDecision(
            GovernanceDecisionOutcome.Denied,
            Array.AsReadOnly([reason]),
            correlationId,
            traceId,
            policyVersion,
            policyHash);
    }

    /// <summary>
    /// Creates a denied governance decision.
    /// </summary>
    /// <param name="reasons">The reasons associated with the denied decision.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <param name="traceId">Optional trace identifier.</param>
    /// <param name="policyVersion">Optional policy version.</param>
    /// <param name="policyHash">Optional policy hash.</param>
    /// <returns>A denied governance decision.</returns>
    public static GovernanceDecision Deny(
        IEnumerable<OperationReason> reasons,
        string? correlationId = null,
        string? traceId = null,
        string? policyVersion = null,
        string? policyHash = null)
    {
        return new GovernanceDecision(
            GovernanceDecisionOutcome.Denied,
            NormalizeReasons(reasons, DefaultDeniedCode, DefaultDeniedMessage),
            correlationId,
            traceId,
            policyVersion,
            policyHash);
    }

    /// <summary>
    /// Creates a warning governance decision.
    /// </summary>
    /// <param name="code">The machine-readable reason code.</param>
    /// <param name="message">The human-readable reason message.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <param name="traceId">Optional trace identifier.</param>
    /// <param name="policyVersion">Optional policy version.</param>
    /// <param name="policyHash">Optional policy hash.</param>
    /// <returns>A warning governance decision.</returns>
    public static GovernanceDecision Warning(
        string code,
        string message,
        string? correlationId = null,
        string? traceId = null,
        string? policyVersion = null,
        string? policyHash = null)
    {
        return Warning(
            OperationReason.Create(code, message),
            correlationId,
            traceId,
            policyVersion,
            policyHash);
    }

    /// <summary>
    /// Creates a warning governance decision.
    /// </summary>
    /// <param name="reason">The reason associated with the warning decision.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <param name="traceId">Optional trace identifier.</param>
    /// <param name="policyVersion">Optional policy version.</param>
    /// <param name="policyHash">Optional policy hash.</param>
    /// <returns>A warning governance decision.</returns>
    public static GovernanceDecision Warning(
        OperationReason reason,
        string? correlationId = null,
        string? traceId = null,
        string? policyVersion = null,
        string? policyHash = null)
    {
        ArgumentNullException.ThrowIfNull(reason);

        return new GovernanceDecision(
            GovernanceDecisionOutcome.Warning,
            Array.AsReadOnly([reason]),
            correlationId,
            traceId,
            policyVersion,
            policyHash);
    }

    /// <summary>
    /// Creates a warning governance decision.
    /// </summary>
    /// <param name="reasons">The reasons associated with the warning decision.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <param name="traceId">Optional trace identifier.</param>
    /// <param name="policyVersion">Optional policy version.</param>
    /// <param name="policyHash">Optional policy hash.</param>
    /// <returns>A warning governance decision.</returns>
    public static GovernanceDecision Warning(
        IEnumerable<OperationReason> reasons,
        string? correlationId = null,
        string? traceId = null,
        string? policyVersion = null,
        string? policyHash = null)
    {
        return new GovernanceDecision(
            GovernanceDecisionOutcome.Warning,
            NormalizeReasons(reasons, DefaultWarningCode, DefaultWarningMessage),
            correlationId,
            traceId,
            policyVersion,
            policyHash);
    }

    /// <summary>
    /// Creates a deferred governance decision.
    /// </summary>
    /// <param name="code">The machine-readable reason code.</param>
    /// <param name="message">The human-readable reason message.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <param name="traceId">Optional trace identifier.</param>
    /// <param name="policyVersion">Optional policy version.</param>
    /// <param name="policyHash">Optional policy hash.</param>
    /// <returns>A deferred governance decision.</returns>
    public static GovernanceDecision Defer(
        string code,
        string message,
        string? correlationId = null,
        string? traceId = null,
        string? policyVersion = null,
        string? policyHash = null)
    {
        return new GovernanceDecision(
            GovernanceDecisionOutcome.Deferred,
            Array.AsReadOnly([OperationReason.Create(code, message)]),
            correlationId,
            traceId,
            policyVersion,
            policyHash);
    }

    /// <summary>
    /// Creates an acknowledgment-required governance decision.
    /// </summary>
    /// <param name="code">The machine-readable reason code.</param>
    /// <param name="message">The human-readable reason message.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <param name="traceId">Optional trace identifier.</param>
    /// <param name="policyVersion">Optional policy version.</param>
    /// <param name="policyHash">Optional policy hash.</param>
    /// <returns>An acknowledgment-required governance decision.</returns>
    public static GovernanceDecision RequireAcknowledgment(
        string code,
        string message,
        string? correlationId = null,
        string? traceId = null,
        string? policyVersion = null,
        string? policyHash = null)
    {
        return new GovernanceDecision(
            GovernanceDecisionOutcome.AcknowledgmentRequired,
            Array.AsReadOnly([OperationReason.Create(code, message)]),
            correlationId,
            traceId,
            policyVersion,
            policyHash);
    }

    /// <summary>
    /// Creates an escalation-recommended governance decision.
    /// </summary>
    /// <param name="code">The machine-readable reason code.</param>
    /// <param name="message">The human-readable reason message.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <param name="traceId">Optional trace identifier.</param>
    /// <param name="policyVersion">Optional policy version.</param>
    /// <param name="policyHash">Optional policy hash.</param>
    /// <returns>An escalation-recommended governance decision.</returns>
    public static GovernanceDecision Escalate(
        string code,
        string message,
        string? correlationId = null,
        string? traceId = null,
        string? policyVersion = null,
        string? policyHash = null)
    {
        return new GovernanceDecision(
            GovernanceDecisionOutcome.EscalationRecommended,
            Array.AsReadOnly([OperationReason.Create(code, message)]),
            correlationId,
            traceId,
            policyVersion,
            policyHash);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
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
