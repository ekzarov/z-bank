using BankOfZ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankOfZ.Setup;

internal static class DemoResetService
{
    public static async Task ClearAsync(BankOfZIdentityContext database)
    {
        await using var transaction = await database.Database.BeginTransactionAsync();
        const string sql = """
            DELETE FROM [StatementTransactionSnapshots];
            DELETE FROM [StatementSnapshots];
            DELETE FROM [StatementAuditEntries];
            DELETE FROM [BookedTransactions];
            DELETE FROM [AccountAuditEntries];
            DELETE FROM [Accounts];

            DELETE FROM [AspNetUserTokens];
            DELETE FROM [AspNetUserLogins];
            DELETE FROM [AspNetUserClaims];
            DELETE FROM [AspNetUserRoles];
            DELETE FROM [AspNetRoleClaims];
            DELETE FROM [AspNetUsers];
            DELETE FROM [AspNetRoles];

            DELETE FROM [LegacyTransactionRuns];
            DELETE FROM [CustomerAuditEntries];
            DELETE FROM [Customers];
            DELETE FROM [ImportReferenceValues];
            DELETE FROM [ImportAttempts];
            DELETE FROM [ImportStagedRecords];
            DELETE FROM [ImportRuns];
            """;
        await database.Database.ExecuteSqlRawAsync(sql);
        await transaction.CommitAsync();
        database.ChangeTracker.Clear();
    }
}
