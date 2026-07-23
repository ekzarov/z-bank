using BankOfZ.Infrastructure.DataInitialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankOfZ.Infrastructure.Persistence.Configurations;

public sealed class ImportRunConfiguration : IEntityTypeConfiguration<ImportRun>
{
    public void Configure(EntityTypeBuilder<ImportRun> builder)
    {
        builder.ToTable(CatalogModelConstants.Tables.ImportRuns);
        builder.HasKey(run => run.Id);
        builder.Property(run => run.PackageVersion).HasMaxLength(CatalogModelConstants.Lengths.ImportPackageVersion);
        builder.Property(run => run.InputFingerprint).HasMaxLength(CatalogModelConstants.Lengths.ImportFingerprint).IsUnicode(false);
        builder.Property(run => run.Environment).HasMaxLength(CatalogModelConstants.Lengths.ImportEnvironment);
        builder.Property(run => run.StagedManifest).HasColumnType("nvarchar(max)");
        builder.Property(run => run.MigrationVersion).HasMaxLength(CatalogModelConstants.Lengths.MigrationVersion);
        builder.Property(run => run.FailureCode).HasMaxLength(CatalogModelConstants.Lengths.ImportFailure);
        builder.HasIndex(run => run.InputFingerprint).IsUnique();
    }
}
