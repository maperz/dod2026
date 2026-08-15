using System.Data.Common;
using Snowflake.Data.Client;

namespace DevOpsDays2026.Data.Common;

public sealed class SnowflakeConnectionFactory(
    SnowflakeConnectionStringBuilder connectionStringBuilder)
{
    public async Task<DbConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = new SnowflakeDbConnection
        {
            ConnectionString = connectionStringBuilder.Build()
        };

        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}