using CDCavell.ASIBackbone.Core.Audit;

namespace CDCavell.ASIBackbone.Storage.InMemory.Audit;

/// <summary>
/// In-memory audit ledger intended for tests, samples, and local validation hosts.
/// </summary>
/// <remarks>
/// This type is not durable storage. It records audit residue in process memory and is suitable only for local development,
/// tests, examples, and non-production validation flows.
/// </remarks>
public sealed class InMemoryAuditLedger : IAsiBackboneAuditSink
{
    private readonly Lock syncRoot = new();
    private readonly List<IAsiBackboneAuditResidue> records = [];

    /// <summary>
    /// Gets a snapshot of all recorded audit residue values.
    /// </summary>
    public IReadOnlyList<IAsiBackboneAuditResidue> Records
    {
        get
        {
            lock (syncRoot)
            {
                return Array.AsReadOnly([.. records]);
            }
        }
    }

    /// <inheritdoc />
    public ValueTask WriteAsync(
        IAsiBackboneAuditResidue residue,
        CancellationToken cancellationToken = default)
    {
        return WriteCore(residue, cancellationToken);
    }

    ValueTask IAsiBackboneAuditSink.WriteAsync(
        IAsiBackboneAuditResidue residue,
        CancellationToken cancellationToken)
    {
        return WriteCore(residue, cancellationToken);
    }

    /// <summary>
    /// Gets a snapshot of records matching the supplied correlation identifier.
    /// </summary>
    /// <param name="correlationId">The correlation identifier to match.</param>
    /// <returns>Audit residue values with the supplied correlation identifier.</returns>
    public IReadOnlyList<IAsiBackboneAuditResidue> GetByCorrelationId(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        string normalizedCorrelationId = correlationId.Trim();

        lock (syncRoot)
        {
            return Array.AsReadOnly([
                .. records.Where(record => string.Equals(
                    record.CorrelationId,
                    normalizedCorrelationId,
                    StringComparison.Ordinal))
            ]);
        }
    }

    /// <summary>
    /// Attempts to get a record by its audit event identifier.
    /// </summary>
    /// <param name="eventId">The audit event identifier.</param>
    /// <returns>The matching audit residue, or <see langword="null"/> when none is found.</returns>
    public IAsiBackboneAuditResidue? GetByEventId(string eventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);

        string normalizedEventId = eventId.Trim();

        lock (syncRoot)
        {
            return records.FirstOrDefault(record => string.Equals(
                record.EventId,
                normalizedEventId,
                StringComparison.Ordinal));
        }
    }

    private ValueTask WriteCore(
        IAsiBackboneAuditResidue residue,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(residue);
        cancellationToken.ThrowIfCancellationRequested();

        lock (syncRoot)
        {
            records.Add(residue);
        }

        return ValueTask.CompletedTask;
    }
}
