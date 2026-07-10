using System.Text;
using AsiBackbone.Core.Signing;
using Xunit;

namespace AsiBackbone.Signing.ManagedKey.Tests;

/// <summary>
/// Unit tests for reserved managed-key diagnostic metadata classification and filtering.
/// </summary>
public sealed class ManagedKeyDiagnosticMetadataKeyClassifierTests
{
    private const string CallerSpoofValue = "caller-spoof";
    private const string ProviderSpoofValue = "provider-spoof";

    private static readonly DateTimeOffset SignedUtc = new(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);

    private static readonly string[] ReservedKeyValues =
    [
        "failure_code",
        "failure_exception_type",
        "failure_message",
        "failure_retryable",
        "last_retry_failure_code",
        "last_retry_failure_exception_type",
        "max_retry_attempts",
        "provider_attempts",
        "provider_kind",
        "provider_operation_id",
        "raw_private_key_loaded",
        "remote_key_material",
        "retry_attempts",
        "retry_delay_applied",
        "retry_delay_configured",
        "retry_delay_milliseconds",
        "signature_algorithm",
        "signing_status"
    ];

    /// <summary>
    /// Gets every currently reserved managed-key diagnostic metadata key.
    /// </summary>
    public static TheoryData<string> ReservedKeys
    {
        get
        {
            TheoryData<string> data = [.. ReservedKeyValues];

            return data;
        }
    }

    /// <summary>
    /// Verifies that every currently reserved diagnostic metadata key is classified as reserved.
    /// </summary>
    /// <param name="key">The diagnostic metadata key to classify.</param>
    [Theory]
    [MemberData(nameof(ReservedKeys))]
    public void IsReservedReturnsTrueForEveryReservedKey(string key)
    {
        Assert.True(ManagedKeyDiagnosticMetadataKeyClassifier.IsReserved(key));
    }

    /// <summary>
    /// Verifies that surrounding whitespace is removed before the ordinal reserved-key lookup.
    /// </summary>
    [Fact]
    public void IsReservedTrimsWhitespaceBeforeLookup()
    {
        Assert.True(ManagedKeyDiagnosticMetadataKeyClassifier.IsReserved(" \tprovider_kind\r\n "));
    }

    /// <summary>
    /// Verifies that null, empty, and whitespace-only keys are not reserved.
    /// </summary>
    /// <param name="key">The missing or blank key to classify.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void IsReservedReturnsFalseForNullEmptyOrWhitespace(string? key)
    {
        Assert.False(ManagedKeyDiagnosticMetadataKeyClassifier.IsReserved(key));
    }

    /// <summary>
    /// Verifies that classification remains ordinal and exact, without implicitly reserving prefixes or near-matches.
    /// </summary>
    /// <param name="key">The unreserved key to classify.</param>
    [Theory]
    [InlineData("Failure_code")]
    [InlineData("FAILURE_CODE")]
    [InlineData("failure")]
    [InlineData("failure_")]
    [InlineData("failure_code_suffix")]
    [InlineData("last_retry_failure_")]
    [InlineData("provider")]
    [InlineData("provider_")]
    [InlineData("provider_region")]
    [InlineData("retry")]
    [InlineData("retry_")]
    [InlineData("retry_attempts_extra")]
    [InlineData("custom_diagnostic")]
    [InlineData(" provider_region ")]
    public void IsReservedReturnsFalseForCaseVariantsPrefixesNearMatchesAndUnknownKeys(string key)
    {
        Assert.False(ManagedKeyDiagnosticMetadataKeyClassifier.IsReserved(key));
    }

    /// <summary>
    /// Verifies that caller metadata cannot spoof any reserved diagnostic key while unreserved metadata remains available.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SignAsyncRejectsAllReservedCallerMetadataAndPreservesUnreservedMetadata()
    {
        Dictionary<string, string> requestMetadata = ReservedKeyValues.ToDictionary(
            key => key,
            _ => CallerSpoofValue,
            StringComparer.Ordinal);
        requestMetadata[" retry_attempts "] = CallerSpoofValue;
        requestMetadata["provider_region"] = "caller-region";
        requestMetadata["retry_attempts_extra"] = "caller-extra";
        requestMetadata["Failure_code"] = "caller-case-variant";

        var service = new ManagedKeySigningService(
            CreateOptions(),
            new MetadataManagedKeySigningClient(new Dictionary<string, string>(StringComparer.Ordinal)));
        SigningRequest request = CreateRequest(requestMetadata);

        SigningResult result = await service.SignAsync(request, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(CallerSpoofValue, result.Metadata.Metadata.Values);
        Assert.Equal("caller-region", result.Metadata.Metadata["provider_region"]);
        Assert.Equal("caller-extra", result.Metadata.Metadata["retry_attempts_extra"]);
        Assert.Equal("caller-case-variant", result.Metadata.Metadata["Failure_code"]);
    }

    /// <summary>
    /// Verifies that provider metadata cannot spoof any reserved diagnostic key while safe provider-defined metadata remains available.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SignAsyncRejectsAllReservedProviderMetadataAndPreservesSafeProviderDefinedMetadata()
    {
        Dictionary<string, string> providerMetadata = ReservedKeyValues.ToDictionary(
            key => key,
            _ => ProviderSpoofValue,
            StringComparer.Ordinal);
        providerMetadata[" retry_attempts "] = ProviderSpoofValue;
        providerMetadata["provider_region"] = "provider-region";
        providerMetadata["retry_attempts_extra"] = "provider-extra";
        providerMetadata["Failure_code"] = "provider-case-variant";

        var service = new ManagedKeySigningService(
            CreateOptions(),
            new MetadataManagedKeySigningClient(providerMetadata));
        SigningRequest request = CreateRequest(new Dictionary<string, string>(StringComparer.Ordinal));

        SigningResult result = await service.SignAsync(request, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(ProviderSpoofValue, result.Metadata.Metadata.Values);
        Assert.Equal("provider-region", result.Metadata.Metadata["provider_region"]);
        Assert.Equal("provider-extra", result.Metadata.Metadata["retry_attempts_extra"]);
        Assert.Equal("provider-case-variant", result.Metadata.Metadata["Failure_code"]);
        Assert.Equal("operation-1", result.Metadata.Metadata["provider_operation_id"]);
    }

    private static ManagedKeySigningOptions CreateOptions()
    {
        return ManagedKeySigningOptions.Create(
            keyId: "managed-key-1",
            keyVersion: "v1",
            providerName: "managed-key-test",
            requireKeyVersion: true,
            returnUnsignedOnFailure: false,
            maxRetryAttempts: 2,
            retryDelay: TimeSpan.Zero);
    }

    private static SigningRequest CreateRequest(IReadOnlyDictionary<string, string> metadata)
    {
        return new SigningRequest(
            "abc123",
            hashAlgorithm: "SHA-256",
            purpose: CanonicalArtifactTypes.AuditLedgerRecord,
            keyId: "managed-key-1",
            keyVersion: "v1",
            metadata: metadata);
    }

    private sealed class MetadataManagedKeySigningClient(IReadOnlyDictionary<string, string> metadata)
        : IManagedKeySigningClient
    {
        public ValueTask<ManagedKeySignResult> SignAsync(
            ManagedKeySignRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            string signature = Convert.ToBase64String(Encoding.UTF8.GetBytes($"managed-signature:{request.SigningHash}"));
            return ValueTask.FromResult(ManagedKeySignResult.Create(
                signature,
                request.SignatureAlgorithm,
                request.KeyId,
                request.KeyVersion,
                SignedUtc,
                providerOperationId: "operation-1",
                metadata: metadata));
        }
    }
}
