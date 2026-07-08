using System.Globalization;
using System.Runtime.ExceptionServices;
using AsiBackbone.Core.Signing;

namespace AsiBackbone.Signing.ManagedKey;

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

        ManagedKeySigningAttemptDiagnostics diagnostics = CreateAttemptDiagnostics();
        string? validationFailure = ValidateSigningRequest(request);
        if (validationFailure is not null)
        {
            return HandleValidationFailure(request, validationFailure, diagnostics);
        }

        int attempt = 0;

        while (true)
        {
            try
            {
                ManagedKeySignResult managedResult = await client
                    .SignAsync(CreateManagedKeyRequest(request), cancellationToken)
                    .ConfigureAwait(false);

                return CreateSignedResult(request, managedResult, attempt, diagnostics);
            }
            catch (ManagedKeySigningException exception) when (exception.IsRetryable && attempt < options.MaxRetryAttempts)
            {
                diagnostics.RecordRetry(exception);
                attempt++;
                await DelayForRetryAsync(diagnostics, cancellationToken).ConfigureAwait(false);
            }
            catch (ManagedKeySigningException exception)
            {
                return HandleFailure(request, exception.FailureCode, exception.Message, attempt, diagnostics, exception);
            }
            catch (TimeoutException exception)
            {
                return HandleFailure(request, "managedkey.signing.provider-unavailable", exception.GetType().Name, attempt, diagnostics, exception);
            }
            catch (InvalidOperationException exception)
            {
                return HandleFailure(request, "managedkey.signing.failed", exception.GetType().Name, attempt, diagnostics, exception);
            }
            catch (NotSupportedException exception)
            {
                return HandleFailure(request, "managedkey.signing.unsupported", exception.GetType().Name, attempt, diagnostics, exception);
            }
        }
    }

    private SigningResult HandleValidationFailure(
        SigningRequest request,
        string failureCode,
        ManagedKeySigningAttemptDiagnostics diagnostics)
    {
        diagnostics.RecordValidationFailure();

        return !options.ReturnUnsignedOnFailure
            ? throw new ManagedKeySigningException(failureCode, failureCode)
            : CreateUnsignedFailureResult(request, failureCode, failureCode, retryAttempts: 0, diagnostics);
    }

    private SigningResult HandleFailure(
        SigningRequest request,
        string failureCode,
        string failureMessage,
        int retryAttempts,
        ManagedKeySigningAttemptDiagnostics diagnostics,
        Exception exception)
    {
        diagnostics.RecordFailure(exception);

        if (!options.ReturnUnsignedOnFailure)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        return CreateUnsignedFailureResult(request, failureCode, failureMessage, retryAttempts, diagnostics);
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
        int retryAttempts,
        ManagedKeySigningAttemptDiagnostics diagnostics)
    {
        string resolvedKeyVersion = managedResult.KeyVersion ?? ResolveKeyVersion(request) ?? string.Empty;
        Dictionary<string, string> metadata = CreateBaseMetadata(request, retryAttempts, providerAttempts: retryAttempts + 1, diagnostics);
        metadata["signing_status"] = "signed";

        if (managedResult.ProviderOperationId is not null)
        {
            metadata["provider_operation_id"] = managedResult.ProviderOperationId;
        }

        foreach (KeyValuePair<string, string> item in managedResult.Metadata)
        {
            if (!IsSafeProviderMetadataKey(item.Key) || IsReservedDiagnosticMetadataKey(item.Key))
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
        int retryAttempts,
        ManagedKeySigningAttemptDiagnostics diagnostics)
    {
        int providerAttempts = diagnostics.HasValidationFailure ? 0 : retryAttempts + 1;
        Dictionary<string, string> metadata = CreateBaseMetadata(request, retryAttempts, providerAttempts, diagnostics);
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

    private Dictionary<string, string> CreateBaseMetadata(
        SigningRequest request,
        int retryAttempts,
        int providerAttempts,
        ManagedKeySigningAttemptDiagnostics diagnostics)
    {
        Dictionary<string, string> metadata = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> item in request.Metadata)
        {
            if (string.IsNullOrWhiteSpace(item.Key) || IsReservedDiagnosticMetadataKey(item.Key))
            {
                continue;
            }

            metadata[item.Key.Trim()] = item.Value?.Trim() ?? string.Empty;
        }

        metadata["provider_kind"] = ProviderKind;
        metadata["remote_key_material"] = "true";
        metadata["raw_private_key_loaded"] = "false";
        metadata["retry_attempts"] = retryAttempts.ToString(CultureInfo.InvariantCulture);
        metadata["provider_attempts"] = providerAttempts.ToString(CultureInfo.InvariantCulture);
        metadata["max_retry_attempts"] = diagnostics.MaxRetryAttempts.ToString(CultureInfo.InvariantCulture);
        metadata["retry_delay_milliseconds"] = diagnostics.RetryDelay.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture);
        metadata["retry_delay_configured"] = ToMetadataBoolean(diagnostics.RetryDelay > TimeSpan.Zero);
        metadata["retry_delay_applied"] = ToMetadataBoolean(diagnostics.RetryDelayApplied);
        metadata["signature_algorithm"] = NormalizeRequired(options.SignatureAlgorithm, ManagedKeySigningOptions.DefaultSignatureAlgorithm);

        if (diagnostics.LastRetryFailureCode is not null)
        {
            metadata["last_retry_failure_code"] = diagnostics.LastRetryFailureCode;
        }

        if (diagnostics.LastRetryFailureExceptionType is not null)
        {
            metadata["last_retry_failure_exception_type"] = diagnostics.LastRetryFailureExceptionType;
        }

        if (diagnostics.FailureExceptionType is not null)
        {
            metadata["failure_exception_type"] = diagnostics.FailureExceptionType;
        }

        if (diagnostics.FailureRetryable is not null)
        {
            metadata["failure_retryable"] = ToMetadataBoolean(diagnostics.FailureRetryable.Value);
        }

        if (request.Purpose is not null)
        {
            metadata["purpose"] = request.Purpose;
        }

        return metadata;
    }

    private string? ValidateSigningRequest(SigningRequest request)
    {
        if (!IsSupportedHashAlgorithm(request.HashAlgorithm))
        {
            return "managedkey.signing.hash-algorithm-unsupported";
        }

        string configuredKeyId = NormalizeRequired(options.KeyId, string.Empty);
        if (request.KeyId is not null && !string.Equals(request.KeyId, configuredKeyId, StringComparison.Ordinal))
        {
            return "managedkey.signing.key-mismatch";
        }

        string? configuredKeyVersion = NormalizeOptional(options.KeyVersion);
        return options.RequireKeyVersion && request.KeyVersion is null && configuredKeyVersion is null
            ? "managedkey.signing.key-version-missing"
            : request.KeyVersion is not null
            && configuredKeyVersion is not null
            && !string.Equals(request.KeyVersion, configuredKeyVersion, StringComparison.Ordinal)
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

    private async ValueTask DelayForRetryAsync(
        ManagedKeySigningAttemptDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        if (options.RetryDelay > TimeSpan.Zero)
        {
            diagnostics.RecordRetryDelayApplied();
            await Task.Delay(options.RetryDelay, cancellationToken).ConfigureAwait(false);
        }
    }

    private ManagedKeySigningAttemptDiagnostics CreateAttemptDiagnostics()
    {
        return new ManagedKeySigningAttemptDiagnostics(options.MaxRetryAttempts, options.RetryDelay);
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

    private static bool IsReservedDiagnosticMetadataKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return key.Trim() switch
        {
            "failure_code" => true,
            "failure_exception_type" => true,
            "failure_message" => true,
            "failure_retryable" => true,
            "last_retry_failure_code" => true,
            "last_retry_failure_exception_type" => true,
            "max_retry_attempts" => true,
            "provider_attempts" => true,
            "provider_kind" => true,
            "provider_operation_id" => true,
            "raw_private_key_loaded" => true,
            "remote_key_material" => true,
            "retry_attempts" => true,
            "retry_delay_applied" => true,
            "retry_delay_configured" => true,
            "retry_delay_milliseconds" => true,
            "signature_algorithm" => true,
            "signing_status" => true,
            _ => false
        };
    }

    private static string NormalizeRequired(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string ToMetadataBoolean(bool value)
    {
        return value ? "true" : "false";
    }

    private sealed class ManagedKeySigningAttemptDiagnostics(int maxRetryAttempts, TimeSpan retryDelay)
    {
        public int MaxRetryAttempts { get; } = maxRetryAttempts;

        public TimeSpan RetryDelay { get; } = retryDelay;

        public bool RetryDelayApplied { get; private set; }

        public bool HasValidationFailure { get; private set; }

        public string? LastRetryFailureCode { get; private set; }

        public string? LastRetryFailureExceptionType { get; private set; }

        public string? FailureExceptionType { get; private set; }

        public bool? FailureRetryable { get; private set; }

        public void RecordValidationFailure()
        {
            HasValidationFailure = true;
        }

        public void RecordRetry(ManagedKeySigningException exception)
        {
            LastRetryFailureCode = exception.FailureCode;
            LastRetryFailureExceptionType = exception.GetType().Name;
        }

        public void RecordRetryDelayApplied()
        {
            RetryDelayApplied = true;
        }

        public void RecordFailure(Exception exception)
        {
            FailureExceptionType = exception.GetType().Name;
            FailureRetryable = exception is ManagedKeySigningException managedKeyException && managedKeyException.IsRetryable;
        }
    }
}
