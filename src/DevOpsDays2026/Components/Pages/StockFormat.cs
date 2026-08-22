using System.Globalization;

namespace DevOpsDays2026.Components.Pages;

public static class StockFormat
{
    public static string? InputDate(DateTimeOffset? date)
    {
        return date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static DateTimeOffset? ParseInputDate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date))
        {
            return null;
        }

        return DateOnly.TryParseExact(
            date,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsedDate)
            ? new DateTimeOffset(parsedDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            : null;
    }

    public static string Date(DateTimeOffset date)
    {
        return date.ToString("d", CultureInfo.CurrentCulture);
    }

    public static string Dollar(decimal price)
    {
        return price.ToString("C2", CultureInfo.GetCultureInfo("en-US"));
    }

    public static string DailyReturn(double dailyReturn)
    {
        const double roundingTolerance = 0.0000005;
        var percentage = Math.Abs(dailyReturn * 100).ToString(
            "0.00",
            CultureInfo.CurrentCulture);

        return dailyReturn switch
        {
            > roundingTolerance => $"▲ + {percentage}%",
            < -roundingTolerance => $"▼ - {percentage}%",
            _ => $"-   {percentage}%"
        };
    }

    public static string DailyReturnClass(double dailyReturn)
    {
        const double roundingTolerance = 0.0000005;

        return dailyReturn switch
        {
            > roundingTolerance => "return-value positive",
            < -roundingTolerance => "return-value negative",
            _ => "return-value"
        };
    }
}
