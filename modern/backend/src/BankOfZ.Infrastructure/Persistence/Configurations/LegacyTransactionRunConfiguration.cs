using BankOfZ.Domain.Customers;
using BankOfZ.Infrastructure.DataInitialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankOfZ.Infrastructure.Persistence.Configurations;

public sealed class LegacyTransactionRunConfiguration : IEntityTypeConfiguration<LegacyTransactionRun>
{
    public void Configure(EntityTypeBuilder<LegacyTransactionRun> builder)
    {
        builder.ToTable(CatalogModelConstants.Tables.LegacyTransactionRuns);
        builder.HasKey(run => run.SourceIdentifier);
        builder.Property(run => run.SourceIdentifier).HasMaxLength(CatalogModelConstants.Lengths.LegacyRunIdentifier);
        builder.Property(run => run.Status).HasMaxLength(CatalogModelConstants.Lengths.LegacyRunStatus);
        builder.Property(run => run.CustomerId).HasMaxLength(CustomerRules.IdLength).IsUnicode(false);
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(run => run.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
