using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using BankOfZ.Api.Security;
using BankOfZ.Api.ErrorHandling;
using BankOfZ.Application.Common;
using BankOfZ.Application.Accounts;
using BankOfZ.Application.Customers;
using BankOfZ.Application.Security;
using BankOfZ.Domain.Security;
using BankOfZ.Infrastructure.Identity;
using BankOfZ.Infrastructure.Common;
using BankOfZ.Infrastructure.Accounts;
using BankOfZ.Infrastructure.Customers;
using BankOfZ.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BankOfZIdentityContext>((services, options) =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("BankOfZ")
        ?? throw new InvalidOperationException("Connection string 'BankOfZ' is required.");
    options.UseSqlServer(connectionString);
});

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        var authentication = builder.Configuration.GetSection("Authentication");
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(
            authentication.GetValue("LockoutMinutes", 15));
        options.Lockout.MaxFailedAccessAttempts = authentication.GetValue("MaxFailedAttempts", 5);
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<BankOfZIdentityContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "BankOfZ.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.Path = "/";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Authentication required"
        });
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Access denied"
        });
    };
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "BankOfZ.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.Path = "/";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.HeaderName = "X-XSRF-TOKEN";
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.Customer, policy => policy.RequireRole(BankRoles.Customer))
    .AddPolicy(AuthorizationPolicies.Operator, policy => policy.RequireRole(BankRoles.Operator))
    .AddPolicy(AuthorizationPolicies.Administrator, policy => policy.RequireRole(BankRoles.Administrator));

builder.Services.AddSingleton<CustomerAccessEvaluator>();
builder.Services.AddSingleton<InvalidCredentialWorkFactor>();
var customerOptions = builder.Configuration.GetSection(CustomerOptions.SectionName).Get<CustomerOptions>()
    ?? new CustomerOptions();
if (customerOptions.CreditProviders.Length == 0)
{
    customerOptions.CreditProviders = CustomerOptions.DefaultCreditProviders.ToArray();
}
builder.Services.AddSingleton(customerOptions);
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton(builder.Configuration.GetSection(AccountOptions.SectionName).Get<AccountOptions>() ?? new AccountOptions());
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAccountAuditWriter, AccountAuditWriter>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerAuditWriter, CustomerAuditWriter>();
builder.Services.AddScoped<ICustomerAccountStatusReader, CustomerAccountStatusReader>();
builder.Services.AddScoped<ICreditAssessmentProvider, DeterministicCreditAssessmentProvider>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<ISecurityAudit, SecurityAudit>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CustomerExceptionHandler>();
builder.Services.AddExceptionHandler<AccountExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("172.16.0.0/12"));
});
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
