using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Statements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankOfZ.Infrastructure.Persistence.Configurations;

public sealed class StatementSnapshotConfiguration : IEntityTypeConfiguration<StatementSnapshot>
{
    public void Configure(EntityTypeBuilder<StatementSnapshot> builder)
    {
        builder.ToTable(CatalogModelConstants.Tables.StatementSnapshots, table =>
            table.HasCheckConstraint(CatalogModelConstants.Constraints.StatementMonth, "[Month] BETWEEN 1 AND 12"));
        builder.HasKey(statement => statement.Id);
        builder.Property(statement => statement.AccountId).HasMaxLength(AccountRules.IdLength).IsUnicode(false);
        builder.Property(statement => statement.CustomerId).HasMaxLength(CustomerRules.IdLength).IsUnicode(false);
        builder.Property(statement => statement.DataVersion)
            .HasMaxLength(CatalogModelConstants.Lengths.StatementDataVersion).IsUnicode(false);
        builder.Property(statement => statement.CustomerName).HasMaxLength(CustomerRules.NameMaxLength * 2 + 16);
        builder.Property(statement => statement.CustomerAddress).HasMaxLength(CustomerRules.AddressMaxLength * 4);
        builder.Property(statement => statement.CustomerPhone).HasMaxLength(CustomerRules.PhoneMaxLength);
        builder.Property(statement => statement.SortCode).HasMaxLength(AccountRules.SortCodeLength).IsUnicode(false);
        builder.Property(statement => statement.AccountType)
            .HasMaxLength(CatalogModelConstants.Lengths.StatementAccountType).IsUnicode(false);
        builder.Property(statement => statement.Currency).HasMaxLength(CatalogModelConstants.Lengths.Currency).IsUnicode(false);
        builder.Property(statement => statement.InterestRate)
            .HasPrecision(CatalogModelConstants.Precision.Interest, CatalogModelConstants.Precision.InterestScale);
        builder.Property(statement => statement.OpeningBalance)
            .HasPrecision(CatalogModelConstants.Precision.StatementMoney, CatalogModelConstants.Precision.StatementMoneyScale);
        builder.Property(statement => statement.TotalCredits)
            .HasPrecision(CatalogModelConstants.Precision.StatementMoney, CatalogModelConstants.Precision.StatementMoneyScale);
        builder.Property(statement => statement.TotalDebits)
            .HasPrecision(CatalogModelConstants.Precision.StatementMoney, CatalogModelConstants.Precision.StatementMoneyScale);
        builder.Property(statement => statement.ClosingBalance)
            .HasPrecision(CatalogModelConstants.Precision.StatementMoney, CatalogModelConstants.Precision.StatementMoneyScale);
        builder.Property(statement => statement.AvailableBalance)
            .HasPrecision(CatalogModelConstants.Precision.StatementMoney, CatalogModelConstants.Precision.StatementMoneyScale);
        builder.HasMany(statement => statement.Transactions)
            .WithOne()
            .HasForeignKey(transaction => transaction.StatementId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(statement => new { statement.AccountId, statement.Year, statement.Month, statement.DataVersion })
            .IsUnique();
    }
}
