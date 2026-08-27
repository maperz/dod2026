using Dapper;
using DevOpsDays2026.Components;
using DevOpsDays2026.Data;
using DevOpsDays2026.Data.Common;
using DevOpsDays2026.Endpoints;
using DevOpsDays2026.Models;
using FluentValidation;

// Snowflake uses positional bind markers. This Dapper setting gives those
// generated parameter names deterministic incremental names.
SqlMapper.Settings.UseIncrementalPseudoPositionalParameterNames = true;
SqlMapper.AddTypeHandler(new GuidTypeHandler());

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("app.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<SnowflakeConnectionStringBuilder>();
builder.Services.AddSingleton<SnowflakeConnectionFactory>();
builder.Services.AddScoped<StockRepository>();
builder.Services.AddScoped<StockNewsRepository>();
builder.Services.AddValidatorsFromAssemblyContaining<StockNewsRequestValidator>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapStockNewsApi();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
