using BankOfZ.Infrastructure.DataInitialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankOfZ.Infrastructure.Persistence.Configurations;

public sealed class SetupOperationAuditConfiguration : IEntityTypeConfiguration<SetupOperationAudit>
{
    public void Configure(EntityTypeBuilder<SetupOperationAudit> builder)
    {
        builder.ToTable(CatalogModelConstants.Tables.SetupOperationAudits);
        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.Operation).HasMaxLength(CatalogModelConstants.Lengths.Action);
        builder.Property(audit => audit.Operator).HasMaxLength(CatalogModelConstants.Lengths.Actor);
        builder.Property(audit => audit.Environment).HasMaxLength(CatalogModelConstants.Lengths.ImportEnvironment);
        builder.Property(audit => audit.Result).HasMaxLength(CatalogModelConstants.Lengths.Result);
        builder.Property(audit => audit.MigrationVersion).HasMaxLength(CatalogModelConstants.Lengths.MigrationVersion);
    }
}
