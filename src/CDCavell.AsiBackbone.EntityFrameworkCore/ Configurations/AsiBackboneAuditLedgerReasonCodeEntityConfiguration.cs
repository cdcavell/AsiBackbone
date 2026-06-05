using CDCavell.AsiBackbone.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CDCavell.AsiBackbone.EntityFrameworkCore.Configurations;

/// <summary>
/// Configures the Entity Framework Core persistence mapping for <see cref="AsiBackboneAuditLedgerReasonCodeEntity" />.
/// </summary>
public sealed class AsiBackboneAuditLedgerReasonCodeEntityConfiguration
    : IEntityTypeConfiguration<AsiBackboneAuditLedgerReasonCodeEntity>
{
    private const int ReasonCodeMaxLength = 256;
    private const int ConcurrencyStampMaxLength = 64;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AsiBackboneAuditLedgerReasonCodeEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = builder.ToTable("AsiBackboneAuditLedgerReasonCodes");

        _ = builder.HasKey(reasonCode => reasonCode.Id);

        _ = builder.Property(reasonCode => reasonCode.Id)
            .ValueGeneratedNever();

        _ = builder.Property(reasonCode => reasonCode.ConcurrencyStamp)
            .IsRequired()
            .HasMaxLength(ConcurrencyStampMaxLength)
            .IsConcurrencyToken();

        _ = builder.Property(reasonCode => reasonCode.AuditLedgerRecordId)
            .IsRequired();

        _ = builder.Property(reasonCode => reasonCode.Sequence)
            .IsRequired();

        _ = builder.Property(reasonCode => reasonCode.ReasonCode)
            .IsRequired()
            .HasMaxLength(ReasonCodeMaxLength);

        _ = builder.HasOne(reasonCode => reasonCode.AuditLedgerRecord)
            .WithMany()
            .HasForeignKey(reasonCode => reasonCode.AuditLedgerRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasIndex(reasonCode => reasonCode.AuditLedgerRecordId);

        _ = builder.HasIndex(reasonCode => reasonCode.ReasonCode);

        _ = builder.HasIndex(reasonCode => new
        {
            reasonCode.AuditLedgerRecordId,
            reasonCode.Sequence
        })
        .IsUnique();

        _ = builder.HasIndex(reasonCode => new
        {
            reasonCode.AuditLedgerRecordId,
            reasonCode.ReasonCode
        });
    }
}
