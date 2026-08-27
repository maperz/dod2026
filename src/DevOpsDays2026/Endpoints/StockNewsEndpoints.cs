using DevOpsDays2026.Data;
using DevOpsDays2026.Models;

namespace DevOpsDays2026.Endpoints;

public static class StockNewsEndpoints
{
    public static RouteGroupBuilder MapStockNewsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stock-news");

        group.MapGet("/", async (
            string? ticker,
            StockNewsRepository repository,
            CancellationToken cancellationToken) =>
        {
            var rows = await repository.GetAllAsync(ticker, cancellationToken);
            return Results.Ok(rows);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            StockNewsRepository repository,
            CancellationToken cancellationToken) =>
        {
            var row = await repository.GetByIdAsync(id, cancellationToken);
            return row is null ? Results.NotFound() : Results.Ok(row);
        });

        group.MapPost("/", async (
            StockNewsRequest request,
            StockNewsRepository repository,
            CancellationToken cancellationToken) =>
        {
            var row = await repository.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/stock-news/{row.Id}", row);
        })
        .AddEndpointFilter<ValidationFilter<StockNewsRequest>>();

        group.MapPut("/{id:guid}", async (
            Guid id,
            StockNewsRequest request,
            StockNewsRepository repository,
            CancellationToken cancellationToken) =>
        {
            var row = await repository.UpdateAsync(id, request, cancellationToken);
            return row is null ? Results.NotFound() : Results.Ok(row);
        })
        .AddEndpointFilter<ValidationFilter<StockNewsRequest>>();

        group.MapDelete("/{id:guid}", async (
            Guid id,
            StockNewsRepository repository,
            CancellationToken cancellationToken) =>
        {
            var deleted = await repository.DeleteAsync(id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return group;
    }
}
