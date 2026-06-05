using CDCavell.AsiBackbone.Core.Actors;
using CDCavell.AsiBackbone.Core.Audit;
using CDCavell.AsiBackbone.Core.Results;
using Xunit;

namespace CDCavell.AsiBackbone.Core.Tests.Audit;

/// <summary>
/// Unit tests for <see cref="IAsiBackboneAuditLedgerStore"/>. These tests verify the contract of the interface, but do not test any specific implementation. Each implementation should have its own set of unit tests to verify its behavior and correctness.
/// </summary>
public sealed class IAsiBackboneAuditLedgerStoreTests
{
    /// <summary>
    /// Verifies that an implementation of <see cref="IAsiBackboneAuditLedgerStore"/> allows appending a record and then looking it up by its record ID. This test uses a simple in-memory implementation of the interface to validate the expected behavior of the contract.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous test operation. The test will pass if the record can be successfully appended and retrieved by its record ID, and will fail if any of these operations do not behave as expected.
    /// </returns>
    [Fact]
    public async Task ContractAllowsAppendAndLookupByRecordId()
    {
        IAsiBackboneAuditLedgerStore store = new TestAuditLedgerStore();

        var residue = AuditResidue.Create(
            AsiBackboneActorContext.System,
            "system.sync",
            "Allowed",
            eventId: "event-123",
            correlationId: "correlation-123");

        var record = AuditLedgerRecord.FromResidue(
            residue,
            recordId: "record-123");

        OperationResult<AuditLedgerRecord> appendResult = await store.AppendAsync(record, TestContext.Current.CancellationToken);
        AuditLedgerRecord? found = await store.FindByRecordIdAsync("record-123", TestContext.Current.CancellationToken);

        Assert.True(appendResult.Succeeded);
        Assert.Same(record, appendResult.Value);
        Assert.Same(record, found);
    }

    /// <summary>
    /// Verifies that an implementation of <see cref="IAsiBackboneAuditLedgerStore"/> allows looking up records by their correlation ID. This test uses a simple in-memory implementation of the interface to validate the expected behavior of the contract, ensuring that multiple records with the same correlation ID can be retrieved correctly.
    /// </summary>
    private sealed class TestAuditLedgerStore : IAsiBackboneAuditLedgerStore
    {
        private readonly Dictionary<string, AuditLedgerRecord> records = new(StringComparer.Ordinal);

        public ValueTask<OperationResult<AuditLedgerRecord>> AppendAsync(
            AuditLedgerRecord record,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(record);

            records[record.RecordId] = record;

            return ValueTask.FromResult(OperationResult.Success(record));
        }

        public ValueTask<AuditLedgerRecord?> FindByRecordIdAsync(
            string recordId,
            CancellationToken cancellationToken = default)
        {
            _ = records.TryGetValue(recordId, out AuditLedgerRecord? record);

            return ValueTask.FromResult(record);
        }

        public ValueTask<IReadOnlyList<AuditLedgerRecord>> FindByCorrelationIdAsync(
            string correlationId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<AuditLedgerRecord> matches = records.Values
                .Where(record => string.Equals(record.CorrelationId, correlationId, StringComparison.Ordinal))
                .ToArray();

            return ValueTask.FromResult(matches);
        }
    }
}
