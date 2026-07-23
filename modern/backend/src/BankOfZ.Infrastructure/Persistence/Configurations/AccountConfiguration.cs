using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankOfZ.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable(CatalogModelConstants.Tables.Accounts, table =>
        {
            table.HasCheckConstraint(CatalogModelConstants.Constraints.AccountType, "[Type] BETWEEN 0 AND 4");
            table.HasCheckConstraint(CatalogModelConstants.Constraints.AccountStatus, "[Status] BETWEEN 0 AND 1");
            table.HasCheckConstraint(CatalogModelConstants.Constraints.AccountInterestRate, "[InterestRate] >= 0 AND [InterestRate] <= 9999.99");
            table.HasCheckConstraint(CatalogModelConstants.Constraints.AccountOverdraftLimit, "[OverdraftLimit] >= 0");
            table.HasCheckConstraint(CatalogModelConstants.Constraints.AccountCurrency, "LEN([Currency]) = 3");
        });
        builder.HasKey(account => account.Id);
        builder.Property(account => account.Id).HasMaxLength(AccountRules.IdLength).IsUnicode(false);
        builder.Property(account => account.CustomerId).HasMaxLength(CustomerRules.IdLength).IsUnicode(false);
        builder.Property(account => account.SortCode).HasMaxLength(AccountRules.SortCodeLength).IsUnicode(false);
        builder.Property(account => account.Currency).HasMaxLength(CatalogModelConstants.Lengths.Currency).IsUnicode(false);
        builder.Property(account => account.InterestRate)
            .HasPrecision(CatalogModelConstants.Precision.Interest, CatalogModelConstants.Precision.InterestScale);
        builder.Property(account => account.ActualBalance)
            .HasPrecision(CatalogModelConstants.Precision.Money, CatalogModelConstants.Precision.MoneyScale);
        builder.Property(account => account.AvailableBalance)
            .HasPrecision(CatalogModelConstants.Precision.Money, CatalogModelConstants.Precision.MoneyScale);
        builder.Property(account => account.RawSourceType).HasMaxLength(AccountRules.RawSourceValueMaxLength);
        builder.Property(account => account.SourceIdentifier).HasMaxLength(AccountRules.SourceIdentifierMaxLength);
        builder.Property(account => account.LastTransactionReference)
            .HasMaxLength(CashTransactionRules.ReferenceLength)
            .IsUnicode(false);
        builder.Property(account => account.Version).IsRowVersion();
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(account => account.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(account => new { account.CustomerId, account.Status, account.Id });
        builder.HasIndex(account => new { account.SourceSystem, account.SourceIdentifier })
            .IsUnique()
            .HasFilter(CatalogModelConstants.Filters.ImportedSourceIdentifier);
    }
}
