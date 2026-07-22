using System.Data;
using BankOfZ.Application.Customers;
using BankOfZ.Domain.Customers;
using BankOfZ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankOfZ.Infrastructure.Customers;

public sealed class CustomerRepository(BankOfZIdentityContext context) : ICustomerRepository
{
    public Task<Customer?> FindAsync(string id, bool tracking, CancellationToken cancellationToken)
    {
        var query = tracking ? context.Customers : context.Customers.AsNoTracking();
        return query.SingleOrDefaultAsync(customer => customer.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> SearchAsync(
        string normalizedName,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        await context.Customers
            .AsNoTracking()
            .Where(customer => customer.Status == CustomerStatus.Active && customer.NormalizedName.Contains(normalizedName))
            .OrderBy(customer => customer.NormalizedName)
            .ThenBy(customer => customer.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

    public async Task<string> AllocateIdAsync(CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT NEXT VALUE FOR dbo.{CatalogModelConstants.Sequences.CustomerNumber}";
            var scalar = await command.ExecuteScalarAsync(cancellationToken);
            var value = Convert.ToInt64(scalar);
            return value.ToString($"D{CustomerRules.IdLength}");
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    public void Add(Customer customer) => context.Customers.Add(customer);

    public void SetExpectedVersion(Customer customer, byte[] version) =>
        context.Entry(customer).Property(entity => entity.Version).OriginalValue = version;

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new CustomerConflictException("Customer was changed by another request.", exception);
        }
    }
}
