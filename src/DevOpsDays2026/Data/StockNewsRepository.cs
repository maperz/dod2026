using Dapper;
using DevOpsDays2026.Data.Common;
using DevOpsDays2026.Models;

namespace DevOpsDays2026.Data;

public sealed class StockNewsRepository(SnowflakeConnectionFactory connectionFactory)
{
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

        await connection.ExecuteAsync(
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

        return await GetByIdAsync(request.Id, cancellationToken)
               ?? throw new InvalidOperationException("Inserted stock news row could not be read.");
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
            : await GetByIdAsync(id, cancellationToken);
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
}
