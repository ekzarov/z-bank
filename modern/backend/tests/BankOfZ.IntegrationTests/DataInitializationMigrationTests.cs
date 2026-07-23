using BankOfZ.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BankOfZ.IntegrationTests;

[Trait("Category", DatabaseIntegrationTestCategory.Name)]
[Collection(DatabaseTestCollection.Name)]
public sealed class DataInitializationMigrationTests(BankOfZTestsFixture fixture)
{
    [Fact]
    public async Task Migrations_apply_from_empty_and_roll_back_to_previous_supported_version()
    {
        await using var database = await TemporarySqlDatabase.CreateAsync(fixture.ConnectionString);
        var options = new DbContextOptionsBuilder<BankOfZIdentityContext>()
            .UseSqlServer(database.ConnectionString)
            .Options;
        await using var context = new BankOfZIdentityContext(options);
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync("20260723123549_AddMonthlyStatements");
        Assert.False(await TableExistsAsync(database.ConnectionString, "ImportRuns"));

        await context.Database.MigrateAsync();
        Assert.True(await TableExistsAsync(database.ConnectionString, "ImportRuns"));
        Assert.True(await TableExistsAsync(database.ConnectionString, "ImportAttempts"));
        Assert.True(await TableExistsAsync(database.ConnectionString, "ImportStagedRecords"));

        await migrator.MigrateAsync("20260723123549_AddMonthlyStatements");
        Assert.False(await TableExistsAsync(database.ConnectionString, "ImportRuns"));
    }

    [Fact]
    public async Task Normal_api_startup_does_not_create_objects_in_empty_database()
    {
        await using var database = await TemporarySqlDatabase.CreateAsync(fixture.ConnectionString);
        await using var factory = new BankOfZApiFactory(database.ConnectionString);

        using var client = factory.CreateClient();

        await using var connection = new SqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT COUNT(*) FROM sys.tables", connection);
        Assert.Equal(0, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }

    private static async Task<bool> TableExistsAsync(string connectionString, string table)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT COUNT(*) FROM sys.tables WHERE [name] = @name",
            connection);
        command.Parameters.AddWithValue("@name", table);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }
}

internal sealed class TemporarySqlDatabase : IAsyncDisposable
{
    private TemporarySqlDatabase(string connectionString, string masterConnectionString, string name)
    {
        ConnectionString = connectionString;
        MasterConnectionString = masterConnectionString;
        Name = name;
    }

    public string ConnectionString { get; }
    private string MasterConnectionString { get; }
    private string Name { get; }

    public static async Task<TemporarySqlDatabase> CreateAsync(string templateConnectionString)
    {
        var name = $"BankOfZ_F008_{Guid.NewGuid():N}";
        var databaseBuilder = new SqlConnectionStringBuilder(templateConnectionString)
        {
            InitialCatalog = name
        };
        var masterBuilder = new SqlConnectionStringBuilder(templateConnectionString)
        {
            InitialCatalog = "master"
        };
        await using var connection = new SqlConnection(masterBuilder.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand($"CREATE DATABASE [{name}]", connection);
        await command.ExecuteNonQueryAsync();
        return new TemporarySqlDatabase(databaseBuilder.ConnectionString, masterBuilder.ConnectionString, name);
    }

    public async ValueTask DisposeAsync()
    {
        SqlConnection.ClearAllPools();
        await using var connection = new SqlConnection(MasterConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            $"ALTER DATABASE [{Name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{Name}]",
            connection);
        await command.ExecuteNonQueryAsync();
    }
}
