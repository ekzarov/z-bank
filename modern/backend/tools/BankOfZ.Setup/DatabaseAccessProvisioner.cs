using Microsoft.Data.SqlClient;

namespace BankOfZ.Setup;

internal static class DatabaseAccessProvisioner
{
    public static async Task ProvisionAsync(string adminConnectionString)
    {
        var apiPassword = RequiredSecret("BANKOFZ_API_DB_PASSWORD");
        var operatorPassword = RequiredSecret("BANKOFZ_OPERATOR_DB_PASSWORD");
        var builder = new SqlConnectionStringBuilder(adminConnectionString)
        {
            InitialCatalog = "master"
        };

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        const string serverSql = """
            DECLARE @statement nvarchar(max);
            IF DB_ID(N'BankOfZ') IS NULL CREATE DATABASE [BankOfZ];

            IF SUSER_ID(N'bankofz_api') IS NULL
                SET @statement = N'CREATE LOGIN [bankofz_api] WITH PASSWORD = ' + QUOTENAME(@apiPassword, '''') + N', CHECK_POLICY = ON';
            ELSE
                SET @statement = N'ALTER LOGIN [bankofz_api] WITH PASSWORD = ' + QUOTENAME(@apiPassword, '''');
            EXEC sys.sp_executesql @statement;

            IF SUSER_ID(N'bankofz_operator') IS NULL
                SET @statement = N'CREATE LOGIN [bankofz_operator] WITH PASSWORD = ' + QUOTENAME(@operatorPassword, '''') + N', CHECK_POLICY = ON';
            ELSE
                SET @statement = N'ALTER LOGIN [bankofz_operator] WITH PASSWORD = ' + QUOTENAME(@operatorPassword, '''');
            EXEC sys.sp_executesql @statement;
            """;
        await using (var command = new SqlCommand(serverSql, connection))
        {
            command.Parameters.AddWithValue("@apiPassword", apiPassword);
            command.Parameters.AddWithValue("@operatorPassword", operatorPassword);
            await command.ExecuteNonQueryAsync();
        }

        builder.InitialCatalog = "BankOfZ";
        await using var databaseConnection = new SqlConnection(builder.ConnectionString);
        await databaseConnection.OpenAsync();
        const string databaseSql = """
            IF USER_ID(N'bankofz_api') IS NULL CREATE USER [bankofz_api] FOR LOGIN [bankofz_api];
            IF USER_ID(N'bankofz_operator') IS NULL CREATE USER [bankofz_operator] FOR LOGIN [bankofz_operator];

            IF IS_ROLEMEMBER(N'db_datareader', N'bankofz_api') <> 1 ALTER ROLE [db_datareader] ADD MEMBER [bankofz_api];
            IF IS_ROLEMEMBER(N'db_datawriter', N'bankofz_api') <> 1 ALTER ROLE [db_datawriter] ADD MEMBER [bankofz_api];

            IF IS_ROLEMEMBER(N'db_datareader', N'bankofz_operator') <> 1 ALTER ROLE [db_datareader] ADD MEMBER [bankofz_operator];
            IF IS_ROLEMEMBER(N'db_datawriter', N'bankofz_operator') <> 1 ALTER ROLE [db_datawriter] ADD MEMBER [bankofz_operator];
            IF IS_ROLEMEMBER(N'db_ddladmin', N'bankofz_operator') <> 1 ALTER ROLE [db_ddladmin] ADD MEMBER [bankofz_operator];
            """;
        await using var databaseCommand = new SqlCommand(databaseSql, databaseConnection);
        await databaseCommand.ExecuteNonQueryAsync();
    }

    private static string RequiredSecret(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: >= 16 } value
            ? value
            : throw new InvalidOperationException($"{name} with at least 16 characters is required.");
}
