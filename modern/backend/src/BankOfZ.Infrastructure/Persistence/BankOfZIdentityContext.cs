using BankOfZ.Domain.Accounts;
using BankOfZ.Domain.Customers;
using BankOfZ.Domain.Transactions;
using BankOfZ.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BankOfZ.Infrastructure.Persistence;

public sealed class BankOfZIdentityContext(DbContextOptions<BankOfZIdentityContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAuditRecord> CustomerAuditEntries => Set<CustomerAuditRecord>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountAuditRecord> AccountAuditEntries => Set<AccountAuditRecord>();
    public DbSet<BookedTransaction> BookedTransactions => Set<BookedTransaction>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .Property(user => user.CustomerId)
            .HasMaxLength(CustomerRules.IdLength)
            .IsUnicode(false);
        builder.Entity<ApplicationUser>()
            .HasOne(user => user.Customer)
            .WithMany()
            .HasForeignKey(user => user.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ApplyConfigurationsFromAssembly(typeof(BankOfZIdentityContext).Assembly);
        builder.HasSequence<long>(CatalogModelConstants.Sequences.CustomerNumber)
            .StartsAt(1000000002)
            .IncrementsBy(1);
        builder.HasSequence<long>(CatalogModelConstants.Sequences.AccountNumber)
            .StartsAt(10000001)
            .IncrementsBy(1);
    }
}
