using BankOfZ.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankOfZ.Infrastructure.Persistence.Configurations;

public sealed class CustomerAuditRecordConfiguration : IEntityTypeConfiguration<CustomerAuditRecord>
{
    public void Configure(EntityTypeBuilder<CustomerAuditRecord> builder)
    {
        builder.ToTable(CatalogModelConstants.Tables.CustomerAuditEntries);
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Actor).HasMaxLength(CatalogModelConstants.Lengths.Actor);
        builder.Property(record => record.Action).HasMaxLength(CatalogModelConstants.Lengths.Action);
        builder.Property(record => record.CustomerId).HasMaxLength(CustomerRules.IdLength).IsUnicode(false);
        builder.Property(record => record.Result).HasMaxLength(CatalogModelConstants.Lengths.Result);
        builder.Property(record => record.CorrelationId).HasMaxLength(CatalogModelConstants.Lengths.CorrelationId);
        builder.HasIndex(record => record.CustomerId);
    }
}
