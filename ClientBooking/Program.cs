global using Microsoft.AspNetCore.Http.HttpResults;
global using Microsoft.EntityFrameworkCore;
using ClientBooking.Components;
using ClientBooking.Configuration;
using ClientBooking.Data;
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

//Allow injection of custom configuration variables
builder.AddConfigurationValues();

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
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(nameof(RoleName.User), 
        policy => policy.RequireAuthenticatedUser());

    options.AddPolicy(nameof(RoleName.Admin), 
        policy => policy.RequireAuthenticatedUser()
            .RequireRole(nameof(RoleName.Admin)));
});

//Configure our database connection and custom services.
builder.AddPostgresDatabaseFromConfiguration();

builder.AddCustomAuthenticationServices();

builder.AddCustomValidators();

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


//Apply auth/security middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();


//Apply our Frontend components to their defined website routes, configure authorisation policy defaults.
app.MapRazorComponents<App>()
    .RequireAuthorization(nameof(RoleName.User));

//Map custom application endpoints, mostly POST requests.
app.MapApplicationRequestHandlers();

app.Run();
