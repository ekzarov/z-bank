using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankOfZ.Infrastructure.Persistence.Configurations;

public sealed class StatementAuditRecordConfiguration : IEntityTypeConfiguration<StatementAuditRecord>
{
    public void Configure(EntityTypeBuilder<StatementAuditRecord> builder)
    {
        builder.ToTable(CatalogModelConstants.Tables.StatementAuditEntries);
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.AccountId)
            .HasMaxLength(BankOfZ.Domain.Accounts.AccountRules.IdLength).IsUnicode(false);
        builder.Property(entry => entry.Actor).HasMaxLength(CatalogModelConstants.Lengths.Actor);
        builder.Property(entry => entry.Action).HasMaxLength(CatalogModelConstants.Lengths.Action);
        builder.Property(entry => entry.Result).HasMaxLength(CatalogModelConstants.Lengths.Result);
        builder.Property(entry => entry.Diagnostics).HasMaxLength(CatalogModelConstants.Lengths.StatementFailure);
        builder.HasIndex(entry => new { entry.AccountId, entry.OccurredAt });
    }
}
