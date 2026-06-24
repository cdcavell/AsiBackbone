namespace AsiBackbone.Core.Integrity;

/// <summary>
/// Describes provider-neutral audit integrity verification categories.
/// </summary>
public enum AuditIntegrityVerificationCategory
{
    /// <summary>
    /// The chain verified successfully.
    /// </summary>
    Valid = 0,

    /// <summary>
    /// No chain links were supplied.
    /// </summary>
    EmptyChain = 1,

    /// <summary>
    /// A required sequence number was missing.
    /// </summary>
    MissingRecord = 2,

    /// <summary>
    /// Links were supplied out of sequence.
    /// </summary>
    ReorderedRecord = 3,

    /// <summary>
    /// A link's persisted hash no longer matches its canonical fields.
    /// </summary>
    ModifiedRecord = 4,

    /// <summary>
    /// A link does not point to the previous link hash.
    /// </summary>
    HashMismatch = 5,

    /// <summary>
    /// Multiple links claim the same sequence or conflicting predecessor.
    /// </summary>
    ForkedChain = 6,

    /// <summary>
    /// A link belongs to a different chain than expected.
    /// </summary>
    WrongChain = 7,

    /// <summary>
    /// A link uses an unsupported hash algorithm.
    /// </summary>
    UnsupportedAlgorithm = 8,

    /// <summary>
    /// Verification failed but no more specific category could be assigned safely.
    /// </summary>
    Failed = 9
}
