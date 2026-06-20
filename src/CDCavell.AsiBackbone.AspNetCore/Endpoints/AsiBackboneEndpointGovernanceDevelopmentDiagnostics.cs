using CDCavell.AsiBackbone.Core.Decisions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CDCavell.AsiBackbone.AspNetCore.Endpoints;

internal static class AsiBackboneEndpointGovernanceDevelopmentDiagnostics
{
    private const string RedactedValue = "[redacted]";
    private const string DocumentationArticleName = "endpoint-governance-development-diagnostics.html";

    public static bool IsEnabled(
        HttpContext httpContext,
        AsiBackboneEndpointGovernanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);

        if (!options.EnableDevelopmentDiagnostics)
        {
            return false;
        }

        IWebHostEnvironment? environment = httpContext.RequestServices.GetService<IWebHostEnvironment>();

        return environment?.IsDevelopment() == true;
    }

    public static IResult CreateProblem(
        HttpContext httpContext,
        AsiBackboneEndpointGovernanceOptions options,
        AsiBackboneEndpointGovernanceDescriptor descriptor,
        GovernanceDecision? decision,
        string decisionStage,
        string title,
        string detail,
        int statusCode,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(decisionStage);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);

        Dictionary<string, string> diagnosticMetadata = metadata is null
            ? new Dictionary<string, string>(descriptor.ToMetadata(), StringComparer.Ordinal)
            : new Dictionary<string, string>(metadata, StringComparer.Ordinal);
        Dictionary<string, object?> extensions = CreateExtensions(
            options,
            descriptor,
            decision,
            decisionStage,
            diagnosticMetadata);

        return Microsoft.AspNetCore.Http.Results.Problem(
            title: title,
            detail: detail,
            statusCode: statusCode,
            extensions: extensions);
    }

    private static Dictionary<string, object?> CreateExtensions(
        AsiBackboneEndpointGovernanceOptions options,
        AsiBackboneEndpointGovernanceDescriptor descriptor,
        GovernanceDecision? decision,
        string decisionStage,
        IReadOnlyDictionary<string, string> metadata)
    {
        var extensions = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["decisionStage"] = decisionStage,
            ["endpointOperationName"] = descriptor.OperationName,
            ["endpointPolicyTypes"] = descriptor.PolicyTypes
                .Select(static policyType => policyType.FullName ?? policyType.Name)
                .OrderBy(static policyType => policyType, StringComparer.Ordinal)
                .ToArray(),
            ["capabilityScopes"] = descriptor.CapabilityScopes
                .OrderBy(static scope => scope, StringComparer.Ordinal)
                .ToArray(),
            ["metadataKeys"] = metadata.Keys
                .OrderBy(static key => key, StringComparer.Ordinal)
                .ToArray(),
            ["metadata"] = RedactMetadata(options, metadata)
        };

        if (decision is not null)
        {
            extensions["outcome"] = decision.Outcome.ToString();
            extensions["reasonCodes"] = decision.ReasonCodes.ToArray();
            extensions["reasonMessages"] = decision.Reasons
                .Select(static reason => reason.Message)
                .ToArray();

            AddIfPresent(extensions, "correlationId", decision.CorrelationId);
            AddIfPresent(extensions, "traceId", decision.TraceId);
            AddIfPresent(extensions, "policyVersion", decision.PolicyVersion);
            AddIfPresent(extensions, "policyHash", decision.PolicyHash);
        }

        string? documentationUrl = CreateDocumentationUrl(options.DevelopmentDiagnosticsDocumentationBaseUrl);
        AddIfPresent(extensions, "documentationUrl", documentationUrl);

        return extensions;
    }

    private static Dictionary<string, string> RedactMetadata(
        AsiBackboneEndpointGovernanceOptions options,
        IReadOnlyDictionary<string, string> metadata)
    {
        Dictionary<string, string> redacted = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> item in metadata.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
        {
            redacted[item.Key] = ShouldRedactMetadataValue(options, item.Key)
                ? RedactedValue
                : item.Value;
        }

        return redacted;
    }

    private static bool ShouldRedactMetadataValue(
        AsiBackboneEndpointGovernanceOptions options,
        string key)
    {
        if (!options.IncludeDevelopmentDiagnosticsMetadataValues)
        {
            return true;
        }

        foreach (string sensitiveKey in options.DevelopmentDiagnosticsRedactedMetadataKeys)
        {
            if (string.Equals(key, sensitiveKey, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return key.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || key.Contains("token", StringComparison.OrdinalIgnoreCase)
            || key.Contains("password", StringComparison.OrdinalIgnoreCase)
            || key.Contains("credential", StringComparison.OrdinalIgnoreCase)
            || key.Contains("cookie", StringComparison.OrdinalIgnoreCase)
            || key.Contains("authorization", StringComparison.OrdinalIgnoreCase)
            || key.Contains("key", StringComparison.OrdinalIgnoreCase);
    }

    private static string? CreateDocumentationUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        string trimmedBaseUrl = baseUrl.Trim();

        return trimmedBaseUrl.EndsWith('/')
            ? trimmedBaseUrl + DocumentationArticleName
            : trimmedBaseUrl + "/" + DocumentationArticleName;
    }

    private static void AddIfPresent(
        Dictionary<string, object?> extensions,
        string key,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            extensions[key] = value;
        }
    }
}
