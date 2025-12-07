global using Microsoft.AspNetCore.Http.HttpResults;
global using Microsoft.EntityFrameworkCore;
using ClientBooking.Authentication;
using ClientBooking.Components;
using ClientBooking.Configuration;
using ClientBooking.Data;
using ClientBooking.Shared;
using ClientBooking.Shared.Enums;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;

var builder = WebApplication.CreateBuilder(args);

//Helpful to ensure all injectable dependencies are registered.
builder.Host.UseDefaultServiceProvider((_, options) =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

//Override Appsettings values with values injected in from Github Secrets, during pipeline run.
builder.Configuration.AddEnvironmentVariables();

//Enable HTTP Context services.
builder.Services.AddHttpContextAccessor();

//Add Token to prevent POST request forgery
builder.Services.AddAntiforgery();

//Add Frontend Pages and components.
builder.Services.AddRazorComponents();

//Configure authentication sessions, stored in HTTP only cookies.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => 
{
    options.Cookie.Name = "ClientBooking.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(3);
    options.SlidingExpiration = true;
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
});

//Configure authorisation services.
builder.Services.AddAuthorizationBuilder()
       .AddPolicy(nameof(RoleName.User), policy => policy.RequireAuthenticatedUser())
       .AddPolicy(nameof(RoleName.Admin), policy => policy.RequireAuthenticatedUser()
           .RequireRole(nameof(RoleName.Admin)));

//Configure our database connection and custom services.
builder.AddPostgresDatabaseFromConfiguration();

builder.AddCustomAuthenticationServices();

builder.AddCustomValidators();

//Configure logging for application using our custom provider to log all custom events.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddSingleton<ILoggerProvider, DatabaseLoggerProvider>();

//Runtime environment behaviour
if (builder.Environment.IsProduction()) { }
else
{
    //Enables local development at runtime.
    StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
}

var app = builder.Build();

//Detect and apply migrations, seed on first run.
await app.ApplyStartupDatabaseMigrations();

//Add Middleware in Production for error page redirecting and strict transport security headers.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

//Server static HTML/CSS/JS files and register routes.
app.MapStaticAssets();
app.UseRouting();
app.UseAntiforgery();


//Apply auth/security middleware
app.UseAuthentication();
app.UseAuthorization();

//Prevent clickjacking
app.Use(async (context, next) => 
{
    context.Response.Headers.XFrameOptions = "DENY";
    await next();
});

//Apply our Frontend components to their defined website routes, configure authorisation policy defaults.
app.MapRazorComponents<App>()
    .RequireAuthorization(nameof(RoleName.User));

//Map custom application endpoints, mostly POST requests.
app.MapApplicationRequestHandlers();

//Add middleware to audit all requests made to the application, used to track engagement.
app.UseMiddleware<RequestAuditMiddleware>();

app.Run();