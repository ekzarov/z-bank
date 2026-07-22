using BankOfZ.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BankOfZ.IntegrationTests;

public sealed class BankOfZApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BankOfZ"] = connectionString,
                ["Authentication:LockoutMinutes"] = "15",
                ["Authentication:MaxFailedAttempts"] = "3"
            });
        });
        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(service =>
                service.ServiceType == typeof(DbContextOptions<BankOfZIdentityContext>));
            services.Remove(descriptor);
            services.AddDbContext<BankOfZIdentityContext>(options => options.UseSqlServer(connectionString));
        });
    }
}
