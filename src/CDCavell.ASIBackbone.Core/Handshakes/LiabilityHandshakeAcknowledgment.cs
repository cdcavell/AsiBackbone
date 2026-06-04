using CDCavell.ASIBackbone.Core.Actors;

namespace CDCavell.ASIBackbone.Core.Handshakes;

/// <summary>
/// Represents a framework-neutral acknowledgment response for a liability or responsibility handshake.
/// </summary>
public sealed class LiabilityHandshakeAcknowledgment
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new Dictionary<string, string>(StringComparer.Ordinal);

    private LiabilityHandshakeAcknowledgment(
        string acknowledgmentId,
        string handshakeId,
        string actorId,
        AsiBackboneActorType actorType,
        string? actorDisplayName,
        string acknowledgmentCode,
        bool acknowledged,
        DateTimeOffset occurredUtc,
        string? correlationId,
        string? traceId,
        IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(acknowledgmentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(handshakeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(acknowledgmentCode);

        AcknowledgmentId = acknowledgmentId.Trim();
        HandshakeId = handshakeId.Trim();
        ActorId = actorId.Trim();
        ActorType = actorType;
        ActorDisplayName = NormalizeOptional(actorDisplayName);
        AcknowledgmentCode = acknowledgmentCode.Trim();
        Acknowledged = acknowledged;
        OccurredUtc = occurredUtc.ToUniversalTime();
        CorrelationId = NormalizeOptional(correlationId);
        TraceId = NormalizeOptional(traceId);
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the stable acknowledgment identifier.
    /// </summary>
    public string AcknowledgmentId { get; }

    /// <summary>
    /// Gets the handshake identifier associated with the acknowledgment.
    /// </summary>
    public string HandshakeId { get; }

    /// <summary>
    /// Gets the stable actor identifier associated with the acknowledgment.
    /// </summary>
    public string ActorId { get; }

    /// <summary>
    /// Gets the actor type associated with the acknowledgment.
    /// </summary>
    public AsiBackboneActorType ActorType { get; }

    /// <summary>
    /// Gets the optional display name or label associated with the actor.
    /// </summary>
    public string? ActorDisplayName { get; }

    /// <summary>
    /// Gets the acknowledgment code accepted or rejected by the actor.
    /// </summary>
    public string AcknowledgmentCode { get; }

    /// <summary>
    /// Gets a value indicating whether the required acknowledgment was accepted.
    /// </summary>
    public bool Acknowledged { get; }

    /// <summary>
    /// Gets a value indicating whether the required acknowledgment was rejected.
    /// </summary>
    public bool Rejected => !Acknowledged;

    /// <summary>
    /// Gets the UTC timestamp when the acknowledgment response occurred.
    /// </summary>
    public DateTimeOffset OccurredUtc { get; }

    /// <summary>
    /// Gets the correlation identifier associated with the acknowledgment, when supplied by the host.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets the trace identifier associated with the acknowledgment, when supplied by the host.
    /// </summary>
    public string? TraceId { get; }

    /// <summary>
    /// Gets additional framework-neutral acknowledgment metadata supplied by the host.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets a value indicating whether this acknowledgment contains metadata.
    /// </summary>
    public bool HasMetadata => Metadata.Count > 0;

    /// <summary>
    /// Creates an acknowledgment response for a liability or responsibility handshake.
    /// </summary>
    /// <param name="request">The handshake request being acknowledged.</param>
    /// <param name="actor">The actor responding to the handshake.</param>
    /// <param name="acknowledged">Whether the actor accepted the required acknowledgment.</param>
    /// <param name="acknowledgmentId">Optional acknowledgment identifier. When omitted, a new identifier is generated.</param>
    /// <param name="occurredUtc">Optional response timestamp. When omitted, the current UTC timestamp is used.</param>
    /// <param name="metadata">Optional host-provided metadata.</param>
    /// <returns>A liability handshake acknowledgment response.</returns>
    public static LiabilityHandshakeAcknowledgment Create(
        LiabilityHandshakeRequest request,
        IAsiBackboneActorContext actor,
        bool acknowledged,
        string? acknowledgmentId = null,
        DateTimeOffset? occurredUtc = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(actor);

        return new LiabilityHandshakeAcknowledgment(
            NormalizeIdentifier(acknowledgmentId),
            request.HandshakeId,
            actor.ActorId,
            actor.ActorType,
            actor.DisplayName,
            request.RequiredAcknowledgmentCode,
            acknowledged,
            occurredUtc ?? DateTimeOffset.UtcNow,
            request.CorrelationId,
            request.TraceId,
            NormalizeMetadata(metadata));
    }

    /// <summary>
    /// Creates an accepted acknowledgment response for a liability or responsibility handshake.
    /// </summary>
    /// <param name="request">The handshake request being acknowledged.</param>
    /// <param name="actor">The actor accepting the handshake.</param>
    /// <param name="acknowledgmentId">Optional acknowledgment identifier. When omitted, a new identifier is generated.</param>
    /// <param name="occurredUtc">Optional response timestamp. When omitted, the current UTC timestamp is used.</param>
    /// <param name="metadata">Optional host-provided metadata.</param>
    /// <returns>An accepted liability handshake acknowledgment response.</returns>
    public static LiabilityHandshakeAcknowledgment Accept(
        LiabilityHandshakeRequest request,
        IAsiBackboneActorContext actor,
        string? acknowledgmentId = null,
        DateTimeOffset? occurredUtc = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return Create(request, actor, acknowledged: true, acknowledgmentId, occurredUtc, metadata);
    }

    /// <summary>
    /// Creates a rejected acknowledgment response for a liability or responsibility handshake.
    /// </summary>
    /// <param name="request">The handshake request being rejected.</param>
    /// <param name="actor">The actor rejecting the handshake.</param>
    /// <param name="acknowledgmentId">Optional acknowledgment identifier. When omitted, a new identifier is generated.</param>
    /// <param name="occurredUtc">Optional response timestamp. When omitted, the current UTC timestamp is used.</param>
    /// <param name="metadata">Optional host-provided metadata.</param>
    /// <returns>A rejected liability handshake acknowledgment response.</returns>
    public static LiabilityHandshakeAcknowledgment Reject(
        LiabilityHandshakeRequest request,
        IAsiBackboneActorContext actor,
        string? acknowledgmentId = null,
        DateTimeOffset? occurredUtc = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return Create(request, actor, acknowledged: false, acknowledgmentId, occurredUtc, metadata);
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
