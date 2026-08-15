namespace SnowflakeDapperExample.Data.Common;

public static class SnowflakeConnectionStringBuilder
{
    public static string BuildFromEnvironment()
    {
        var account = Required("SNOWFLAKE_ACCOUNT");
        var user = Required("SNOWFLAKE_USER");
        var password = Required("SNOWFLAKE_PASSWORD");
        var warehouse = Required("SNOWFLAKE_WAREHOUSE");
        var database = Environment.GetEnvironmentVariable("SNOWFLAKE_DATABASE");
        var schema = Environment.GetEnvironmentVariable("SNOWFLAKE_SCHEMA") ?? "PUBLIC";
        var role = Environment.GetEnvironmentVariable("SNOWFLAKE_ROLE");

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


    private static string Required(string name)
    {
        return Environment.GetEnvironmentVariable(name)
               ?? throw new InvalidOperationException(
                   $"Required environment variable '{name}' is not set.");
    }


    // ADO.NET connection strings use doubled '=' characters inside values.
    private static string Escape(string value)
    {
        return value.Replace("=", "==");
    }
}
