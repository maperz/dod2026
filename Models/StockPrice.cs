namespace SnowflakeDapperExample.Models;

public sealed class StockPrice
{
    public string Ticker { get; init; } = string.Empty;
    public DateTimeOffset Date { get; init; }
    public decimal ClosePrice { get; init; }
}