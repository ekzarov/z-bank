using System.Text.Json;
using BankOfZ.Domain.Security;
using BankOfZ.Infrastructure.DataInitialization;
using BankOfZ.Infrastructure.Identity;
using BankOfZ.Infrastructure.Persistence;
using BankOfZ.Setup;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var options = SetupCommandOptions.Parse(args);
var builder = Host.CreateApplicationBuilder();
var operatorId = Environment.GetEnvironmentVariable("BANKOFZ_SETUP_OPERATOR")
    ?? throw new InvalidOperationException("BANKOFZ_SETUP_OPERATOR is required.");
var connectionName = options.Command == "provision-access" ? "BankOfZAdmin" : "BankOfZOperator";
var connectionString = builder.Configuration.GetSection("ConnectionStrings")[connectionName]
    ?? throw new InvalidOperationException($"Connection string '{connectionName}' is required.");

if (options.Command == "provision-access")
{
    await DatabaseAccessProvisioner.ProvisionAsync(connectionString);
    Console.WriteLine("""{"status":"succeeded","operation":"provision-access"}""");
    return;
}

builder.Services.AddDbContext<BankOfZIdentityContext>(db => db.UseSqlServer(connectionString));
builder.Services
    .AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<BankOfZIdentityContext>();

using var host = builder.Build();
await using var scope = host.Services.CreateAsyncScope();
var database = scope.ServiceProvider.GetRequiredService<BankOfZIdentityContext>();

switch (options.Command)
{
    case "inspect-migrations":
        await InspectMigrationsAsync(database);
        break;
    case "migrate":
        var migrationStartedAt = DateTimeOffset.UtcNow;
        var migrationEnvironment = options.Required("environment");
        await database.Database.MigrateAsync();
        var migration = (await database.Database.GetAppliedMigrationsAsync()).LastOrDefault() ?? "none";
        database.SetupOperationAudits.Add(SetupOperationAudit.Succeeded(
            "migrate",
            operatorId,
            migrationEnvironment,
            migration,
            migrationStartedAt,
            DateTimeOffset.UtcNow));
        await database.SaveChangesAsync();
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            status = "succeeded",
            migration,
            @operator = operatorId
        }));
        break;
    case "import":
        await ImportAsync(database, options, operatorId);
        break;
    case "verify-import":
        await VerifyImportAsync(database, options);
        break;
    case "reset-demo":
        await ResetDemoAsync(scope.ServiceProvider, database, options, operatorId);
        break;
    default:
        throw new ArgumentException($"Unknown setup command '{options.Command}'.");
}

static async Task InspectMigrationsAsync(BankOfZIdentityContext database)
{
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        applied = await database.Database.GetAppliedMigrationsAsync(),
        pending = await database.Database.GetPendingMigrationsAsync()
    }, ImportPackageJson.Options));
}

static async Task ImportAsync(
    BankOfZIdentityContext database,
    SetupCommandOptions options,
    string operatorId)
{
    var file = options.Required("file");
    var environment = options.Required("environment");
    var bytes = await File.ReadAllBytesAsync(file);
    var result = await new DataImportService(database).ImportAsync(bytes, operatorId, environment);
    Console.WriteLine(JsonSerializer.Serialize(result, ImportPackageJson.Options));
}

static async Task VerifyImportAsync(BankOfZIdentityContext database, SetupCommandOptions options)
{
    var fingerprint = options.Required("fingerprint");
    var result = await new DataImportService(database).VerifyAsync(fingerprint);
    if (result is null)
    {
        Environment.ExitCode = 2;
        Console.WriteLine("""{"status":"not_found"}""");
        return;
    }

    Console.WriteLine(JsonSerializer.Serialize(result, ImportPackageJson.Options));
    if (result.Status != ImportRunStatus.Succeeded)
    {
        Environment.ExitCode = 3;
    }
}

static async Task ResetDemoAsync(
    IServiceProvider services,
    BankOfZIdentityContext database,
    SetupCommandOptions options,
    string operatorId)
{
    var resetStartedAt = DateTimeOffset.UtcNow;
    var environment = options.Required("environment");
    SetupAuthorization.EnsureResetAllowed(environment, options.Required("confirm"));
    var start = DateOnly.ParseExact(options.Required("start"), "yyyy-MM-dd");
    var end = DateOnly.ParseExact(options.Required("end"), "yyyy-MM-dd");
    var stepDays = int.Parse(options.Required("step-days"));
    var seed = int.Parse(options.Required("seed"));
    var password = Environment.GetEnvironmentVariable("BANKOFZ_DEMO_PASSWORD")
        ?? throw new InvalidOperationException("BANKOFZ_DEMO_PASSWORD is required for demo reset.");

    var package = DemoPackageFactory.Create(start, end, stepDays, seed);
    var bytes = JsonSerializer.SerializeToUtf8Bytes(package, ImportPackageJson.Options);

    if ((await database.Database.GetPendingMigrationsAsync()).Any())
    {
        throw new InvalidOperationException("Apply all migrations explicitly before demo reset.");
    }
    await DemoResetService.ClearAsync(database);
    var import = await new DataImportService(database).ImportAsync(bytes, operatorId, environment);
    await DemoIdentityProvisioner.ProvisionAsync(
        services.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
        services.GetRequiredService<UserManager<ApplicationUser>>(),
        password);
    var completedAt = DateTimeOffset.UtcNow;
    database.SetupOperationAudits.Add(SetupOperationAudit.Succeeded(
        "reset-demo",
        operatorId,
        environment,
        import.MigrationVersion,
        resetStartedAt,
        completedAt));
    await database.SaveChangesAsync();
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        status = "succeeded",
        package = ImportPackageJson.CurrentSchemaVersion,
        parameters = new { start, end, stepDays, seed },
        import
    }, ImportPackageJson.Options));
}
