using BankOfZ.Infrastructure.DataInitialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankOfZ.Infrastructure.Persistence.Configurations;

public sealed class ImportAttemptConfiguration : IEntityTypeConfiguration<ImportAttempt>
{
    public void Configure(EntityTypeBuilder<ImportAttempt> builder)
    {
        builder.ToTable(CatalogModelConstants.Tables.ImportAttempts);
        builder.HasKey(attempt => attempt.Id);
        builder.Property(attempt => attempt.Operator).HasMaxLength(CatalogModelConstants.Lengths.Actor);
        builder.Property(attempt => attempt.Environment).HasMaxLength(CatalogModelConstants.Lengths.ImportEnvironment);
        builder.Property(attempt => attempt.FailureCode).HasMaxLength(CatalogModelConstants.Lengths.ImportFailure);
        builder.Property(attempt => attempt.MigrationVersion).HasMaxLength(CatalogModelConstants.Lengths.MigrationVersion);
        builder.HasOne<ImportRun>()
            .WithMany()
            .HasForeignKey(attempt => attempt.ImportRunId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(attempt => new { attempt.ImportRunId, attempt.AttemptNumber });
    }
}
