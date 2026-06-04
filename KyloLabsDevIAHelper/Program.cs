using KyloLabs.DevIAHelper.Console.Interfaces.Repositories;
using KyloLabs.DevIAHelper.Console.Interfaces.Services;
using KyloLabs.DevIAHelper.Console.Repositories;
using KyloLabs.DevIAHelper.Console.Services;
using KyloLabs.DevIAHelper.Core;
using KyloLabs.DevIAHelper.Core.Models.Nvidia;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//builder.Services.AddScoped<DevIAHelperFunctions>();
builder.Services.AddSingleton<DevIAHelperFunctions>();
builder.Services.AddScoped<IDevIAHelperRepository, DevIAHelperRepository>();
builder.Services.AddScoped<IDevIAHelperService, DevIAHelperService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
