using Dapper;
using SnowflakeDapperExample.Data.Common;
using SnowflakeDapperExample.Models;

namespace SnowflakeDapperExample.Data;

public sealed class StockRepository(SnowflakeConnectionFactory connectionFactory)
{
    public Task<DayResult<StockPrice>> GetDailyStockPricesAsync(
        string? date,
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
        string? date,
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


    public Task<string?> GetLatestStockPriceDateAsync(CancellationToken cancellationToken = default)
    {
        return GetLatestDateAsync("latest-stock-price-date.sql", cancellationToken);
    }


    public Task<string?> GetLatestDailyReturnDateAsync(CancellationToken cancellationToken = default)
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


    private async Task<string?> GetLatestDateAsync(
        string queryFileName,
        CancellationToken cancellationToken)
    {
        var sql = SqlFileLoader.Load(queryFileName);

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<string?>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
    }


    private async Task<DayResult<T>> QueryDayAsync<T>(
        string queryFileName,
        string filteredQueryFileName,
        string latestDateQueryFileName,
        string previousDateQueryFileName,
        string nextDateQueryFileName,
        string? date,
        string? ticker,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        date = string.IsNullOrWhiteSpace(date)
            ? await GetLatestDateAsync(connection, latestDateQueryFileName, cancellationToken)
            : date;

        if (string.IsNullOrWhiteSpace(date))
        {
            return new DayResult<T>([], null, null, null);
        }

        var parameters = new DynamicParameters();
        parameters.Add("date", date);

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
            date,
            cancellationToken);

        var nextDate = await GetAdjacentDateAsync(
            connection,
            nextDateQueryFileName,
            date,
            cancellationToken);

        return new DayResult<T>(rows.AsList(), date, previousDate, nextDate);
    }


    private static async Task<string?> GetLatestDateAsync(
        System.Data.Common.DbConnection connection,
        string queryFileName,
        CancellationToken cancellationToken)
    {
        var sql = SqlFileLoader.Load(queryFileName);

        return await connection.ExecuteScalarAsync<string?>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
    }


    private static async Task<string?> GetAdjacentDateAsync(
        System.Data.Common.DbConnection connection,
        string queryFileName,
        string date,
        CancellationToken cancellationToken)
    {
        var sql = SqlFileLoader.Load(queryFileName);

        return await connection.ExecuteScalarAsync<string?>(
            new CommandDefinition(sql, new { date }, cancellationToken: cancellationToken));
    }


}
