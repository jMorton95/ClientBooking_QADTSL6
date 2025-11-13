global using Microsoft.AspNetCore.Http.HttpResults;
global using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseHttpsRedirection();


app.Run();
