using CDCavell.ASIBackbone.Core.Actors;
using CDCavell.ASIBackbone.Core.Decisions;

namespace CDCavell.ASIBackbone.Core.Handshakes;

/// <summary>
/// Represents a framework-neutral liability or responsibility handshake request before consequential execution.
/// </summary>
public sealed class LiabilityHandshakeRequest
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new Dictionary<string, string>(StringComparer.Ordinal);

    private LiabilityHandshakeRequest(
        string handshakeId,
        string actorId,
        AsiBackboneActorType actorType,
        string? actorDisplayName,
        string operationName,
        string reasonCode,
        string message,
        string requiredAcknowledgmentCode,
        string requiredAcknowledgmentText,
        LiabilityHandshakeRiskLevel riskLevel,
        string? riskCategory,
        string? correlationId,
        string? traceId,
        string? policyVersion,
        string? policyHash,
        IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handshakeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredAcknowledgmentCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredAcknowledgmentText);

        HandshakeId = handshakeId.Trim();
        ActorId = actorId.Trim();
        ActorType = actorType;
        ActorDisplayName = NormalizeOptional(actorDisplayName);
        OperationName = operationName.Trim();
        ReasonCode = reasonCode.Trim();
        Message = message.Trim();
        RequiredAcknowledgmentCode = requiredAcknowledgmentCode.Trim();
        RequiredAcknowledgmentText = requiredAcknowledgmentText.Trim();
        RiskLevel = riskLevel;
        RiskCategory = NormalizeOptional(riskCategory);
        CorrelationId = NormalizeOptional(correlationId);
        TraceId = NormalizeOptional(traceId);
        PolicyVersion = NormalizeOptional(policyVersion);
        PolicyHash = NormalizeOptional(policyHash);
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the stable handshake identifier.
    /// </summary>
    public string HandshakeId { get; }

    /// <summary>
    /// Gets the stable actor identifier associated with the handshake.
    /// </summary>
    public string ActorId { get; }

    /// <summary>
    /// Gets the actor type associated with the handshake.
    /// </summary>
    public AsiBackboneActorType ActorType { get; }

    /// <summary>
    /// Gets the optional display name or label associated with the actor.
    /// </summary>
    public string? ActorDisplayName { get; }

    /// <summary>
    /// Gets the operation name requiring acknowledgment.
    /// </summary>
    public string OperationName { get; }

    /// <summary>
    /// Gets the machine-readable reason code explaining why the handshake is required.
    /// </summary>
    public string ReasonCode { get; }

    /// <summary>
    /// Gets the human-readable message explaining why the handshake is required.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the required acknowledgment code the host may display or require before execution.
    /// </summary>
    public string RequiredAcknowledgmentCode { get; }

    /// <summary>
    /// Gets the required acknowledgment text the host may display before execution.
    /// </summary>
    public string RequiredAcknowledgmentText { get; }

    /// <summary>
    /// Gets the risk level associated with the handshake.
    /// </summary>
    public LiabilityHandshakeRiskLevel RiskLevel { get; }

    /// <summary>
    /// Gets the optional host-defined risk category associated with the handshake.
    /// </summary>
    public string? RiskCategory { get; }

    /// <summary>
    /// Gets the correlation identifier associated with the handshake, when supplied by the host.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets the trace identifier associated with the handshake, when supplied by the host.
    /// </summary>
    public string? TraceId { get; }

    /// <summary>
    /// Gets the policy version associated with the handshake, when supplied by the host.
    /// </summary>
    public string? PolicyVersion { get; }

    /// <summary>
    /// Gets the policy hash associated with the handshake, when supplied by the host.
    /// </summary>
    public string? PolicyHash { get; }

    /// <summary>
    /// Gets additional framework-neutral handshake metadata supplied by the host.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets a value indicating whether this handshake contains metadata.
    /// </summary>
    public bool HasMetadata => Metadata.Count > 0;

    /// <summary>
    /// Creates a liability or responsibility handshake request.
    /// </summary>
    /// <param name="actor">The actor associated with the handshake.</param>
    /// <param name="operationName">The operation name requiring acknowledgment.</param>
    /// <param name="reasonCode">The machine-readable reason code.</param>
    /// <param name="message">The human-readable reason message.</param>
    /// <param name="requiredAcknowledgmentCode">The required acknowledgment code.</param>
    /// <param name="requiredAcknowledgmentText">The required acknowledgment text.</param>
    /// <param name="riskLevel">The risk level associated with the handshake.</param>
    /// <param name="riskCategory">Optional host-defined risk category.</param>
    /// <param name="handshakeId">Optional handshake identifier. When omitted, a new identifier is generated.</param>
    /// <param name="correlationId">Optional correlation identifier.</param>
    /// <param name="traceId">Optional trace identifier.</param>
    /// <param name="policyVersion">Optional policy version.</param>
    /// <param name="policyHash">Optional policy hash.</param>
    /// <param name="metadata">Optional host-provided metadata.</param>
    /// <returns>A liability handshake request.</returns>
    public static LiabilityHandshakeRequest Create(
        IAsiBackboneActorContext actor,
        string operationName,
        string reasonCode,
        string message,
        string requiredAcknowledgmentCode,
        string requiredAcknowledgmentText,
        LiabilityHandshakeRiskLevel riskLevel = LiabilityHandshakeRiskLevel.Unspecified,
        string? riskCategory = null,
        string? handshakeId = null,
        string? correlationId = null,
        string? traceId = null,
        string? policyVersion = null,
        string? policyHash = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(actor);

        return new LiabilityHandshakeRequest(
            NormalizeIdentifier(handshakeId),
            actor.ActorId,
            actor.ActorType,
            actor.DisplayName,
            operationName,
            reasonCode,
            message,
            requiredAcknowledgmentCode,
            requiredAcknowledgmentText,
            riskLevel,
            riskCategory,
            correlationId,
            traceId,
            policyVersion,
            policyHash,
            NormalizeMetadata(metadata));
    }

    /// <summary>
    /// Creates a liability or responsibility handshake request from a governance decision.
    /// </summary>
    /// <param name="actor">The actor associated with the handshake.</param>
    /// <param name="operationName">The operation name requiring acknowledgment.</param>
    /// <param name="decision">The governance decision requiring acknowledgment.</param>
    /// <param name="requiredAcknowledgmentCode">The required acknowledgment code.</param>
    /// <param name="requiredAcknowledgmentText">The required acknowledgment text.</param>
    /// <param name="riskLevel">The risk level associated with the handshake.</param>
    /// <param name="riskCategory">Optional host-defined risk category.</param>
    /// <param name="handshakeId">Optional handshake identifier. When omitted, a new identifier is generated.</param>
    /// <param name="metadata">Optional host-provided metadata.</param>
    /// <returns>A liability handshake request.</returns>
    public static LiabilityHandshakeRequest FromDecision(
        IAsiBackboneActorContext actor,
        string operationName,
        GovernanceDecision decision,
        string requiredAcknowledgmentCode,
        string requiredAcknowledgmentText,
        LiabilityHandshakeRiskLevel riskLevel = LiabilityHandshakeRiskLevel.Unspecified,
        string? riskCategory = null,
        string? handshakeId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(decision);

        string reasonCode = decision.ReasonCodes.Count > 0
            ? decision.ReasonCodes[0]
            : "handshake.required";

        string message = decision.Reasons.Count > 0
            ? decision.Reasons[0].Message
            : "Acknowledgment is required before proceeding.";

        return Create(
            actor,
            operationName,
            reasonCode,
            message,
            requiredAcknowledgmentCode,
            requiredAcknowledgmentText,
            riskLevel,
            riskCategory,
            handshakeId,
            decision.CorrelationId,
            decision.TraceId,
            decision.PolicyVersion,
            decision.PolicyHash,
            metadata);
    }

    private static string NormalizeIdentifier(string? identifier)
    {
        return string.IsNullOrWhiteSpace(identifier)
            ? Guid.NewGuid().ToString("N")
            : identifier.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static IReadOnlyDictionary<string, string> NormalizeMetadata(
        IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return EmptyMetadata;
        }

        Dictionary<string, string> normalizedMetadata = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> item in metadata)
        {
            if (string.IsNullOrWhiteSpace(item.Key))
            {
                continue;
            }

            normalizedMetadata[item.Key.Trim()] = item.Value?.Trim() ?? string.Empty;
        }

        return normalizedMetadata.Count == 0
            ? EmptyMetadata
            : normalizedMetadata;
    }
}
