using Dapper;
using DevOpsDays2026.Data.Common;
using DevOpsDays2026.Models;

namespace DevOpsDays2026.Data;

public sealed class StockRepository(SnowflakeConnectionFactory connectionFactory)
{
    public Task<DayResult<StockPrice>> GetDailyStockPricesAsync(
        DateTimeOffset? date,
        string? ticker,
        CancellationToken cancellationToken = default)
    {
        return QueryDayAsync<StockPrice>(
            "stock-prices-by-day.sql",
            "stock-prices-by-day-filtered.sql",
            "latest-stock-price-date.sql",
            "previous-stock-price-date.sql",
            "next-stock-price-date.sql",
            date,
            ticker,
            cancellationToken);
    }

    public Task<DayResult<StockDailyReturn>> GetDailyReturnsAsync(
        DateTimeOffset? date,
        string? ticker,
        CancellationToken cancellationToken = default)
    {
        return QueryDayAsync<StockDailyReturn>(
            "daily-returns-by-day.sql",
            "daily-returns-by-day-filtered.sql",
            "latest-daily-return-date.sql",
            "previous-daily-return-date.sql",
            "next-daily-return-date.sql",
            date,
            ticker,
            cancellationToken);
    }

    public Task<DateTimeOffset?> GetLatestStockPriceDateAsync(CancellationToken cancellationToken = default)
    {
        return GetLatestDateAsync("latest-stock-price-date.sql", cancellationToken);
    }

    public Task<DateTimeOffset?> GetLatestDailyReturnDateAsync(
        CancellationToken cancellationToken = default)
    {
        return GetLatestDateAsync("latest-daily-return-date.sql", cancellationToken);
    }

    public async Task<IReadOnlyList<StockPrice>> GetStockPriceHistoryAsync(
        string ticker,
        CancellationToken cancellationToken = default)
    {
        var sql = SqlFileLoader.Load("stock-price-history.sql");

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<StockPrice>(
            new CommandDefinition(sql, new { ticker }, cancellationToken: cancellationToken));

        return rows.AsList();
    }

    private async Task<DateTimeOffset?> GetLatestDateAsync(
        string queryFileName,
        CancellationToken cancellationToken)
    {
        var sql = SqlFileLoader.Load(queryFileName);

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<DateTimeOffset?>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    private async Task<DayResult<T>> QueryDayAsync<T>(
        string queryFileName,
        string filteredQueryFileName,
        string latestDateQueryFileName,
        string previousDateQueryFileName,
        string nextDateQueryFileName,
        DateTimeOffset? date,
        string? ticker,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        date ??= await GetLatestDateAsync(connection, latestDateQueryFileName, cancellationToken);

        if (date is null)
        {
            return new DayResult<T>([], null, null, null);
        }

        var parameters = new DynamicParameters();
        parameters.Add("date", date.Value.Date);

        var query = queryFileName;
        if (!string.IsNullOrWhiteSpace(ticker))
        {
            query = filteredQueryFileName;
            parameters.Add("ticker", ticker.Trim());
        }

        var rowsSql = SqlFileLoader.Load(query);

        var rows = await connection.QueryAsync<T>(
            new CommandDefinition(rowsSql, parameters, cancellationToken: cancellationToken));

        var previousDate = await GetAdjacentDateAsync(
            connection,
            previousDateQueryFileName,
            date.Value,
            cancellationToken);

        var nextDate = await GetAdjacentDateAsync(
            connection,
            nextDateQueryFileName,
            date.Value,
            cancellationToken);

        return new DayResult<T>(rows.AsList(), date, previousDate, nextDate);
    }

    private static async Task<DateTimeOffset?> GetLatestDateAsync(
        System.Data.Common.DbConnection connection,
        string queryFileName,
        CancellationToken cancellationToken)
    {
        var sql = SqlFileLoader.Load(queryFileName);

        return await connection.ExecuteScalarAsync<DateTimeOffset?>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    private static async Task<DateTimeOffset?> GetAdjacentDateAsync(
        System.Data.Common.DbConnection connection,
        string queryFileName,
        DateTimeOffset date,
        CancellationToken cancellationToken)
    {
        var sql = SqlFileLoader.Load(queryFileName);

        return await connection.ExecuteScalarAsync<DateTimeOffset?>(
            new CommandDefinition(sql, new { date = date.Date }, cancellationToken: cancellationToken));
    }
}
