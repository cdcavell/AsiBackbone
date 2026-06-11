using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Decisions;
using CDCavell.AsiBackbone.Core.Handshakes;
using Microsoft.Extensions.Options;

namespace CDCavell.AsiBackbone.AspNetCore.Handshakes;

/// <summary>
/// Provides the default ASP.NET Core-friendly acknowledgment challenge service.
/// </summary>
public sealed class DefaultAsiBackboneAcknowledgmentChallengeService : IAsiBackboneAcknowledgmentChallengeService
{
    private const string ChallengeNotRequiredCode = "acknowledgment.challenge.not_required";
    private const string ChallengeMismatchCode = "acknowledgment.challenge.mismatch";
    private const string ChallengeCodeMismatchCode = "acknowledgment.challenge.code_mismatch";

    private readonly AsiBackboneAcknowledgmentChallengeOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAsiBackboneAcknowledgmentChallengeService" /> class.
    /// </summary>
    /// <param name="options">The acknowledgment challenge options.</param>
    public DefaultAsiBackboneAcknowledgmentChallengeService(IOptions<AsiBackboneAcknowledgmentChallengeOptions> options)
    {
        this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        this.options.Validate();
    }

    /// <inheritdoc />
    public AsiBackboneAcknowledgmentChallenge CreateChallenge(
        IAsiBackboneActorContext actor,
        string operationName,
        GovernanceDecision decision,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(decision);

        if (!decision.RequiresAcknowledgment)
        {
            throw new InvalidOperationException("Only acknowledgment-required governance decisions can be converted into acknowledgment challenges.");
        }

        LiabilityHandshakeRequest request = LiabilityHandshakeRequest.FromDecision(
            actor,
            operationName,
            decision,
            options.RequiredAcknowledgmentCode,
            options.RequiredAcknowledgmentText,
            options.RiskLevel,
            options.RiskCategory,
            metadata: metadata);

        return AsiBackboneAcknowledgmentChallenge.FromHandshakeRequest(request, options);
    }

    /// <inheritdoc />
    public AsiBackboneAcknowledgmentChallengeResult HandleResponse(
        AsiBackboneAcknowledgmentChallenge challenge,
        IAsiBackboneActorContext actor,
        AsiBackboneAcknowledgmentChallengeRequest response,
        DateTimeOffset? occurredUtc = null)
    {
        ArgumentNullException.ThrowIfNull(challenge);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(response);

        if (!string.Equals(challenge.HandshakeId, response.HandshakeId?.Trim(), StringComparison.Ordinal))
        {
            return AsiBackboneAcknowledgmentChallengeResult.Failure(
                ChallengeMismatchCode,
                "The acknowledgment response did not match the active challenge.");
        }

        if (!string.Equals(challenge.RequiredAcknowledgmentCode, response.AcknowledgmentCode?.Trim(), StringComparison.Ordinal))
        {
            return AsiBackboneAcknowledgmentChallengeResult.Failure(
                ChallengeCodeMismatchCode,
                "The acknowledgment response did not contain the required acknowledgment code.");
        }

        LiabilityHandshakeAcknowledgment acknowledgment = LiabilityHandshakeAcknowledgment.Create(
            challenge.HandshakeRequest,
            actor,
            response.Acknowledged,
            occurredUtc: occurredUtc,
            metadata: response.Metadata);

        return AsiBackboneAcknowledgmentChallengeResult.Success(acknowledgment);
    }
}
