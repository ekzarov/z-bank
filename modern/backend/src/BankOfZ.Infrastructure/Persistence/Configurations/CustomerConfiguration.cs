using BankOfZ.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankOfZ.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable(CatalogModelConstants.Tables.Customers);
        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.Id).HasMaxLength(CustomerRules.IdLength).IsUnicode(false);
        builder.Property(customer => customer.SortCode).HasMaxLength(CustomerRules.SortCodeLength).IsUnicode(false);
        builder.Property(customer => customer.Title).HasMaxLength(CustomerRules.TitleMaxLength);
        builder.Property(customer => customer.FirstName).HasMaxLength(CustomerRules.NameMaxLength);
        builder.Property(customer => customer.LastName).HasMaxLength(CustomerRules.NameMaxLength);
        builder.Property(customer => customer.NormalizedName).HasMaxLength(CustomerRules.NameMaxLength * 2 + 1);
        builder.Property(customer => customer.AddressLine1).HasMaxLength(CustomerRules.AddressMaxLength);
        builder.Property(customer => customer.AddressLine2).HasMaxLength(CustomerRules.AddressMaxLength);
        builder.Property(customer => customer.City).HasMaxLength(CustomerRules.CityMaxLength);
        builder.Property(customer => customer.Region).HasMaxLength(CustomerRules.RegionMaxLength);
        builder.Property(customer => customer.PostalCode).HasMaxLength(CustomerRules.PostalCodeMaxLength);
        builder.Property(customer => customer.CountryCode).HasMaxLength(CustomerRules.CountryCodeLength).IsUnicode(false);
        builder.Property(customer => customer.Email).HasMaxLength(CustomerRules.EmailMaxLength);
        builder.Property(customer => customer.Phone).HasMaxLength(CustomerRules.PhoneMaxLength);
        builder.Property(customer => customer.SourceIdentifier).HasMaxLength(CustomerRules.SourceIdentifierMaxLength);
        builder.Property(customer => customer.CreditScore).HasPrecision(5, 2);
        builder.Property(customer => customer.Version).IsRowVersion();
        builder.HasIndex(customer => customer.NormalizedName)
            .HasDatabaseName("IX_Customers_NormalizedName_Active")
            .HasFilter("[Status] = 0");
        builder.HasIndex(customer => new { customer.SourceSystem, customer.SourceIdentifier })
            .IsUnique()
            .HasFilter(CatalogModelConstants.Filters.ImportedSourceIdentifier);
    }
}
