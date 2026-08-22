namespace DevOpsDays2026.Models;

public sealed record DayResult<T>(
    IReadOnlyList<T> Rows,
    DateTimeOffset? Date,
    DateTimeOffset? PreviousDate,
    DateTimeOffset? NextDate);
