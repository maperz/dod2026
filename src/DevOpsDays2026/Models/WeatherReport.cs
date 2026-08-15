namespace DevOpsDays2026.Models;

public sealed class WeatherReport
{
    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public double Temperature { get; init; }

    public int WeatherCode { get; init; }

    public double WindSpeed { get; init; }

    public DateTime Time { get; init; }
}