namespace DevOpsDays2026.Models;

public sealed record DayResult<T>(
    IReadOnlyList<T> Rows,
    string? Date,
    string? PreviousDate,
    string? NextDate);
