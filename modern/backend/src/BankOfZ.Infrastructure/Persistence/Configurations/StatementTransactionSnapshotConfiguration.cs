using BankOfZ.Domain.Statements;
using BankOfZ.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankOfZ.Infrastructure.Persistence.Configurations;

public sealed class StatementTransactionSnapshotConfiguration
    : IEntityTypeConfiguration<StatementTransactionSnapshot>
{
    public void Configure(EntityTypeBuilder<StatementTransactionSnapshot> builder)
    {
        builder.ToTable(CatalogModelConstants.Tables.StatementTransactionSnapshots, table =>
            table.HasCheckConstraint(
                CatalogModelConstants.Constraints.StatementTransactionDirection,
                "[Direction] BETWEEN 0 AND 1"));
        builder.HasKey(transaction => transaction.Id);
        builder.Property(transaction => transaction.Reference)
            .HasMaxLength(CashTransactionRules.ReferenceLength).IsUnicode(false);
        builder.Property(transaction => transaction.Description)
            .HasMaxLength(CatalogModelConstants.Lengths.StatementDescription);
        builder.Property(transaction => transaction.Amount)
            .HasPrecision(CatalogModelConstants.Precision.StatementMoney, CatalogModelConstants.Precision.StatementMoneyScale);
        builder.HasIndex(transaction => new { transaction.StatementId, transaction.Sequence }).IsUnique();
    }
}
