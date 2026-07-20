namespace AsiBackbone.Core.CapabilityTokens;

public sealed class CapabilityGrantValidationOptions
{
    private static readonly string[] EmptyScopes = [];

    private CapabilityGrantValidationOptions(
        string? issuer,
        string? audience,
        IReadOnlyList<string> scopes,
        DateTimeOffset? validationUtc,
        TimeSpan allowedClockSkew,
       