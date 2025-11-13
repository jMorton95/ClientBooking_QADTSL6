global using Microsoft.AspNetCore.Http.HttpResults;
global using Microsoft.EntityFrameworkCore;
using ClientBooking.Components;
using ClientBooking.Configuration;
using ClientBooking.Data;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.AddPostGresDatabaseFromConfiguration();

if (builder.Environment.IsProduction())
{
}
else
{
    StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
}

builder.Services.AddAntiforgery();

builder.Services.AddRazorComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

//Add Migrations here

await app.ApplyStartupDatabaseMigrations();

app.MapStaticAssets();

app.UseAntiforgery();

app.MapRazorComponents<App>();

app.Run();
