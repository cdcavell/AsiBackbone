using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CDCavell.AsiBackbone.AspNetCore.Endpoints;

/// <summary>
/// Middleware that evaluates AsiBackbone endpoint governance metadata before endpoint execution.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AsiBackboneEndpointGovernanceMiddleware" /> class.
/// </remarks>
/// <param name="next">The next request delegate.</param>
public sealed class AsiBackboneEndpointGovernanceMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate next = next ?? throw new ArgumentNullException(nameof(next));
    private static readonly string[] extensions = ["endpoint.governance_metadata.missing"];

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
                IResult missingGovernanceResult = Microsoft.AspNetCore.Http.Results.Problem(
                    title: "Governance metadata is required for this endpoint.",
                    detail: "The selected endpoint does not contain AsiBackbone governance metadata.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        ["reasonCodes"] = extensions,
                        ["outcome"] = "Denied"
                    });

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

        IResult failureResult = result.FailureResult
            ?? Microsoft.AspNetCore.Http.Results.Problem(statusCode: StatusCodes.Status403Forbidden);

        await failureResult.ExecuteAsync(httpContext).ConfigureAwait(false);
    }
}
