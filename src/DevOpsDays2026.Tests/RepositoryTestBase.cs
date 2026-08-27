using Dapper;
using DevOpsDays2026.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevOpsDays2026.Tests;

public abstract class RepositoryTestBase
{
    private static readonly IServiceProvider Services;

    static RepositoryTestBase()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("app.json", optional: true)
            .AddJsonFile("ci.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        Services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<SnowflakeConnectionStringBuilder>()
            .AddSingleton<SnowflakeConnectionFactory>()
            .BuildServiceProvider();

        SqlMapper.Settings.UseIncrementalPseudoPositionalParameterNames = true;
        SqlMapper.AddTypeHandler(new GuidTypeHandler());
    }

    protected static SnowflakeConnectionFactory CreateSnowflakeConnectionFactory()
    {
        return Services.GetRequiredService<SnowflakeConnectionFactory>();
    }
}