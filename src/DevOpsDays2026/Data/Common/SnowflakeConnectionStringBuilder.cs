namespace DevOpsDays2026.Data.Common;

using Microsoft.Extensions.Configuration;

public sealed class SnowflakeConnectionStringBuilder(IConfiguration configuration)
{
    public string Build()
    {
        var connectionString = configuration.GetConnectionString("Snowflake")
                               ?? configuration["Snowflake:ConnectionString"]
                               ?? configuration["SNOWFLAKE_CONNECTION_STRING"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var account = Required("Account", "SNOWFLAKE_ACCOUNT");
        var user = Required("User", "SNOWFLAKE_USER");
        var password = Required("Password", "SNOWFLAKE_PASSWORD");
        var warehouse = Required("Warehouse", "SNOWFLAKE_WAREHOUSE");
        var database = Value("Database", "SNOWFLAKE_DATABASE");
        var schema = Value("Schema", "SNOWFLAKE_SCHEMA") ?? "PUBLIC";
        var role = Value("Role", "SNOWFLAKE_ROLE");

        var parts = new List<string>
        {
            $"account={Escape(account)}",
            $"user={Escape(user)}",
            $"password={Escape(password)}",
            $"warehouse={Escape(warehouse)}",
            $"schema={Escape(schema)}"
        };

        if (!string.IsNullOrWhiteSpace(database))
        {
            parts.Add($"db={Escape(database)}");
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            parts.Add($"role={Escape(role)}");
        }

        return string.Join(';', parts);
    }

    private string Required(string key, string environmentVariable)
    {
        return Value(key, environmentVariable)
               ?? throw new InvalidOperationException(
                   $"Required Snowflake configuration value 'Snowflake:{key}' or '{environmentVariable}' is not set.");
    }

    private string? Value(string key, string environmentVariable)
    {
        var value = configuration[$"Snowflake:{key}"] ?? configuration[environmentVariable];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    // ADO.NET connection strings use doubled '=' characters inside values.
    private string Escape(string value)
    {
        return value.Replace("=", "==");
    }
}
