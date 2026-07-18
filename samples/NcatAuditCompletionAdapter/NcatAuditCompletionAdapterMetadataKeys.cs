namespace AsiBackbone.Samples.NcatAuditCompletionAdapter;

/// <summary>
/// Defines stable minimized metadata keys emitted by the optional NCAT audit-completion adapter.
/// </summary>
public static class NcatAuditCompletionAdapterMetadataKeys
{
    /// <summary>Gets the NCAT completion-outbox entry identifier used as the idempotency key.</summary>
    public const string CompletionEntryId = "ncatCompletionEntryId";

    /// <summary>Gets the source adapter label.</summary>
    public const string Adapter = "completionAdapter";
}
