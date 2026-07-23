using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankOfZ.Infrastructure.Persistence.Configurations;

public sealed class BookedTransactionConfiguration : IEntityTypeConfiguration<BookedTransaction>
{
    public void Configure(EntityTypeBuilder<BookedTransaction> builder)
    {
        builder.ToTable(CatalogModelConstants.Tables.BookedTransactions, table =>
        {
            table.HasCheckConstraint(CatalogModelConstants.Constraints.TransactionDirection, "[Direction] BETWEEN 0 AND 1");
            table.HasCheckConstraint(CatalogModelConstants.Constraints.TransactionAmount, "[Amount] > 0");
            table.HasCheckConstraint(CatalogModelConstants.Constraints.TransactionCurrency, "LEN([Currency]) = 3");
            table.HasCheckConstraint(CatalogModelConstants.Constraints.TransactionSourceSystem, "[SourceSystem] BETWEEN 0 AND 2");
        });
        builder.HasKey(transaction => transaction.Id);
        builder.Property(transaction => transaction.Reference).HasMaxLength(CashTransactionRules.ReferenceLength).IsUnicode(false);
        builder.Property(transaction => transaction.AccountId).HasMaxLength(AccountRules.IdLength).IsUnicode(false);
        builder.Property(transaction => transaction.CustomerId).HasMaxLength(CustomerRules.IdLength).IsUnicode(false);
        builder.Property(transaction => transaction.Amount)
            .HasPrecision(CatalogModelConstants.Precision.Money, CatalogModelConstants.Precision.MoneyScale);
        builder.Property(transaction => transaction.Currency).HasMaxLength(CatalogModelConstants.Lengths.Currency).IsUnicode(false);
        builder.Property(transaction => transaction.ResultingActualBalance)
            .HasPrecision(CatalogModelConstants.Precision.Money, CatalogModelConstants.Precision.MoneyScale);
        builder.Property(transaction => transaction.ResultingAvailableBalance)
            .HasPrecision(CatalogModelConstants.Precision.Money, CatalogModelConstants.Precision.MoneyScale);
        builder.Property(transaction => transaction.IdempotencyKey)
            .HasMaxLength(CashTransactionRules.IdempotencyKeyMaxLength).IsUnicode(false);
        builder.Property(transaction => transaction.RequestFingerprint)
            .HasMaxLength(CashTransactionRules.RequestFingerprintMaxLength).IsUnicode(false);
        builder.Property(transaction => transaction.TransferCorrelationId)
            .HasMaxLength(CashTransactionRules.TransferCorrelationIdLength).IsUnicode(false);
        builder.Property(transaction => transaction.SourceIdentifier)
            .HasMaxLength(CashTransactionRules.SourceIdentifierMaxLength);
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(transaction => transaction.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(transaction => transaction.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(transaction => transaction.Reference).IsUnique();
        builder.HasIndex(transaction => new { transaction.AccountId, transaction.IdempotencyKey }).IsUnique();
        builder.HasIndex(transaction => transaction.TransferCorrelationId);
        builder.HasIndex(transaction => new { transaction.AccountId, transaction.CreatedAt });
        builder.HasIndex(transaction => new { transaction.SourceSystem, transaction.SourceIdentifier })
            .IsUnique()
            .HasFilter(CatalogModelConstants.Filters.ImportedSourceIdentifier);
    }
}
