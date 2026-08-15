# Snowflake + C# + Dapper example

A minimal .NET 8 console application using:

- `Snowflake.Data` — Snowflake's official ADO.NET driver
- `Dapper` — lightweight object mapping and parameterized SQL

The project intentionally uses Dapper rather than EF Core because Snowflake's official .NET
integration is an ADO.NET driver, not an official EF Core provider. For a query-oriented warehouse
such as Snowflake, Dapper is usually the simpler fit.

## Prerequisites

- .NET 10 SDK
- A Snowflake account
- A warehouse, database, schema, and user/role with appropriate permissions

## 1. Create the example table

Run `sql/setup.sql` in Snowflake.

## 2. Set connection environment variables

macOS/Linux:

```bash
export SNOWFLAKE_ACCOUNT="myorg-myaccount"
export SNOWFLAKE_USER="my_user"
export SNOWFLAKE_PASSWORD="my_password"
export SNOWFLAKE_WAREHOUSE="COMPUTE_WH"
export SNOWFLAKE_DATABASE="DEMO_DB"
export SNOWFLAKE_SCHEMA="PUBLIC"
export SNOWFLAKE_ROLE="DEVELOPER"
```

PowerShell:

```powershell
$env:SNOWFLAKE_ACCOUNT="myorg-myaccount"
$env:SNOWFLAKE_USER="my_user"
$env:SNOWFLAKE_PASSWORD="my_password"
$env:SNOWFLAKE_WAREHOUSE="COMPUTE_WH"
$env:SNOWFLAKE_DATABASE="DEMO_DB"
$env:SNOWFLAKE_SCHEMA="PUBLIC"
$env:SNOWFLAKE_ROLE="DEVELOPER"
```

`SNOWFLAKE_DATABASE` is only added to the connection string when it is set. `SNOWFLAKE_SCHEMA`
defaults to `PUBLIC` when it is not set. The query files use schema-relative table names such as
`"DAILY_STOCK_PRICES"`, so tests can point the same SQL at an isolated schema:

```bash
export SNOWFLAKE_SCHEMA="CUSTOM_CI_123"
```

As long as that schema contains compatible copies or temporary versions of the expected tables, the
application code and SQL files do not need to change.

Do not commit credentials to source control.

## 3. Restore and run

```bash
dotnet restore
dotnet run
```

## Why the SQL uses `?id?` instead of `@id`

Snowflake's .NET driver uses positional `?` bind markers. Dapper supports a pseudo-positional syntax
such as `?id?`; Dapper rewrites it to `?` while still binding the corresponding object property
safely.

At startup the project also enables:

```csharp
SqlMapper.Settings.UseIncrementalPseudoPositionalParameterNames = true;
```

This setting exists specifically to support providers with Snowflake-style parameter naming
conventions.

## Authentication

This example uses username/password only to keep the sample easy to run. For production, prefer the
authentication mechanism required by your organization, such as key-pair authentication, OAuth,
workload identity, or another supported Snowflake authentication mode.

## Structure

```text
SnowflakeDapperExample/
├── Data/
│   ├── CustomerRepository.cs
│   └── SnowflakeConnectionFactory.cs
├── Models/
│   └── Customer.cs
├── sql/
│   └── setup.sql
├── .env.example
├── .gitignore
├── Program.cs
└── SnowflakeDapperExample.csproj
```

## Notes about EF Core

EF Core requires a relational database provider that implements EF Core's provider abstractions and
SQL dialect behavior. Snowflake publishes an official .NET ADO.NET driver, but this sample does not
depend on a Snowflake-maintained EF Core provider. Using a third-party EF provider adds another
compatibility layer, especially around migrations, generated SQL, identity/key semantics, and
Snowflake-specific types.

If the application mainly queries warehouse data, Dapper or direct ADO.NET is generally a more
transparent design. If you need domain change tracking and rich aggregate persistence, a
transactional database behind EF Core plus Snowflake for analytics is often a cleaner architecture.
