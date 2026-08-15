using System.Data.Common;
using Snowflake.Data.Client;

namespace SnowflakeDapperExample.Data.Common;

public sealed class SnowflakeConnectionFactory(string connectionString)
{
    public async Task<DbConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = new SnowflakeDbConnection
        {
            ConnectionString = connectionString
        };

        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}