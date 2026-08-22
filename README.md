# DevOpsDays2026

Small .NET Blazor demo application for exploring stock prices and trends from Snowflake.

This project is used for a workshop demonstration at DevOpsDays 2026 in Graz.

## Run

Requires the .NET 10 SDK and access to the expected Snowflake database.

Copy `example.env` to `app.env`, fill in the Snowflake values, then run:

```bash
dotnet run --project src/DevOpsDays2026/DevOpsDays2026.csproj
```

## Contents

- Blazor web UI for stock prices and trends
- Dapper-based Snowflake data access
- SQL files and a small test project
