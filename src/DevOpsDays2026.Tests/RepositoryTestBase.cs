using Dapper;
using DevOpsDays2026.Data.Common;

namespace DevOpsDays2026.Tests;

public abstract class RepositoryTestBase
{
    private static readonly string[] RequiredSnowflakeEnvironmentVariables =
    [
        "SNOWFLAKE_ACCOUNT",
        "SNOWFLAKE_USER",
        "SNOWFLAKE_PASSWORD",
        "SNOWFLAKE_WAREHOUSE"
    ];

    static RepositoryTestBase()
    {
        EnvironmentFileLoader.Load(EnvironmentFileLoader.GetEnvironmentFilePath("ci.env"));

        SqlMapper.Settings.UseIncrementalPseudoPositionalParameterNames = true;
        SqlMapper.AddTypeHandler(new GuidTypeHandler());
    }

    protected static SnowflakeConnectionFactory CreateSnowflakeConnectionFactory()
    {
        var missingVariables = RequiredSnowflakeEnvironmentVariables
            .Where(name => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
            .ToArray();

        if (missingVariables.Length > 0)
        {
            throw new InvalidOperationException(
                $"Missing required environment variables: {string.Join(", ", missingVariables)}");
        }

        return new SnowflakeConnectionFactory(
            SnowflakeConnectionStringBuilder.BuildFromEnvironment());
    }
}
