using System.Collections.Frozen;

namespace AsiBackbone.Signing.ManagedKey;

internal static class ManagedKeyDiagnosticMetadataKeyClassifier
{
    private static readonly FrozenSet<string> ReservedKeys =
        new[]
        {
            "failure_code",
            "failure_exception_type",
            "failure_message",
            "failure_retryable",
            "last_retry_delay_milliseconds",
            "last_retry_failure_code",
            "last_retry_failure_exception_type",
            "max_retry_attempts",
            "max_retry_delay_milliseconds",
            "provider_attempts",
            "provider_kind",
            "provider_operation_id",
            "raw_private_key_loaded",
            "remote_key_material",
            "retry_attempts",
            "retry_backoff_strategy",
            "retry_delay_applied",
            "retry_delay_configured",
            "retry_delay_count",
            "retry_delay_milliseconds",
            "signature_algorithm",
            "signing_status",
            "total_retry_delay_milliseconds"
        }.ToFrozenSet(StringComparer.Ordinal);

    public static bool IsReserved(string? key)
    {
        return !string.IsNullOrWhiteSpace(key)
            && ReservedKeys.Contains(key.Trim());
    }
}
