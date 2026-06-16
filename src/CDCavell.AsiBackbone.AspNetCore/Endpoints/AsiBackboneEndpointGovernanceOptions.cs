using Microsoft.AspNetCore.Http;

namespace CDCavell.AsiBackbone.AspNetCore.Endpoints;

/// <summary>
/// Provides options for ergonomic ASP.NET Core endpoint governance integration.
/// </summary>
public sealed class AsiBackboneEndpointGovernanceOptions
{
    /// <summary>
    /// Gets or sets the policy version attached to generated endpoint governance evaluation contexts.
    /// </summary>
    public string? PolicyVersion { get; set; }

    /// <summary>
    /// Gets or sets the policy hash attached to generated endpoint governance evaluation contexts.
    /// </summary>
    public string? PolicyHash { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether policy metadata should fail closed when no policy evaluator is configured.
    /// </summary>
    public bool FailClosedWhenPolicyEvaluatorMissing { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether capability metadata should fail closed when no capability validator is configured.
    /// </summary>
    public bool FailClosedWhenCapabilityValidatorMissing { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether audit metadata should fail closed when no host-owned audit sink is configured.
    /// </summary>
    public bool FailClosedWhenAuditSinkMissing { get; set; } = true;

    /// <summary>
    /// Gets or sets the HTTP status code used when endpoint governance is missing required host configuration.
    /// </summary>
    public int ConfigurationFailureStatusCode { get; set; } = StatusCodes.Status500InternalServerError;

    /// <summary>
    /// Gets or sets the HTTP status code used when endpoint capability validation fails before policy evaluation returns a stricter status.
    /// </summary>
    public int CapabilityFailureStatusCode { get; set; } = StatusCodes.Status403Forbidden;

    /// <summary>
    /// Gets or sets the HTTP status code used when a governance decision requires acknowledgment and the endpoint requested a liability handshake.
    /// </summary>
    public int AcknowledgmentChallengeStatusCode { get; set; } = StatusCodes.Status428PreconditionRequired;

    /// <summary>
    /// Validates endpoint governance options.
    /// </summary>
    public void Validate()
    {
        ValidateStatusCode(ConfigurationFailureStatusCode, nameof(ConfigurationFailureStatusCode));
        ValidateStatusCode(CapabilityFailureStatusCode, nameof(CapabilityFailureStatusCode));
        ValidateStatusCode(AcknowledgmentChallengeStatusCode, nameof(AcknowledgmentChallengeStatusCode));
    }

    private static void ValidateStatusCode(int statusCode, string propertyName)
    {
        if (statusCode is < 100 or > 599)
        {
            throw new InvalidOperationException($"{propertyName} must be a valid HTTP status code.");
        }
    }
}
