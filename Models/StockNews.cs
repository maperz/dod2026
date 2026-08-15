namespace SnowflakeDapperExample.Models;

public sealed class StockNews
{
    public Guid Id { get; init; }
    public string Ticker { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public DateTime Date { get; init; }
}

public sealed record StockNewsRequest(
    Guid Id,
    string Ticker,
    string Text,
    string Date);
