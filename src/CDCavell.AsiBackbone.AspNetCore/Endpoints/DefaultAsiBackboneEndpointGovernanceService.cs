using CDCavell.AsiBackbone.AspNetCore.Actors;
using CDCavell.AsiBackbone.AspNetCore.Correlation;
using CDCavell.AsiBackbone.AspNetCore.Handshakes;
using CDCavell.AsiBackbone.AspNetCore.Results;
using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Audit;
using CDCavell.AsiBackbone.Core.Constraints;
using CDCavell.AsiBackbone.Core.Decisions;
using CDCavell.AsiBackbone.Core.Evaluation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CDCavell.AsiBackbone.AspNetCore.Endpoints;

/// <summary>
/// Default host-adapter implementation for ergonomic ASP.NET Core endpoint governance.
/// </summary>
public sealed class DefaultAsiBackboneEndpointGovernanceService : IAsiBackboneEndpointGovernanceService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IAsiBackboneHttpActorContextResolver actorContextResolver;
    private readonly IAsiBackboneHttpRequestCorrelationResolver requestCorrelationResolver;
    private readonly IAsiBackboneAcknowledgmentChallengeService acknowledgmentChallengeService;
    private readonly AsiBackboneEndpointGovernanceOptions endpointOptions;
    private readonly AsiBackboneHttpResultMappingOptions resultOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAsiBackboneEndpointGovernanceService" /> class.
    /// </summary>
    public DefaultAsiBackboneEndpointGovernanceService(
        IServiceProvider serviceProvider,
        IAsiBackboneHttpActorContextResolver actorContextResolver,
        IAsiBackboneHttpRequestCorrelationResolver requestCorrelationResolver,
        IAsiBackboneAcknowledgmentChallengeService acknowledgmentChallengeService,
        IOptions<AsiBackboneEndpointGovernanceOptions> endpointOptions,
        IOptions<AsiBackboneHttpResultMappingOptions> resultOptions)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.actorContextResolver = actorContextResolver ?? throw new ArgumentNullException(nameof(actorContextResolver));
        this.requestCorrelationResolver = requestCorrelationResolver ?? throw new ArgumentNullException(nameof(requestCorrelationResolver));
        this.acknowledgmentChallengeService = acknowledgmentChallengeService ?? throw new ArgumentNullException(nameof(acknowledgmentChallengeService));
        this.endpointOptions = endpointOptions?.Value ?? throw new ArgumentNullException(nameof(endpointOptions));
        this.resultOptions = resultOptions?.Value ?? throw new ArgumentNullException(nameof(resultOptions));
        this.endpointOptions.Validate();
        this.resultOptions.Validate();
    }

    /// <inheritdoc />
    public async ValueTask<AsiBackboneEndpointGovernanceResult> EvaluateAsync(
        HttpContext httpContext,
        AsiBackboneEndpointGovernanceDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(descriptor);
        cancellationToken.ThrowIfCancellationRequested();

        if (!descriptor.HasGovernanceMetadata)
        {
            return AsiBackboneEndpointGovernanceResult.Allow();
        }

        AsiBackboneHttpRequestCorrelation correlation = requestCorrelationResolver.ResolveRequestCorrelation();
        IReadOnlyDictionary<string, string> endpointMetadata = descriptor.ToMetadata();
        AsiBackboneConstraintEvaluationContext evaluationContext = correlation.ToEvaluationContext(
            endpointOptions.PolicyVersion,
            endpointOptions.PolicyHash,
            endpointMetadata);

        var decision = GovernanceDecision.Allow(
            correlationId: evaluationContext.CorrelationId,
            traceId: correlation.TraceId,
            policyVersion: evaluationContext.PolicyVersion,
            policyHash: evaluationContext.PolicyHash);

        if (descriptor.PolicyTypes.Count > 0)
        {
            IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>? evaluator =
                serviceProvider.GetService<IAsiBackbonePolicyEvaluator<AsiBackboneConstraintEvaluationContext>>();

            if (evaluator is null)
            {
                return endpointOptions.FailClosedWhenPolicyEvaluatorMissing
                    ? CreateConfigurationFailure(
                        "endpoint.policy_evaluator.missing",
                        "Endpoint governance policy metadata was present, but no AsiBackbone policy evaluator was registered.",
                        decision)
                    : AsiBackboneEndpointGovernanceResult.Allow(decision);
            }

            decision = await evaluator
                .EvaluateAsync(evaluationContext, cancellationToken)
                .ConfigureAwait(false);
        }

        if (decision.CanProceed && descriptor.CapabilityScopes.Count > 0)
        {
            IAsiBackboneEndpointCapabilityGrantValidator? capabilityValidator =
                serviceProvider.GetService<IAsiBackboneEndpointCapabilityGrantValidator>();

            if (capabilityValidator is null)
            {
                return endpointOptions.FailClosedWhenCapabilityValidatorMissing
                    ? CreateCapabilityFailure(
                        "endpoint.capability_validator.missing",
                        "Endpoint capability metadata was present, but no host-owned endpoint capability validator was registered.",
                        decision)
                    : AsiBackboneEndpointGovernanceResult.Allow(decision);
            }

            decision = await capabilityValidator
                .ValidateAsync(httpContext, descriptor, decision, cancellationToken)
                .ConfigureAwait(false);
        }

        IAsiBackboneActorContext actor = actorContextResolver.ResolveActorContext();

        if (descriptor.EmitGovernanceAudit)
        {
            IAsiBackboneAuditSink? auditSink = serviceProvider.GetService<IAsiBackboneAuditSink>();
            if (auditSink is null)
            {
                return endpointOptions.FailClosedWhenAuditSinkMissing
                    ? CreateConfigurationFailure(
                        "endpoint.audit_sink.missing",
                        "Endpoint governance audit emission was requested, but no host-owned audit sink was registered.",
                        decision)
                    : AsiBackboneEndpointGovernanceResult.Allow(decision);
            }

            var residue = AuditResidue.FromDecision(
                actor,
                descriptor.OperationName,
                decision,
                metadata: endpointMetadata,
                decisionStage: "aspnetcore.endpoint.governance");

            await auditSink.WriteAsync(residue, cancellationToken).ConfigureAwait(false);
        }

        if (decision.RequiresAcknowledgment && descriptor.RequiresLiabilityHandshake)
        {
            AsiBackboneAcknowledgmentChallenge challenge = acknowledgmentChallengeService.CreateChallenge(
                actor,
                descriptor.OperationName,
                decision,
                endpointMetadata);

            IResult challengeResult = Microsoft.AspNetCore.Http.Results.Json(
                challenge,
                statusCode: endpointOptions.AcknowledgmentChallengeStatusCode);

            return AsiBackboneEndpointGovernanceResult.Challenge(challenge, challengeResult, decision);
        }

        return decision.CanProceed
            ? AsiBackboneEndpointGovernanceResult.Allow(decision)
            : AsiBackboneEndpointGovernanceResult.Block(decision.ToHttpResult(resultOptions), decision);
    }

    private AsiBackboneEndpointGovernanceResult CreateConfigurationFailure(
        string code,
        string message,
        GovernanceDecision currentDecision)
    {
        var decision = GovernanceDecision.Deny(
            code,
            message,
            correlationId: currentDecision.CorrelationId,
            traceId: currentDecision.TraceId,
            policyVersion: currentDecision.PolicyVersion,
            policyHash: currentDecision.PolicyHash);

        return AsiBackboneEndpointGovernanceResult.Block(
            Microsoft.AspNetCore.Http.Results.Problem(
                title: "Endpoint governance configuration is incomplete.",
                detail: message,
                statusCode: endpointOptions.ConfigurationFailureStatusCode,
                extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["reasonCodes"] = decision.ReasonCodes,
                    ["outcome"] = decision.Outcome.ToString()
                }),
            decision);
    }

    private AsiBackboneEndpointGovernanceResult CreateCapabilityFailure(
        string code,
        string message,
        GovernanceDecision currentDecision)
    {
        var decision = GovernanceDecision.Deny(
            code,
            message,
            correlationId: currentDecision.CorrelationId,
            traceId: currentDecision.TraceId,
            policyVersion: currentDecision.PolicyVersion,
            policyHash: currentDecision.PolicyHash);

        return AsiBackboneEndpointGovernanceResult.Block(
            Microsoft.AspNetCore.Http.Results.Problem(
                title: "Endpoint capability grant validation failed.",
                detail: message,
                statusCode: endpointOptions.CapabilityFailureStatusCode,
                extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["reasonCodes"] = decision.ReasonCodes,
                    ["outcome"] = decision.Outcome.ToString()
                }),
            decision);
    }
}
