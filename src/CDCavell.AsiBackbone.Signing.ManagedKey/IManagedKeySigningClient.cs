namespace CDCavell.AsiBackbone.Signing.ManagedKey;

/// <summary>
/// Defines the host-owned managed-key signing boundary used by the AsiBackbone managed-key signing provider.
/// </summary>
/// <remarks>
/// Implementations should call a managed key system, HSM, cloud KMS, or equivalent key-management service without
/// exposing raw private key material to AsiBackbone Core or to this provider package.
/// </remarks>
public interface IManagedKeySigningClient
{
    /// <summary>
    /// Signs a precomputed governance artifact hash through a managed-key boundary.
    /// </summary>
    /// <param name="request">The managed-key signing request.</param>
    /// <param name="cancellationToken">A token used to observe cancellation.</param>
    /// <returns>The managed-key signing result.</returns>
    ValueTask<ManagedKeySignResult> SignAsync(
        ManagedKeySignRequest request,
        CancellationToken cancellationToken = default);
}
