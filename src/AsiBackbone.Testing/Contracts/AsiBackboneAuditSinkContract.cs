using AsiBackbone.Core.Audit;

namespace AsiBackbone.Testing.Contracts;

/// <summary>
/// Reusable contract fixture for <see cref="IAsiBackboneAuditSink" /> implementations.
/// </summary>
public abstract class AsiBackboneAuditSinkContract
{
    /// <summary>
    /// Creates the audit sink implementation under test.
    /// </summary>
    /// <returns>The audit sink implementation to validate.</returns>
    protected abstract IAsiBackboneAuditSink CreateAuditSink();

    /// <summary>
    /// Creates the audit residue supplied to the audit sink implementation under test.
    /// </summary>
    /// <returns>The audit residue to write.</returns>
    protected abstract IAsiBackboneAuditResidue CreateAuditResidue();

    /// <summary>
    /// Verifies that an audit sink accepts a valid audit residue value without weakening the residue shape.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the contract validation.</param>
    /// <returns>A task that completes when the contract validation succeeds.</returns>
    public async ValueTask VerifyAuditSinkAcceptsValidResidueAsync(CancellationToken cancellationToken = default)
    {
        IAsiBackboneAuditSink auditSink = CreateAuditSink()
            ?? throw new AsiBackboneContractViolationException("Audit sink contract must provide an audit sink instance.");
        IAsiBackboneAuditResidue residue = CreateAuditResidue()
            ?? throw new AsiBackboneContractViolationException("Audit sink contract must provide audit residue.");

        _ = AsiBackboneDecisionContract.VerifyAuditResidue(residue, "Audit sink residue");

        try
        {
            await auditSink.WriteAsync(residue, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (AsiBackboneContractViolationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new AsiBackboneContractViolationException(
                "Audit sink implementations must accept valid audit residue during normal contract validation or document a fail-closed/degraded behavior.",
                exception);
        }
    }
}
