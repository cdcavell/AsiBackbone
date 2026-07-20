using AsiBackbone.Core.Signing;

namespace AsiBackbone.Core.Integrity;

/// <summary>
/// Verifies provider-neutral append-only audit integrity chains.
/// </summary>
public static class AuditIntegrityVerifier
{
    /// <summary>
    /// Verifies that the supplied links form one continuous append-only chain in the supplied order