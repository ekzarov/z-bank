using BankOfZ.Infrastructure.AccessAdministration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankOfZ.Infrastructure.Persistence.Configurations;

public sealed class SecurityAuditRecordConfiguration : IEntityTypeConfiguration<SecurityAuditRecord>
{
    public void Configure(EntityTypeBuilder<SecurityAuditRecord> builder)
    {
        builder.ToTable(CatalogModelConstants.Tables.SecurityAuditEntries);
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.EventName).HasMaxLength(CatalogModelConstants.Lengths.Action);
        builder.Property(entry => entry.ActorId).HasMaxLength(CatalogModelConstants.Lengths.Actor);
        builder.Property(entry => entry.SubjectId).HasMaxLength(CatalogModelConstants.Lengths.Actor);
        builder.Property(entry => entry.Outcome).HasMaxLength(CatalogModelConstants.Lengths.Result);
        builder.Property(entry => entry.CorrelationId).HasMaxLength(CatalogModelConstants.Lengths.CorrelationId);
        builder.HasIndex(entry => new { entry.OccurredAt, entry.Id });
        builder.HasIndex(entry => entry.EventName);
        builder.HasIndex(entry => entry.ActorId);
        builder.HasIndex(entry => entry.SubjectId);
    }
}
