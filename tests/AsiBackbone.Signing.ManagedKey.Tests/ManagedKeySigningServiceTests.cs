using System.Text;
using AsiBackbone.Core.Signing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AsiBackbone.Signing.ManagedKey.Tests;

public sealed class ManagedKeySigningServiceTests
{
    private static readonly DateTimeOffset SignedUtc = new(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SignAsyncReturnsProviderNeutralManagedKeyMetadata()
    {
        var client = new FakeManagedKeySigningClient();
        var service = new ManagedKeySigningService(CreateOptions(), client);
        var request = new SigningRequest(
            "abc123",
            hashAlgorithm: "SHA-256",
            purpose: CanonicalArtifactTypes.AuditLedgerRecord,
            keyId: "managed-key-1",
            keyVersion: "v1",
            metadata: new Dictionary<string, string>
            {
                ["artifact_type"] = CanonicalArtifactTypes.AuditLedgerRecord,
                ["artifact_id"] = "record-1"
            });

        SigningResult result = await service.SignAsync(request, TestContext.Current.CancellationToken);

        Assert.True(result.IsSigned);
        Assert.Equal("abc123", result.Metadata.SigningHash);
        Assert.Equal("SHA-256", result.Metadata.HashAlgorithm);
        Assert.NotNull(result.Metadata.Signature);
        Assert.Equal(ManagedKeySigningOptions.DefaultSignatureAlgorithm, result.Metadata.SignatureAlgorithm);
        Assert.Equal("managed-key-1", result.Metadata.KeyId);
        Assert.Equal("v1", result.Metadata.KeyVersion);
        Assert.Equal("managed-key-test", result.Metadata.Provider);
        Assert.Equal(SignedUtc, result.Metadata.SignedUtc);
        Assert.Equal("managed-key", result.Metadata.Metadata["provider_kind"]);
        Assert.Equal("true", result.Metadata.Metadata["remote_key_material"]);
        Assert.Equal("false", result.Metadata.Metadata["raw_private_key_loaded"]);
        Assert.Equal("signed", result.Metadata.Metadata["signing_status"]);
        Assert.Equal("operation-1", result.Metadata.Metadata["provider_operation_id"]);
        Assert.Equal(CanonicalArtifactTypes.AuditLedgerRecord, result.Metadata.Metadata["artifact_type"]);
        Assert.False(result.Metadata.Metadata.ContainsKey("access_token"));
    }

    [Fact]
    public async Task SignAsyncThrowsForUnsupportedHashAlgorithmByDefault()
    {
        var service = new ManagedKeySigningService(CreateOptions(), new FakeManagedKeySigningClient());
        var request = new SigningRequest(
            "abc123",
            hashAlgorithm: "SHA-512",
            purpose: CanonicalArtifactTypes.AuditLedgerRecord,
            keyId: "managed-key-1",
            keyVersion: "v1");

        ManagedKeySigningException exception = await Assert.ThrowsAsync<ManagedKeySigningException>(async () =>
            await service.SignAsync(request, TestContext.Current.CancellationToken));

        Assert.Equal("managedkey.signing.hash-algorithm-unsupported", exception.FailureCode);
    }

    [Fact]
    public async Task SignAsyncThrowsForProviderFailureByDefault()
    {
        var client = new FakeManagedKeySigningClient(retryableFailuresBeforeSuccess: 1);
        ManagedKeySigningOptions options = CreateOptions(maxRetryAttempts: 0);
        var service = new ManagedKeySigningService(options, client);
        var request = new SigningRequest(
            "abc123",
            hashAlgorithm: "SHA-256",
            purpose: CanonicalArtifactTypes.AuditLedgerRecord,
            keyId: "managed-key-1",
            keyVersion: "v1");

        ManagedKeySigningException exception = await Assert.ThrowsAsync<ManagedKeySigningException>(async () =>
            await service.SignAsync(request, TestContext.Current.CancellationToken));

        Assert.Equal("managedkey.signing.provider-unavailable", exception.FailureCode);
        Assert.Equal(1, client.CallCount);
    }

    [Fact]
    public async Task SignAsyncReturnsUnsignedFailureForUnsupportedHashAlgorithmInLocalValidationMode()
    {
        var service = new ManagedKeySigningService(CreateLocalValidationOptions(), new FakeManagedKeySigningClient());
        var request = new SigningRequest(
            "abc123",
            hashAlgorithm: "SHA-512",
            purpose: CanonicalArtifactTypes.AuditLedgerRecord,
            keyId: "managed-key-1",
            keyVersion: "v1");

        SigningResult result = await service.SignAsync(request, TestContext.Current.CancellationToken);

        Assert.False(result.IsSigned);
        Assert.Null(result.Metadata.Signature);
        Assert.Equal("failed", result.Metadata.Metadata["signing_status"]);
        Assert.Equal("managedkey.signing.hash-algorithm-unsupported", result.Metadata.Metadata["failure_code"]);
    }

    [Fact]
    public async Task SignAsyncReturnsUnsignedFailureForMissingKeyVersionWhenRequiredInLocalValidationMode()
    {
        ManagedKeySigningOptions options = CreateLocalValidationOptions(keyVersion: null, requireKeyVersion: true);
        var service = new ManagedKeySigningService(options, new FakeManagedKeySigningClient());
        var request = new SigningRequest(
            "abc123",
            hashAlgorithm: "SHA-256",
            purpose: CanonicalArtifactTypes.AuditLedgerRecord,
            keyId: "managed-key-1");

        SigningResult result = await service.SignAsync(request, TestContext.Current.CancellationToken);

        Assert.False(result.IsSigned);
        Assert.Equal("managedkey.signing.key-version-missing", result.Metadata.Metadata["failure_code"]);
    }

    [Fact]
    public async Task SignAsyncReturnsUnsignedFailureForProviderFailureInLocalValidationMode()
    {
        var client = new FakeManagedKeySigningClient(retryableFailuresBeforeSuccess: 1);
        ManagedKeySigningOptions options = CreateLocalValidationOptions(maxRetryAttempts: 0);
        var service = new ManagedKeySigningService(options, client);
        var request = new SigningRequest(
            "abc123",
            hashAlgorithm: "SHA-256",
            purpose: CanonicalArtifactTypes.AuditLedgerRecord,
            keyId: "managed-key-1",
            keyVersion: "v1");

        SigningResult result = await service.SignAsync(request, TestContext.Current.CancellationToken);

        Assert.False(result.IsSigned);
        Assert.Equal("failed", result.Metadata.Metadata["signing_status"]);
        Assert.Equal("managedkey.signing.provider-unavailable", result.Metadata.Metadata["failure_code"]);
        Assert.Equal("0", result.Metadata.Metadata["retry_attempts"]);
        Assert.Equal(1, client.CallCount);
    }

    [Fact]
    public async Task SignAsyncRetriesRetryableClientFailureThenSigns()
    {
        var client = new FakeManagedKeySigningClient(retryableFailuresBeforeSuccess: 1);
        ManagedKeySigningOptions options = CreateOptions(retryDelay: TimeSpan.Zero);
        var service = new ManagedKeySigningService(options, client);
        var request = new SigningRequest(
            "abc123",
            hashAlgorithm: "SHA-256",
            purpose: CanonicalArtifactTypes.AuditLedgerRecord,
            keyId: "managed-key-1",
            keyVersion: "v1");

        SigningResult result = await service.SignAsync(request, TestContext.Current.CancellationToken);

        Assert.True(result.IsSigned);
        Assert.Equal(2, client.CallCount);
        Assert.Equal("1", result.Metadata.Metadata["retry_attempts"]);
    }

    [Fact]
    public void ManagedKeySigningOptionsDefaultToFailClosed()
    {
        var options = new ManagedKeySigningOptions
        {
            KeyId = "managed-key-1",
            KeyVersion = "v1"
        };

        Assert.False(options.ReturnUnsignedOnFailure);
    }

    [Fact]
    public void AddAsiBackboneManagedKeySigningRegistersFailClosedSigningServiceByDefault()
    {
        ServiceCollection services = new();
        _ = services.AddAsiBackboneManagedKeySigning(
            options =>
            {
                options.ProviderName = "managed-key-test";
                options.KeyId = "managed-key-1";
                options.KeyVersion = "v1";
                options.RetryDelay = TimeSpan.Zero;
            },
            _ => new FakeManagedKeySigningClient());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetRequiredService<ManagedKeySigningService>());
        Assert.NotNull(serviceProvider.GetRequiredService<IAsiBackboneSigningService>());
        Assert.False(serviceProvider.GetRequiredService<ManagedKeySigningOptions>().ReturnUnsignedOnFailure);
    }

    [Fact]
    public void AddAsiBackboneManagedKeySigningForLocalValidationRegistersFailOpenSigningService()
    {
        ServiceCollection services = new();
        _ = services.AddAsiBackboneManagedKeySigningForLocalValidation(
            options =>
            {
                options.ProviderName = "managed-key-test";
                options.KeyId = "managed-key-1";
                options.KeyVersion = "v1";
                options.RetryDelay = TimeSpan.Zero;
            },
            _ => new FakeManagedKeySigningClient());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetRequiredService<ManagedKeySigningService>());
        Assert.NotNull(serviceProvider.GetRequiredService<IAsiBackboneSigningService>());
        Assert.True(serviceProvider.GetRequiredService<ManagedKeySigningOptions>().ReturnUnsignedOnFailure);
    }

    private static ManagedKeySigningOptions CreateOptions(
        string? keyVersion = "v1",
        bool requireKeyVersion = true,
        bool returnUnsignedOnFailure = false,
        int maxRetryAttempts = 2,
        TimeSpan? retryDelay = null)
    {
        return ManagedKeySigningOptions.Create(
            keyId: "managed-key-1",
            keyVersion: keyVersion,
            providerName: "managed-key-test",
            requireKeyVersion: requireKeyVersion,
            returnUnsignedOnFailure: returnUnsignedOnFailure,
            maxRetryAttempts: maxRetryAttempts,
            retryDelay: retryDelay ?? TimeSpan.Zero);
    }

    private static ManagedKeySigningOptions CreateLocalValidationOptions(
        string? keyVersion = "v1",
        bool requireKeyVersion = true,
        int maxRetryAttempts = 2,
        TimeSpan? retryDelay = null)
    {
        return ManagedKeySigningOptions.CreateLocalValidation(
            keyId: "managed-key-1",
            keyVersion: keyVersion,
            providerName: "managed-key-test",
            requireKeyVersion: requireKeyVersion,
            maxRetryAttempts: maxRetryAttempts,
            retryDelay: retryDelay ?? TimeSpan.Zero);
    }

    private sealed class FakeManagedKeySigningClient(int retryableFailuresBeforeSuccess = 0) : IManagedKeySigningClient
    {
        public int CallCount { get; private set; }

        public ValueTask<ManagedKeySignResult> SignAsync(
            ManagedKeySignRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;

            if (CallCount <= retryableFailuresBeforeSuccess)
            {
                throw new ManagedKeySigningException(
                    "managedkey.signing.provider-unavailable",
                    "The managed-key provider is temporarily unavailable.",
                    isRetryable: true);
            }

            string signature = Convert.ToBase64String(Encoding.UTF8.GetBytes($"managed-signature:{request.SigningHash}"));

            return ValueTask.FromResult(ManagedKeySignResult.Create(
                signature,
                request.SignatureAlgorithm,
                request.KeyId,
                request.KeyVersion,
                SignedUtc,
                providerOperationId: "operation-1",
                metadata: new Dictionary<string, string>
                {
                    ["provider_region"] = "test-region",
                    ["access_token"] = "should-not-be-preserved"
                }));
        }
    }
}
