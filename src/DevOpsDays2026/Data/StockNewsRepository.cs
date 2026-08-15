using Dapper;
using DevOpsDays2026.Data.Common;
using DevOpsDays2026.Models;
using System.Text.Json;

namespace DevOpsDays2026.Data;

public sealed class StockNewsRepository(SnowflakeConnectionFactory connectionFactory)
{
    private const int MaxSearchResults = 100;

    public async Task<IReadOnlyList<StockNews>> GetAllAsync(
        string? ticker = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        var query = "stock-news-list.sql";

        if (!string.IsNullOrWhiteSpace(ticker))
        {
            query = "stock-news-list-by-ticker.sql";
            parameters.Add("ticker", ticker.Trim());
        }

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<StockNews>(
            new CommandDefinition(
                SqlFileLoader.Load(query),
                parameters,
                cancellationToken: cancellationToken));

        return rows.AsList();
    }

    public async Task<IReadOnlyList<StockNewsEventSearchResult>> SearchEventsAsync(
        string searchText,
        int limit = 25,
        bool useCortexSearch = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return [];
        }

        var boundedLimit = Math.Clamp(limit, 1, MaxSearchResults);
        var sql = useCortexSearch
            ? SqlFileLoader.Load("stock-news-event-cortex-search.sql")
            : SqlFileLoader.Load("stock-news-event-search.sql");

        object parameters = useCortexSearch
            ? new
            {
                queryParametersJson =
                    BuildCortexSearchQueryParameters(searchText.Trim(), boundedLimit)
            }
            : new
            {
                searchText = searchText.Trim(),
                limit = boundedLimit
            };

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<StockNewsEventSearchResult>(
            new CommandDefinition(
                sql,
                parameters,
                cancellationToken: cancellationToken));

        return rows.AsList();
    }

    private static string BuildCortexSearchQueryParameters(string searchText, int limit)
    {
        return JsonSerializer.Serialize(new
        {
            query = searchText,
            columns = new[] { "HEADLINE", "STOCK", "DATE", "PUBLISHER", "SENTIMENT" },
            limit
        });
    }

    public async Task<StockNews?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<StockNews>(
            new CommandDefinition(
                SqlFileLoader.Load("stock-news-get-by-id.sql"),
                new { id = id.ToString("D") },
                cancellationToken: cancellationToken));
    }

    public async Task<StockNews> CreateAsync(
        StockNewsRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        var affectedRows = await connection.ExecuteAsync(
            new CommandDefinition(
                SqlFileLoader.Load("stock-news-insert.sql"),
                new
                {
                    Id = request.Id.ToString("D"),
                    request.Ticker,
                    request.Text,
                    request.Date
                },
                cancellationToken: cancellationToken));

        if (affectedRows != 1)
        {
            throw new InvalidOperationException(
                $"Expected to insert one stock news row, but inserted {affectedRows}.");
        }

        return ToStockNews(request.Id, request);
    }

    public async Task<StockNews?> UpdateAsync(
        Guid id,
        StockNewsRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        var affectedRows = await connection.ExecuteAsync(
            new CommandDefinition(
                SqlFileLoader.Load("stock-news-update.sql"),
                new
                {
                    id = id.ToString("D"),
                    request.Ticker,
                    request.Text,
                    request.Date
                },
                cancellationToken: cancellationToken));

        return affectedRows == 0
            ? null
            : ToStockNews(id, request);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        var affectedRows = await connection.ExecuteAsync(
            new CommandDefinition(
                SqlFileLoader.Load("stock-news-delete.sql"),
                new { id = id.ToString("D") },
                cancellationToken: cancellationToken));

        return affectedRows > 0;
    }

    private static StockNews ToStockNews(Guid id, StockNewsRequest request)
    {
        return new StockNews
        {
            Id = id,
            Ticker = request.Ticker,
            Text = request.Text,
            Date = request.Date.Date
        };
    }
}
