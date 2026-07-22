using System.Net;
using BankOfZ.Api.Security;
using BankOfZ.Application.Security;
using BankOfZ.Domain.Security;
using BankOfZ.Infrastructure.Identity;
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
builder.Services.AddScoped<ISecurityAudit, SecurityAudit>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("172.16.0.0/12"));
});
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
