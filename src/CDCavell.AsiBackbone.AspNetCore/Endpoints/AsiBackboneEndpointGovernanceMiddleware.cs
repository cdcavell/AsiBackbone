using Microsoft.AspNetCore.Http;

namespace CDCavell.AsiBackbone.AspNetCore.Endpoints;

/// <summary>
/// Middleware that evaluates AsiBackbone endpoint governance metadata before endpoint execution.
/// </summary>
public sealed class AsiBackboneEndpointGovernanceMiddleware
{
    private readonly RequestDelegate next;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsiBackboneEndpointGovernanceMiddleware" /> class.
    /// </summary>
    /// <param name="next">The next request delegate.</param>
    public AsiBackboneEndpointGovernanceMiddleware(RequestDelegate next)
    {
        this.next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>
    /// Evaluates endpoint governance metadata and either continues execution or writes a failure result.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="governanceService">The endpoint governance service.</param>
    /// <returns>A task that completes when the middleware has run.</returns>
    public async Task InvokeAsync(
        HttpContext httpContext,
        IAsiBackboneEndpointGovernanceService governanceService)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(governanceService);

        Endpoint? endpoint = httpContext.GetEndpoint();
        AsiBackboneEndpointGovernanceDescriptor descriptor = AsiBackboneEndpointGovernanceDescriptor.FromEndpoint(endpoint);

        if (!descriptor.HasGovernanceMetadata)
        {
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
