using CDCavell.AsiBackbone.AspNetCore.Correlation;
using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Audit;
using CDCavell.AsiBackbone.Core.Decisions;
using Xunit;

namespace CDCavell.AsiBackbone.AspNetCore.Tests.Correlation;

public sealed class AsiBackboneHttpRequestCorrelationAuditExtensionsTests
{
    [Fact]
    public void CreateAuditResidueUsesRequestCorrelationBeforeDecisionCorrelation()
    {
        AsiBackboneHttpRequestCorrelation correlation = new(
            correlationId: "request-correlation",
            traceId: "request-trace");
        GovernanceDecision decision = GovernanceDecision.Allow(
            correlationId: "decision-correlation",
            traceId: "decision-trace",
            policyVersion: "v1",
            policyHash: "hash-1");

        AuditResidue residue = correlation.CreateAuditResidue(
            AsiBackboneActorContext.System,
            "operate",
            decision);

        Assert.Equal("request-correlation", residue.CorrelationId);
        Assert.Equal("request-trace", residue.TraceId);
        Assert.Equal("v1", residue.PolicyVersion);
        Assert.Equal("hash-1", residue.PolicyHash);
    }

    [Fact]
    public void CreateAuditResidueFallsBackToDecisionCorrelationWhenRequestCorrelationIsMissing()
    {
        AsiBackboneHttpRequestCorrelation correlation = new();
        GovernanceDecision decision = GovernanceDecision.Allow(
            correlationId: "decision-correlation",
            traceId: "decision-trace");

        AuditResidue residue = correlation.CreateAuditResidue(
            AsiBackboneActorContext.System,
            "operate",
            decision);

        Assert.Equal("decision-correlation", residue.CorrelationId);
        Assert.Equal("decision-trace", residue.TraceId);
    }

    [Fact]
    public void CreateAuditResidueMergesSafeRequestMetadataWithHostMetadata()
    {
        AsiBackboneHttpRequestCorrelation correlation = new(
            metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [AsiBackboneHttpRequestMetadataKeys.Method] = "POST",
                [AsiBackboneHttpRequestMetadataKeys.RoutePattern] = "/api/widgets/{id}",
            });
        GovernanceDecision decision = GovernanceDecision.Allow();
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            ["operation.scope"] = "test",
        };

        AuditResidue residue = correlation.CreateAuditResidue(
            AsiBackboneActorContext.System,
            "operate",
            decision,
            metadata: metadata);

        Assert.Equal("POST", residue.Metadata[AsiBackboneHttpRequestMetadataKeys.Method]);
        Assert.Equal("/api/widgets/{id}", residue.Metadata[AsiBackboneHttpRequestMetadataKeys.RoutePattern]);
        Assert.Equal("test", residue.Metadata["operation.scope"]);
    }

    [Fact]
    public void ToEvaluationContextPropagatesCorrelationAndSafeMetadata()
    {
        AsiBackboneHttpRequestCorrelation correlation = new(
            correlationId: "request-correlation",
            traceId: "request-trace",
            metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [AsiBackboneHttpRequestMetadataKeys.Method] = "GET",
            });

        var context = correlation.ToEvaluationContext(
            policyVersion: "v2",
            policyHash: "hash-2",
            metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["operation.scope"] = "policy",
            });

        Assert.Equal("request-correlation", context.CorrelationId);
        Assert.Equal("v2", context.PolicyVersion);
        Assert.Equal("hash-2", context.PolicyHash);
        Assert.Equal("GET", context.Metadata[AsiBackboneHttpRequestMetadataKeys.Method]);
        Assert.Equal("policy", context.Metadata["operation.scope"]);
    }
}