using AsiBackbone.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AsiBackbone.EntityFrameworkCore.Configurations;

/// <summary>
/// Configures the Entity Framework Core persistence mapping for <see cref="AsiBackboneAuditLedgerRecordEntity" />.
/// </summary>
public sealed class AsiBackboneAuditLedgerRecordEntityConfiguration
    : IEntityTypeConfiguration<AsiBackboneAuditLedgerRecordEntity>
{
    private const int IdentifierMaxLength = 128;
    private const int SchemaVersionMaxLength = 64;
    private const int DisplayNameMaxLength = 256;
    private const int OperationNameMaxLength = 256;
    private const int OutcomeMaxLength = 128;
    private const int ActorTypeMaxLength = 64;
    private const int CorrelationMaxLength = 128;
    private const int PolicyVersionMaxLength = 128;
    private const int PolicyScopeMaxLength = 256;
    private const int StatusMaxLength = 128;
    private const int ProviderMaxLength = 128;
    private const int StageMaxLength = 128;
    private const int HashMaxLength = 512;
    private const int SignatureKeyIdMaxLength = 128;
    private const int SignatureKeyVersionMaxLength = 128;
    private const int SignatureAlgorithmMaxLength = 128;
    private const int SignatureProviderMaxLength = 128;
    private const int ConcurrencyStampMaxLength = 64;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AsiBackboneAuditLedgerRecordEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.ToTable("AsiBackboneAuditLedgerRecords");

        _ = builder.HasKey(record => record.Id);

        _ = builder.Property(record => record.Id)
            .ValueGeneratedNever();

        _ = builder.Property(record => record.ConcurrencyStamp)
            .IsRequired()
            .HasMaxLength(ConcurrencyStampMaxLength)
            .IsConcurrencyToken();

        _ = builder.Property(record => record.RecordId)
            .IsRequired()
            .HasMaxLength(IdentifierMaxLength);

        _ = builder.Property(record => record.SchemaVersion)
            .IsRequired()
            .HasMaxLength(SchemaVersionMaxLength);

        _ = builder.Property(record => record.EventId)
            .IsRequired()
            .HasMaxLength(IdentifierMaxLength);

        _ = builder.Property(record => record.AuditResidueId)
            .HasMaxLength(IdentifierMaxLength);

        _ = builder.Property(record => record.OccurredUtc)
            .IsRequired();

        _ = builder.Property(record => record.RecordedUtc)
            .IsRequired();

        _ = builder.Property(record => record.ActorId)
            .IsRequired()
            .HasMaxLength(IdentifierMaxLength);

        _ = builder.Property(record => record.ActorType)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(ActorTypeMaxLength);

        _ = builder.Property(record => record.ActorDisplayName)
            .HasMaxLength(DisplayNameMaxLength);

        _ = builder.Property(record => record.OperationName)
            .IsRequired()
            .HasMaxLength(OperationNameMaxLength);

        _ = builder.Property(record => record.Outcome)
            .IsRequired()
            .HasMaxLength(OutcomeMaxLength);

        _ = builder.Property(record => record.ReasonCodesJson)
            .IsRequired();

        _ = builder.Property(record => record.CorrelationId)
            .HasMaxLength(CorrelationMaxLength);

        _ = builder.Property(record => record.TraceId)
            .HasMaxLength(CorrelationMaxLength);

        _ = builder.Property(record => record.SpanId)
            .HasMaxLength(CorrelationMaxLength);

        _ = builder.Property(record => record.ParentSpanId)
            .HasMaxLength(CorrelationMaxLength);

        _ = builder.Property(record => record.ConstraintSetHash)
            .HasMaxLength(HashMaxLength);

        _ = builder.Property(record => record.PolicyScope)
            .HasMaxLength(PolicyScopeMaxLength);

        _ = builder.Property(record => record.TenantHash)
            .HasMaxLength(HashMaxLength);

        _ = builder.Property(record => record.OrganizationHash)
            .HasMaxLength(HashMaxLength);

        _ = builder.Property(record => record.EmitterStatus)
            .HasMaxLength(StatusMaxLength);

        _ = builder.Property(record => record.EmitterProvider)
            .HasMaxLength(ProviderMaxLength);

        _ = builder.Property(record => record.GatewayExecutionId)
            .HasMaxLength(IdentifierMaxLength);

        _ = builder.Property(record => record.DecisionStage)
            .HasMaxLength(StageMaxLength);

        _ = builder.Property(record => record.PolicyVersion)
            .HasMaxLength(PolicyVersionMaxLength);

        _ = builder.Property(record => record.PolicyHash)
            .HasMaxLength(HashMaxLength);

        _ = builder.Property(record => record.HandshakeId)
            .HasMaxLength(IdentifierMaxLength);

        _ = builder.Property(record => record.AcknowledgmentId)
            .HasMaxLength(IdentifierMaxLength);

        _ = builder.Property(record => record.CapabilityTokenId)
            .HasMaxLength(IdentifierMaxLength);

        _ = builder.Property(record => record.PreviousRecordHash)
            .HasMaxLength(HashMaxLength);

        _ = builder.Property(record => record.RecordHash)
            .HasMaxLength(HashMaxLength);

        _ = builder.Property(record => record.SigningHash)
            .HasMaxLength(HashMaxLength);

        _ = builder.Property(record => record.SignatureKeyId)
            .HasMaxLength(SignatureKeyIdMaxLength);

        _ = builder.Property(record => record.SignatureKeyVersion)
            .HasMaxLength(SignatureKeyVersionMaxLength);

        _ = builder.Property(record => record.SignatureAlgorithm)
            .HasMaxLength(SignatureAlgorithmMaxLength);

        _ = builder.Property(record => record.SignatureValue);

        _ = builder.Property(record => record.SignatureProvider)
            .HasMaxLength(SignatureProviderMaxLength);

        _ = builder.Property(record => record.SignedUtc);

        _ = builder.Property(record => record.MetadataJson)
            .IsRequired();

        _ = builder.HasIndex(record => record.RecordId)
            .IsUnique();

        _ = builder.HasIndex(record => record.SchemaVersion);

        _ = builder.HasIndex(record => record.EventId);

        _ = builder.HasIndex(record => record.AuditResidueId);

        _ = builder.HasIndex(record => record.OccurredUtc);

        _ = builder.HasIndex(record => record.RecordedUtc);

        _ = builder.HasIndex(record => record.ActorId);

        _ = builder.HasIndex(record => record.ActorType);

        _ = builder.HasIndex(record => record.OperationName);

        _ = builder.HasIndex(record => record.Outcome);

        _ = builder.HasIndex(record => record.CorrelationId);

        _ = builder.HasIndex(record => record.TraceId);

        _ = builder.HasIndex(record => record.SpanId);

        _ = builder.HasIndex(record => record.PolicyVersion);

        _ = builder.HasIndex(record => record.PolicyHash);

        _ = builder.HasIndex(record => record.PolicyScope);

        _ = builder.HasIndex(record => record.TenantHash);

        _ = builder.HasIndex(record => record.OrganizationHash);

        _ = builder.HasIndex(record => record.EmitterStatus);

        _ = builder.HasIndex(record => record.EmitterProvider);

        _ = builder.HasIndex(record => record.OutboxSequence);

        _ = builder.HasIndex(record => record.GatewayExecutionId);

        _ = builder.HasIndex(record => record.DecisionStage);

        _ = builder.HasIndex(record => record.HandshakeId);

        _ = builder.HasIndex(record => record.AcknowledgmentId);

        _ = builder.HasIndex(record => record.CapabilityTokenId);

        _ = builder.HasIndex(record => record.RecordHash);

        _ = builder.HasIndex(record => record.SigningHash);

        _ = builder.HasIndex(record => record.SignatureKeyId);

        _ = builder.HasIndex(record => record.SignatureKeyVersion);

        _ = builder.HasIndex(record => record.SignatureProvider);

        _ = builder.HasIndex(record => record.SignedUtc);

        _ = builder.HasIndex(record => new
        {
            record.ActorId,
            record.RecordedUtc
        });

        _ = builder.HasIndex(record => new
        {
            record.CorrelationId,
            record.RecordedUtc
        });
    }
}
