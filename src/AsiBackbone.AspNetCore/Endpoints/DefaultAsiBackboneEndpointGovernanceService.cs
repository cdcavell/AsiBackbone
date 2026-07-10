using AsiBackbone.AspNetCore.Actors;
using AsiBackbone.AspNetCore.Correlation;
using AsiBackbone.AspNetCore.Handshakes;
using AsiBackbone.AspNetCore.Results;
using AsiBackbone.Core.Actors;
using AsiBackbone.Core.Audit;
using AsiBackbone.Core.Constraints;
using AsiBackbone.Core.Decisions;
using AsiBackbone.Core.Evaluation;
using AsiBackbone.Core.Metadata;
using AsiBackbone.Core.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AsiBackbone.AspNetCore.Endpoints;

/// <summary>
/// Default host-adapter implementation for ergonomic ASP.NET Core endpoint governance.
/// </summary>
public sealed class DefaultAsiBackboneEndpointGovernanceService : IAsiBackboneEndpointGovernanceService
{
    private const string MetadataSanitizationDeniedReasonCode = "endpoint.metadata_sanitization.denied";
    private const string MetadataSanitizationDeniedReasonMessage =
        "Endpoint governance metadata was denied by the configured sanitation policy.";

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

        var optionalServices = new EndpointGovernanceOptionalServiceResolver(httpContext.RequestServices, serviceProvider);
        Asi