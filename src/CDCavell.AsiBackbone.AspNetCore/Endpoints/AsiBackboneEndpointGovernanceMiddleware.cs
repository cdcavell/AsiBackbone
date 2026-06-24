using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AsiBackbone.AspNetCore.Endpoints;

/// <summary>
/// Middleware that evaluates AsiBackbone endpoint governance metadata before endpoint execution.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AsiBackboneEndpointGovernanceMiddleware" /> class.
/// </remarks>
/// <param name="next">The next request delegate.</param>
public sealed class AsiBackboneEndpointGovernanceMiddleware(RequestDelegate next)
{
    // Example: use a precomputed static result for the most common forbidden response
    private static readonly IResult LightweightForbiddenResult =
        Microsoft.AspNetCore.Http.Results.StatusCode(StatusCodes.Status403Forbidden);
    // Use this for default cases instead of constructing new ProblemDetails every time.

    private readonly RequestDelegate next = next ?? throw new ArgumentNullException(nameof(next));

    /// <summary>
    /// Evaluates endpoint governance metadata and either continues execution or writes a failure result.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="governanceService">The endpoint governance service.</param>
    /// <param name="endpointOptions">The endpoint governance options.</param>
    /// <returns>A task that completes when the middleware has run.</returns>
    public async Task InvokeAsync(
        HttpContext httpContext,
        IAsiBackboneEndpointGovernanceService governanceService,
        IOptions<AsiBackboneEndpointGovernanceOptions> endpointOptions)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(governanceService);
        ArgumentNullException.ThrowIfNull(endpointOptions);

        AsiBackboneEndpointGovernanceOptions options = endpointOptions.Value;
        options.Validate();

        Endpoint? endpoint = httpContext.GetEndpoint();
        var descriptor = AsiBackboneEndpointGovernanceDescriptor.FromEndpoint(endpoint);

        bool endpointAllowsMissingGovernance = endpoint?.Metadata
            .GetMetadata<AllowMissingGovernanceMetadataAttribute>() is not null;

        if (!descriptor.HasGovernanceMetadata)
        {
            if (options.RequireGovernanceMetadata && !endpointAllowsMissingGovernance)
            {
                IResult missingGovernanceResult = CreateDefaultForbiddenResult(
                    httpContext,
                    options,
                    descriptor,
                    result: null,
                    decisionStage: "aspnetcore.endpoint.governance.metadata");

                await missingGovernanceResult.ExecuteAsync(httpContext).ConfigureAwait(false);
                return;
            }

            await next(httpContext).ConfigureAwait(false);
            return;
        }

        AsiBackboneEndpointGovernanceResult result = await governanceService
            .EvaluateAsync(httpContext, descriptor, httpContext.RequestAborted)
            .ConfigureAwait(false);

        if (result.CanExecute)
        {
            await next(httpContext).ConfigureAwait(false);
            return;
        }

        IResult failureResult = result.FailureResult ?? CreateDefaultForbiddenResult(
            httpContext,
            options,
            descriptor,
            result,
            "aspnetcore.endpoint.governance.decision");

        await failureResult.ExecuteAsync(httpContext).ConfigureAwait(false);
    }

    private static IResult CreateDefaultForbiddenResult(
        HttpContext httpContext,
        AsiBackboneEndpointGovernanceOptions options,
        AsiBackboneEndpointGovernanceDescriptor descriptor,
        AsiBackboneEndpointGovernanceResult? result,
        string decisionStage)
    {
        return AsiBackboneEndpointGovernanceDevelopmentDiagnostics.IsEnabled(httpContext, options)
            ? AsiBackboneEndpointGovernanceDevelopmentDiagnostics.CreateProblem(
                httpContext,
                options,
                descriptor,
                result?.Decision,
                decisionStage,
                title: "Endpoint governance blocked execution.",
                detail: "Endpoint governance blocked this request before the selected endpoint executed.",
                statusCode: StatusCodes.Status403Forbidden)
            : options.DefaultForbiddenResultFactory is null
            ? LightweightForbiddenResult
            : options.DefaultForbiddenResultFactory(httpContext)
                ?? throw new InvalidOperationException($"{nameof(AsiBackboneEndpointGovernanceOptions.DefaultForbiddenResultFactory)} must return a non-null result.");
    }
}
