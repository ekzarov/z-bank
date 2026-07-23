using BankOfZ.Infrastructure.DataInitialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankOfZ.Infrastructure.Persistence.Configurations;

public sealed class ImportStagedRecordConfiguration : IEntityTypeConfiguration<ImportStagedRecord>
{
    public void Configure(EntityTypeBuilder<ImportStagedRecord> builder)
    {
        builder.ToTable(CatalogModelConstants.Tables.ImportStagedRecords);
        builder.HasKey(record => new { record.ImportRunId, record.RecordType, record.SourceKey });
        builder.Property(record => record.RecordType).HasMaxLength(CatalogModelConstants.Lengths.ImportRecordType);
        builder.Property(record => record.SourceKey).HasMaxLength(CatalogModelConstants.Lengths.ImportSourceKey);
        builder.Property(record => record.ContentHash).HasMaxLength(CatalogModelConstants.Lengths.ImportFingerprint).IsUnicode(false);
        builder.HasOne<ImportRun>()
            .WithMany()
            .HasForeignKey(record => record.ImportRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
