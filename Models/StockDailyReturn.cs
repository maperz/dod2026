namespace SnowflakeDapperExample.Models;

public sealed class StockDailyReturn
{
    public string Ticker { get; init; } = string.Empty;

    public DateTimeOffset Date { get; init; }

    public double DailyReturn { get; init; }
}