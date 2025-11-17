global using Microsoft.AspNetCore.Http.HttpResults;
global using Microsoft.EntityFrameworkCore;
using ClientBooking.Authentication;
using ClientBooking.Components;
using ClientBooking.Configuration;
using ClientBooking.Data;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;

var builder = WebApplication.CreateBuilder(args);


//Override Appsettings values with values injected in from Github Secrets, during pipeline run.
builder.Configuration.AddEnvironmentVariables();


//Enable HTTP Context services.
builder.Services.AddHttpContextAccessor();


//Enable application to store session data in application memory.
builder.Services.AddDistributedMemoryCache();


//Configure session stored in HTTP only cookies.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(12);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});


//Configure our database connection
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


//Adds Token to prevent POST request forgery
builder.Services.AddAntiforgery();

//Add Frontend Pages / Code Behind.
builder.Services.AddRazorComponents();



var app = builder.Build();

//Detect and apply migrations, seed on first run.
await app.ApplyStartupDatabaseMigrations();

//Add Middleware in Production for error page redirecting and strict transport security headers.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

//Enable session state

app.MapStaticAssets();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

//Enable custom Auth Middleware
app.UseMiddleware<AuthenticationMiddleware>();

//Add middleware
app.UseAntiforgery();

//Apply our Frontend components to their defined website routes.
app.MapRazorComponents<App>();

//Map custom application endpoints, mostly POST requests.
app.MapApplicationRequestHandlers();

app.Run();
