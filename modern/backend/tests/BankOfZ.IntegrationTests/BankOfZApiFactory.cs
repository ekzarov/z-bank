using BankOfZ.Application.Customers;
using BankOfZ.Application.Common;
using BankOfZ.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BankOfZ.IntegrationTests;

public sealed class BankOfZApiFactory(
    string connectionString,
    CustomerOptions? customerOptions = null,
    Action<IServiceCollection>? configureServices = null) : WebApplicationFactory<Program>
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
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(new FixedTestClock());
            if (customerOptions is not null)
            {
                services.RemoveAll<CustomerOptions>();
                services.AddSingleton(customerOptions);
            }
            configureServices?.Invoke(services);
        });
    }

    private sealed class FixedTestClock : IClock
    {
        private long tick;
        public DateTimeOffset UtcNow =>
            new DateTimeOffset(2026, 7, 22, 12, 0, 0, TimeSpan.Zero)
                .AddMilliseconds(Interlocked.Increment(ref tick));
    }
}
