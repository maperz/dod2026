namespace DevOpsDays2026.Models;

public sealed class StockNews
{
    public Guid Id { get; init; }
    public string Ticker { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public DateTime Date { get; init; }
}

public sealed record StockNewsRequest
{
    public required Guid Id { get; init; }

    public required string Ticker { get; init; }

    public required string Text { get; init; }

    public required DateTimeOffset Date { get; init; }
}

public sealed class StockNewsEventSearchResult
{
    public string Headline { get; init; } = string.Empty;
    public string Publisher { get; init; } = string.Empty;
    public DateTimeOffset Date { get; init; }
    public string Stock { get; init; } = string.Empty;
    public string Sentiment { get; init; } = string.Empty;
    public string SearchScore { get; init; } = string.Empty;
}
