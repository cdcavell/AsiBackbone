namespace CDCavell.AsiBackbone.Core.Classification;

/// <summary>
/// Default provider-neutral resolver for DLP or classification failure behavior.
/// </summary>
public sealed class DefaultAsiBackboneDlpFailurePolicyResolver : IAsiBackboneDlpFailurePolicyResolver
{
    private readonly DlpFailurePolicyOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAsiBackboneDlpFailurePolicyResolver" /> class.
    /// </summary>
    /// <param name="options">Optional policy options. When omitted, conservative defaults are used.</param>
    public DefaultAsiBackboneDlpFailurePolicyResolver(DlpFailurePolicyOptions? options = null)
    {
        this.options = options ?? new DlpFailurePolicyOptions();
    }

    /// <inheritdoc />
    public ValueTask<DlpFailurePolicyResolution> ResolveAsync(
        DlpFailurePolicyContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        DlpFailureBehavior behavior = options.GetBehavior(context);

        return ValueTask.FromResult(
            DlpFailurePolicyResolution.Create(context, behavior));
    }
}
