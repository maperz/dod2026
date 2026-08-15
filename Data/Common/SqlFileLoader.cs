namespace SnowflakeDapperExample.Data.Common;

public static class SqlFileLoader
{
    private static readonly Dictionary<string, string> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Lock CacheLock = new();

    public static string Load(string fileName)
    {
        lock (CacheLock)
        {
            if (Cache.TryGetValue(fileName, out var sql))
            {
                return sql;
            }

            var path = GetSqlFilePath(fileName);
            sql = File.ReadAllText(path);
            Cache[fileName] = sql;

            return sql;
        }
    }


    private static string GetSqlFilePath(string fileName)
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, "Sql", fileName);
        if (File.Exists(outputPath))
        {
            return outputPath;
        }

        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var sourcePath = Path.Combine(directory.FullName, "Sql", fileName);
            if (File.Exists(sourcePath))
            {
                return sourcePath;
            }

            directory = directory.Parent;
        }

        return outputPath;
    }
}
