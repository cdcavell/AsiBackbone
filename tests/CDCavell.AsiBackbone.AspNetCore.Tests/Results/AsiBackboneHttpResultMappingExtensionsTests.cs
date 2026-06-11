using CDCavell.AsiBackbone.AspNetCore.Results;
using CDCavell.AsiBackbone.Core.Decisions;
using CDCavell.AsiBackbone.Core.Results;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace CDCavell.AsiBackbone.AspNetCore.Tests.Results;

public sealed class AsiBackboneHttpResultMappingExtensionsTests
{
    [Fact]
    public async Task ToHttpResultMapsAllowedDecisionToSuccessResponse()
    {
        var decision = GovernanceDecision.Allow(correlationId: " correlation-123 ");

        var capture = await ExecuteAsync(decision.ToHttpResult());

        Assert.Equal(StatusCodes.Status200OK, capture.StatusCode);
        Assert.Contains("\"allowed\":true", capture.Body, StringComparison.Ordinal);
        Assert.Contains("Allowed", capture.Body, StringComparison.Ordinal);
        Assert.Contains("correlation-123", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsWarningDecisionToConfiguredWarningStatusCode()
    {
        var decision = GovernanceDecision.Warning("policy.warning", "Warning detail.");
        var options = new AsiBackboneHttpResultMappingOptions
        {
            WarningStatusCode = StatusCodes.Status206PartialContent,
        };

        var capture = await ExecuteAsync(decision.ToHttpResult(options));

        Assert.Equal(StatusCodes.Status206PartialContent, capture.StatusCode);
        Assert.Contains("Warning", capture.Body, StringComparison.Ordinal);
        Assert.Contains("policy.warning", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsDeniedDecisionToProblemDetails()
    {
        var decision = GovernanceDecision.Deny(
            "policy.denied",
            "Sensitive policy internals.",
            correlationId: "correlation-deny",
            traceId: "trace-deny",
            policyVersion: "v1",
            policyHash: "hash-secret");

        var capture = await ExecuteAsync(decision.ToHttpResult());

        Assert.Equal(StatusCodes.Status403Forbidden, capture.StatusCode);
        Assert.Contains("Governance decision denied execution.", capture.Body, StringComparison.Ordinal);
        Assert.Contains("policy.denied", capture.Body, StringComparison.Ordinal);
        Assert.Contains("correlation-deny", capture.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("Sensitive policy internals", capture.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("trace-deny", capture.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("hash-secret", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsDeferredDecisionToAcceptedProblemDetails()
    {
        var decision = GovernanceDecision.Defer("policy.deferred", "Try again later.");

        var capture = await ExecuteAsync(decision.ToHttpResult());

        Assert.Equal(StatusCodes.Status202Accepted, capture.StatusCode);
        Assert.Contains("Governance decision deferred execution.", capture.Body, StringComparison.Ordinal);
        Assert.Contains("policy.deferred", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsAcknowledgmentRequiredDecisionToPreconditionProblemDetails()
    {
        var decision = GovernanceDecision.RequireAcknowledgment("ack.required", "User acknowledgment required.");

        var capture = await ExecuteAsync(decision.ToHttpResult());

        Assert.Equal(StatusCodes.Status428PreconditionRequired, capture.StatusCode);
        Assert.Contains("Governance decision requires acknowledgment.", capture.Body, StringComparison.Ordinal);
        Assert.Contains("ack.required", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsEscalationRecommendedDecisionToConflictProblemDetails()
    {
        var decision = GovernanceDecision.Escalate("escalation.required", "Manual review required.");

        var capture = await ExecuteAsync(decision.ToHttpResult());

        Assert.Equal(StatusCodes.Status409Conflict, capture.StatusCode);
        Assert.Contains("Governance decision recommends escalation.", capture.Body, StringComparison.Ordinal);
        Assert.Contains("escalation.required", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultCanExposeReasonMessagesAndDiagnosticMetadataWhenConfigured()
    {
        var decision = GovernanceDecision.Deny(
            "policy.denied",
            "Host-approved public detail.",
            correlationId: "correlation-public",
            traceId: "trace-public",
            policyVersion: "v2",
            policyHash: "hash-public");
        var options = new AsiBackboneHttpResultMappingOptions
        {
            IncludeReasonMessages = true,
            IncludeTraceId = true,
            IncludePolicyMetadata = true,
        };

        var capture = await ExecuteAsync(decision.ToHttpResult(options));

        Assert.Contains("Host-approved public detail.", capture.Body, StringComparison.Ordinal);
        Assert.Contains("trace-public", capture.Body, StringComparison.Ordinal);
        Assert.Contains("v2", capture.Body, StringComparison.Ordinal);
        Assert.Contains("hash-public", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsSuccessfulOperationResultToSuccessResponse()
    {
        var result = OperationResult.Success(["Completed with warning."]);

        var capture = await ExecuteAsync(result.ToHttpResult());

        Assert.Equal(StatusCodes.Status200OK, capture.StatusCode);
        Assert.Contains("\"succeeded\":true", capture.Body, StringComparison.Ordinal);
        Assert.Contains("Completed with warning.", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsFailedOperationResultToProblemDetailsWithoutReasonMessagesByDefault()
    {
        var result = OperationResult.Failure("operation.denied", "Sensitive failure detail.");

        var capture = await ExecuteAsync(result.ToHttpResult());

        Assert.Equal(StatusCodes.Status400BadRequest, capture.StatusCode);
        Assert.Contains("operation.denied", capture.Body, StringComparison.Ordinal);
        Assert.Contains("The operation did not complete successfully.", capture.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("Sensitive failure detail", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void ToHttpResultRejectsNullDecision()
    {
        GovernanceDecision? decision = null;

        _ = Assert.Throws<ArgumentNullException>(() => decision!.ToHttpResult());
    }

    [Fact]
    public void ResultMappingOptionsRejectInvalidStatusCode()
    {
        var options = new AsiBackboneHttpResultMappingOptions
        {
            DeniedStatusCode = 99,
        };

        var exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains(nameof(AsiBackboneHttpResultMappingOptions.DeniedStatusCode), exception.Message, StringComparison.Ordinal);
    }

    private static async Task<HttpResultCapture> ExecuteAsync(IResult result)
    {
        var httpContext = new DefaultHttpContext();
        await using var body = new MemoryStream();
        httpContext.Response.Body = body;

        await result.ExecuteAsync(httpContext);

        _ = body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(body);
        string content = await reader.ReadToEndAsync();

        return new HttpResultCapture(httpContext.Response.StatusCode, content);
    }

    private sealed record HttpResultCapture(int StatusCode, string Body);
}
