namespace AsiBackbone.Signing.LocalDevelopment;

/// <summary>
/// Configures the local-development signing provider.
/// </summary>
/// <remarks>
/// This provider is intended for local development, samples, and tests. It is not a production managed-key provider and does not create tamper-evidence by itself.
/// </remarks>
public sealed class LocalDevelopmentSigningOptions
