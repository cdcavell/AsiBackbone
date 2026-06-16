using CDCavell.AsiBackbone.Core.Signing;

namespace CDCavell.AsiBackbone.Signing.ManagedKey;

/// <summary>
/// Provides managed-key signing for AsiBackbone signing abstractions through a host-owned managed-key client.
/// </summary>
/// <remarks>
/// This service signs precomputed hashes only. It never requests or handles raw private key material.
/// </remarks>
public sealed class ManagedKeySigningService : IAsiBackboneSigningService
{
    private const string ProviderKind = "managed-key";

    private readonly ManagedKeySigningOptions options;
    private readonly IManagedKeySigningClient client;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedKeySigningService" /> class.
    /// </summary>
    public ManagedKeySigningService(ManagedKeySigningOptions options, IManagedKeySigningClient client)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(client);

        options.Validate();
        this.options = options;
        this.client = client;
    }

    /// <inheritdoc />
    public async ValueTask<SigningResult> SignAsync(
        SigningRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        string? validationFailure = ValidateSigningRequest(request);
        if (validationFailure is not null)
        {
            return CreateUnsignedFailureResult(request, validationFailure, validationFailure, retryAttempts: 0);
        }

        int attempt = 0;

        while (true)
        {
            try
            {
                ManagedKeySignResult managedResult = await client
                    .SignAsync(CreateManagedKeyRequest(request), cancellationToken)
                    .ConfigureAwait(false);

                return CreateSignedResult(request, managedResult, attempt);
            }
            catch (ManagedKeySigningException exception) when (exception.IsRetryable && attempt < options.MaxRetryAttempts)
            {
                attempt++;
                await DelayForRetryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (ManagedKeySigningException exception)
            {
                return HandleFailure(request, exception.FailureCode, exception.Message, attempt, exception);
            }
            catch (TimeoutException exception)
            {
                return HandleFailure(request, "managedkey.signing.provider-unavailable", exception.GetType().Name, attempt, exception);
            }
            catch (InvalidOperationException exception)
            {
                return HandleFailure(request, "managedkey.signing.failed", exception.GetType().Name, attempt, exception);
            }
            catch (NotSupportedException exception)
            {
                return HandleFailure(request, "managedkey.signing.unsupported", exception.GetType().Name, attempt, exception);
            }
        }
    }

    private SigningResult HandleFailure(
        SigningRequest request,
        string failureCode,
        string failureMessage,
        int retryAttempts,
        Exception exception)
    {
        if (!options.ReturnUnsignedOnFailure)
        {
            throw exception;
        }

        return CreateUnsignedFailureResult(request, failureCode, failureMessage, retryAttempts);
    }

    private ManagedKeySignRequest CreateManagedKeyRequest(SigningRequest request)
    {
        return new ManagedKeySignRequest(
            request.SigningHash,
            NormalizeHashAlgorithm(request.HashAlgorithm),
            NormalizeRequired(options.SignatureAlgorithm, ManagedKeySigningOptions.DefaultSignatureAlgorithm),
            ResolveKeyId(request),
            ResolveKeyVersion(request),
            request.Purpose,
            request.Metadata);
    }

    private SigningResult CreateSignedResult(
        SigningRequest request,
        ManagedKeySignResult managedResult,
        int retryAttempts)
    {
        string resolvedKeyVersion = managedResult.KeyVersion ?? ResolveKeyVersion(request) ?? string.Empty;
        Dictionary<string, string> metadata = CreateBaseMetadata(request, retryAttempts);
        metadata["signing_status"] = "signed";

        if (managedResult.ProviderOperationId is not null)
        {
            metadata["provider_operation_id"] = managedResult.ProviderOperationId;
        }

        foreach (KeyValuePair<string, string> item in managedResult.Metadata)
        {
            if (!IsSafeProviderMetadataKey(item.Key))
            {
                continue;
            }

            metadata[item.Key.Trim()] = item.Value?.Trim() ?? string.Empty;
        }

        var signingMetadata = SigningMetadata.Create(
            signingHash: request.SigningHash,
            hashAlgorithm: NormalizeHashAlgorithm(request.HashAlgorithm),
            signature: managedResult.Signature,
            signatureAlgorithm: managedResult.SignatureAlgorithm,
            keyId: managedResult.KeyId,
            keyVersion: string.IsNullOrWhiteSpace(resolvedKeyVersion) ? null : resolvedKeyVersion,
            provider: NormalizeRequired(options.ProviderName, ManagedKeySigningOptions.DefaultProviderName),
            signedUtc: managedResult.SignedUtc,
            metadata: metadata);

        return SigningResult.FromMetadata(signingMetadata);
    }

    private SigningResult CreateUnsignedFailureResult(
        SigningRequest request,
        string failureCode,
        string failureMessage,
        int retryAttempts)
    {
        Dictionary<string, string> metadata = CreateBaseMetadata(request, retryAttempts);
        metadata["signing_status"] = "failed";
        metadata["failure_code"] = failureCode;
        metadata["failure_message"] = failureMessage;

        var signingMetadata = SigningMetadata.Create(
            signingHash: request.SigningHash,
            hashAlgorithm: NormalizeHashAlgorithm(request.HashAlgorithm),
            keyId: ResolveKeyId(request),
            keyVersion: ResolveKeyVersion(request),
            provider: NormalizeRequired(options.ProviderName, ManagedKeySigningOptions.DefaultProviderName),
            metadata: metadata);

        return SigningResult.FromMetadata(signingMetadata);
    }

    private Dictionary<string, string> CreateBaseMetadata(SigningRequest request, int retryAttempts)
    {
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            ["provider_kind"] = ProviderKind,
            ["remote_key_material"] = "true",
            ["raw_private_key_loaded"] = "false",
            ["retry_attempts"] = retryAttempts.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["signature_algorithm"] = NormalizeRequired(options.SignatureAlgorithm, ManagedKeySigningOptions.DefaultSignatureAlgorithm)
        };

        if (request.Purpose is not null)
        {
            metadata["purpose"] = request.Purpose;
        }

        foreach (KeyValuePair<string, string> item in request.Metadata)
        {
            if (string.IsNullOrWhiteSpace(item.Key))
            {
                continue;
            }

            metadata[item.Key.Trim()] = item.Value?.Trim() ?? string.Empty;
        }

        return metadata;
    }

    private string? ValidateSigningRequest(SigningRequest request)
    {
        if (!IsSupportedHashAlgorithm(request.HashAlgorithm))
        {
            return "managedkey.signing.hash-algorithm-unsupported";
        }

        if (request.KeyId is not null && !string.Equals(request.KeyId, ResolveKeyId(request), StringComparison.Ordinal))
        {
            return "managedkey.signing.key-mismatch";
        }

        if (options.RequireKeyVersion && ResolveKeyVersion(request) is null)
        {
            return "managedkey.signing.key-version-missing";
        }

        return request.KeyVersion is not null && !string.Equals(request.KeyVersion, ResolveKeyVersion(request), StringComparison.Ordinal)
            ? "managedkey.signing.key-version-mismatch"
            : null;
    }

    private string ResolveKeyId(SigningRequest request)
    {
        return request.KeyId ?? NormalizeRequired(options.KeyId, string.Empty);
    }

    private string? ResolveKeyVersion(SigningRequest request)
    {
        return request.KeyVersion ?? NormalizeOptional(options.KeyVersion);
    }

    private string NormalizeHashAlgorithm(string? hashAlgorithm)
    {
        string normalized = string.IsNullOrWhiteSpace(hashAlgorithm)
            ? options.HashAlgorithm
            : hashAlgorithm.Trim();

        return normalized.Equals("SHA256", StringComparison.OrdinalIgnoreCase)
            ? ManagedKeySigningOptions.DefaultHashAlgorithm
            : normalized.Equals(ManagedKeySigningOptions.DefaultHashAlgorithm, StringComparison.OrdinalIgnoreCase)
                ? ManagedKeySigningOptions.DefaultHashAlgorithm
                : normalized;
    }

    private bool IsSupportedHashAlgorithm(string? hashAlgorithm)
    {
        return NormalizeHashAlgorithm(hashAlgorithm).Equals(
            NormalizeHashAlgorithm(options.HashAlgorithm),
            StringComparison.OrdinalIgnoreCase);
    }

    private async ValueTask DelayForRetryAsync(CancellationToken cancellationToken)
    {
        if (options.RetryDelay > TimeSpan.Zero)
        {
            await Task.Delay(options.RetryDelay, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsSafeProviderMetadataKey(string key)
    {
        return !string.IsNullOrWhiteSpace(key)
            && !key.Contains("secret", StringComparison.OrdinalIgnoreCase)
            && !key.Contains("token", StringComparison.OrdinalIgnoreCase)
            && !key.Contains("credential", StringComparison.OrdinalIgnoreCase)
            && !key.Contains("private", StringComparison.OrdinalIgnoreCase)
            && !key.Contains("connection", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRequired(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
