using Dapper;
using DevOpsDays2026.Data.Common;
using DevOpsDays2026.Models;

namespace DevOpsDays2026.Data;

public sealed class WeatherRepository(SnowflakeConnectionFactory connectionFactory)
{
    public async Task<IReadOnlyList<WeatherReport>> SearchByCityAsync(
        string? city,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return [];
        }

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<WeatherReport>(
            new CommandDefinition(
                SqlFileLoader.Load("weather-by-city.sql"),
                new { city = city.Trim() },
                cancellationToken: cancellationToken));

        return rows.AsList();
    }
}
