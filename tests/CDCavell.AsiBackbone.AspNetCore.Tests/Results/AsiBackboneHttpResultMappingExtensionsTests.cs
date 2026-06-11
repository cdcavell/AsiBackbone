using CDCavell.AsiBackbone.AspNetCore.Results;
using CDCavell.AsiBackbone.Core.Decisions;
using CDCavell.AsiBackbone.Core.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CDCavell.AsiBackbone.AspNetCore.Tests.Results;

public sealed class AsiBackboneHttpResultMappingExtensionsTests
{
    [Fact]
    public async Task ToHttpResultMapsAllowedDecisionToSuccessResponse()
    {
        var decision = GovernanceDecision.Allow(correlationId: " correlation-123 ");

        HttpResultCapture capture = await ExecuteAsync(decision.ToHttpResult());

        Assert.Equal(StatusCodes.Status200OK, capture.StatusCode);
        Assert.Contains("\"allowed\":true", capture.Body, StringComparison.Ordinal);
        Assert.Contains("Allowed", capture.Body, StringComparison.Ordinal);
        Assert.Contains("correlation-123", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsWarningDecisionToConfiguredWarningStatusCode()
    {
        var decision = GovernanceDecision.Warning("policy.warning", "Warning detail.");
        var options = new AsiBackboneHttpResultMappingOptions { WarningStatusCode = StatusCodes.Status206PartialContent };

        HttpResultCapture capture = await ExecuteAsync(decision.ToHttpResult(options));

        Assert.Equal(StatusCodes.Status206PartialContent, capture.StatusCode);
        Assert.Contains("Warning", capture.Body, StringComparison.Ordinal);
        Assert.Contains("policy.warning", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsDeniedDecisionToProblemDetails()
    {
        var decision = GovernanceDecision.Deny(
            "policy.denied",
            "Policy detail.",
            correlationId: "correlation-deny",
            traceId: "trace-deny",
            policyVersion: "v1",
            policyHash: "hash-value");

        HttpResultCapture capture = await ExecuteAsync(decision.ToHttpResult());

        Assert.Equal(StatusCodes.Status403Forbidden, capture.StatusCode);
        Assert.Contains("Governance decision denied execution.", capture.Body, StringComparison.Ordinal);
        Assert.Contains("policy.denied", capture.Body, StringComparison.Ordinal);
        Assert.Contains("correlation-deny", capture.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("Policy detail", capture.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("trace-deny", capture.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("hash-value", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsDeferredDecisionToAcceptedProblemDetails()
    {
        var decision = GovernanceDecision.Defer("policy.deferred", "Try again later.");

        HttpResultCapture capture = await ExecuteAsync(decision.ToHttpResult());

        Assert.Equal(StatusCodes.Status202Accepted, capture.StatusCode);
        Assert.Contains("Governance decision deferred execution.", capture.Body, StringComparison.Ordinal);
        Assert.Contains("policy.deferred", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsAcknowledgmentRequiredDecisionToPreconditionProblemDetails()
    {
        var decision = GovernanceDecision.RequireAcknowledgment("ack.required", "User acknowledgment required.");

        HttpResultCapture capture = await ExecuteAsync(decision.ToHttpResult());

        Assert.Equal(StatusCodes.Status428PreconditionRequired, capture.StatusCode);
        Assert.Contains("Governance decision requires acknowledgment.", capture.Body, StringComparison.Ordinal);
        Assert.Contains("ack.required", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsEscalationRecommendedDecisionToConflictProblemDetails()
    {
        var decision = GovernanceDecision.Escalate("escalation.required", "Manual review required.");

        HttpResultCapture capture = await ExecuteAsync(decision.ToHttpResult());

        Assert.Equal(StatusCodes.Status409Conflict, capture.StatusCode);
        Assert.Contains("Governance decision recommends escalation.", capture.Body, StringComparison.Ordinal);
        Assert.Contains("escalation.required", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultCanExposeReasonMessagesAndDiagnosticMetadataWhenConfigured()
    {
        var decision = GovernanceDecision.Deny(
            "policy.denied",
            "Public detail.",
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

        HttpResultCapture capture = await ExecuteAsync(decision.ToHttpResult(options));

        Assert.Contains("Public detail.", capture.Body, StringComparison.Ordinal);
        Assert.Contains("trace-public", capture.Body, StringComparison.Ordinal);
        Assert.Contains("v2", capture.Body, StringComparison.Ordinal);
        Assert.Contains("hash-public", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultOmitsMissingOptionalDecisionMetadataWhenDiagnosticsAreEnabled()
    {
        var decision = GovernanceDecision.Allow();
        var options = new AsiBackboneHttpResultMappingOptions
        {
            IncludeReasonMessages = true,
            IncludeTraceId = true,
            IncludePolicyMetadata = true,
        };

        HttpResultCapture capture = await ExecuteAsync(decision.ToHttpResult(options));

        Assert.Equal(StatusCodes.Status200OK, capture.StatusCode);
        Assert.DoesNotContain("reasonCodes", capture.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("reasonMessages", capture.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("correlationId", capture.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("traceId", capture.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("policyVersion", capture.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("policyHash", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsSuccessfulOperationResultToSuccessResponse()
    {
        var result = OperationResult.Success(["Completed with warning."]);

        HttpResultCapture capture = await ExecuteAsync(result.ToHttpResult());

        Assert.Equal(StatusCodes.Status200OK, capture.StatusCode);
        Assert.Contains("\"succeeded\":true", capture.Body, StringComparison.Ordinal);
        Assert.Contains("Completed with warning.", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsSuccessfulOperationResultWithoutWarnings()
    {
        var result = OperationResult.Success();

        HttpResultCapture capture = await ExecuteAsync(result.ToHttpResult());

        Assert.Equal(StatusCodes.Status200OK, capture.StatusCode);
        Assert.Contains("\"succeeded\":true", capture.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("warnings", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultMapsFailedOperationResultToProblemDetailsWithoutReasonMessagesByDefault()
    {
        var result = OperationResult.Failure("operation.denied", "Failure detail.");

        HttpResultCapture capture = await ExecuteAsync(result.ToHttpResult());

        Assert.Equal(StatusCodes.Status400BadRequest, capture.StatusCode);
        Assert.Contains("operation.denied", capture.Body, StringComparison.Ordinal);
        Assert.Contains("The operation did not complete successfully.", capture.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("Failure detail", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToHttpResultCanExposeFailedOperationReasonMessagesWhenConfigured()
    {
        var result = OperationResult.Failure("operation.denied", "Public operation detail.");
        var options = new AsiBackboneHttpResultMappingOptions
        {
            IncludeReasonMessages = true,
            OperationFailureStatusCode = StatusCodes.Status409Conflict,
            OperationFailureMessage = "Custom safe failure message.",
        };

        HttpResultCapture capture = await ExecuteAsync(result.ToHttpResult(options));

        Assert.Equal(StatusCodes.Status409Conflict, capture.StatusCode);
        Assert.Contains("Custom safe failure message.", capture.Body, StringComparison.Ordinal);
        Assert.Contains("Public operation detail.", capture.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void ToHttpResultRejectsNullDecision()
    {
        GovernanceDecision? decision = null;

        _ = Assert.Throws<ArgumentNullException>(() => decision!.ToHttpResult());
    }

    [Fact]
    public void ToHttpResultRejectsNullDecisionOptions()
    {
        var decision = GovernanceDecision.Allow();
        AsiBackboneHttpResultMappingOptions? options = null;

        _ = Assert.Throws<ArgumentNullException>(() => decision.ToHttpResult(options!));
    }

    [Fact]
    public void ToHttpResultRejectsNullOperationResult()
    {
        OperationResult? result = null;

        _ = Assert.Throws<ArgumentNullException>(() => result!.ToHttpResult());
    }

    [Fact]
    public void ToHttpResultRejectsNullOperationResultOptions()
    {
        var result = OperationResult.Success();
        AsiBackboneHttpResultMappingOptions? options = null;

        _ = Assert.Throws<ArgumentNullException>(() => result.ToHttpResult(options!));
    }

    [Theory]
    [InlineData(nameof(AsiBackboneHttpResultMappingOptions.SuccessStatusCode))]
    [InlineData(nameof(AsiBackboneHttpResultMappingOptions.WarningStatusCode))]
    [InlineData(nameof(AsiBackboneHttpResultMappingOptions.DeniedStatusCode))]
    [InlineData(nameof(AsiBackboneHttpResultMappingOptions.DeferredStatusCode))]
    [InlineData(nameof(AsiBackboneHttpResultMappingOptions.AcknowledgmentRequiredStatusCode))]
    [InlineData(nameof(AsiBackboneHttpResultMappingOptions.EscalationRecommendedStatusCode))]
    [InlineData(nameof(AsiBackboneHttpResultMappingOptions.OperationFailureStatusCode))]
    public void ResultMappingOptionsRejectInvalidStatusCode(string propertyName)
    {
        var options = new AsiBackboneHttpResultMappingOptions();
        SetStatusCode(options, propertyName, 99);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains(propertyName, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(nameof(AsiBackboneHttpResultMappingOptions.GovernanceDecisionNotAllowedMessage))]
    [InlineData(nameof(AsiBackboneHttpResultMappingOptions.OperationFailureMessage))]
    public void ResultMappingOptionsRejectBlankSafeMessages(string propertyName)
    {
        var options = new AsiBackboneHttpResultMappingOptions();
        SetSafeMessage(options, propertyName, "   ");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains("safe", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static void SetStatusCode(
        AsiBackboneHttpResultMappingOptions options,
        string propertyName,
        int statusCode)
    {
        switch (propertyName)
        {
            case nameof(AsiBackboneHttpResultMappingOptions.SuccessStatusCode):
                options.SuccessStatusCode = statusCode;
                break;
            case nameof(AsiBackboneHttpResultMappingOptions.WarningStatusCode):
                options.WarningStatusCode = statusCode;
                break;
            case nameof(AsiBackboneHttpResultMappingOptions.DeniedStatusCode):
                options.DeniedStatusCode = statusCode;
                break;
            case nameof(AsiBackboneHttpResultMappingOptions.DeferredStatusCode):
                options.DeferredStatusCode = statusCode;
                break;
            case nameof(AsiBackboneHttpResultMappingOptions.AcknowledgmentRequiredStatusCode):
                options.AcknowledgmentRequiredStatusCode = statusCode;
                break;
            case nameof(AsiBackboneHttpResultMappingOptions.EscalationRecommendedStatusCode):
                options.EscalationRecommendedStatusCode = statusCode;
                break;
            case nameof(AsiBackboneHttpResultMappingOptions.OperationFailureStatusCode):
                options.OperationFailureStatusCode = statusCode;
                break;
            default:
                throw new InvalidOperationException("Unknown status code option.");
        }
    }

    private static void SetSafeMessage(
        AsiBackboneHttpResultMappingOptions options,
        string propertyName,
        string message)
    {
        switch (propertyName)
        {
            case nameof(AsiBackboneHttpResultMappingOptions.GovernanceDecisionNotAllowedMessage):
                options.GovernanceDecisionNotAllowedMessage = message;
                break;
            case nameof(AsiBackboneHttpResultMappingOptions.OperationFailureMessage):
                options.OperationFailureMessage = message;
                break;
            default:
                throw new InvalidOperationException("Unknown message option.");
        }
    }

    private static async Task<HttpResultCapture> ExecuteAsync(IResult result)
    {
        using ServiceProvider serviceProvider = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
        };
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
