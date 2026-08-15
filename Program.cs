using Dapper;
using SnowflakeDapperExample;
using SnowflakeDapperExample.Components;
using SnowflakeDapperExample.Data;
using SnowflakeDapperExample.Data.Common;
using SnowflakeDapperExample.Endpoints;

// Snowflake uses positional bind markers. This Dapper setting gives those
// generated parameter names deterministic incremental names.
SqlMapper.Settings.UseIncrementalPseudoPositionalParameterNames = true;
SqlMapper.AddTypeHandler(new GuidTypeHandler());

EnvironmentFileLoader.Load(EnvironmentFileLoader.GetEnvironmentFilePath("app.env"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton(
    new SnowflakeConnectionFactory(SnowflakeConnectionStringBuilder.BuildFromEnvironment()));
builder.Services.AddScoped<StockRepository>();
builder.Services.AddScoped<StockNewsRepository>();

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
