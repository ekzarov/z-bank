using BankOfZ.Infrastructure.DataInitialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankOfZ.Infrastructure.Persistence.Configurations;

public sealed class ImportReferenceValueConfiguration : IEntityTypeConfiguration<ImportReferenceValue>
{
    public void Configure(EntityTypeBuilder<ImportReferenceValue> builder)
    {
        builder.ToTable(CatalogModelConstants.Tables.ImportReferenceValues);
        builder.HasKey(value => new { value.Type, value.Code });
        builder.Property(value => value.Type).HasMaxLength(CatalogModelConstants.Lengths.ImportReferenceType);
        builder.Property(value => value.Code).HasMaxLength(CatalogModelConstants.Lengths.ImportReferenceCode);
        builder.Property(value => value.Description).HasMaxLength(CatalogModelConstants.Lengths.ImportReferenceDescription);
        builder.Property(value => value.SourceIdentifier).HasMaxLength(CatalogModelConstants.Lengths.ImportReferenceSource);
    }
}
