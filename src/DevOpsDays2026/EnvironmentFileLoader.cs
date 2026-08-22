namespace DevOpsDays2026;

public static class EnvironmentFileLoader
{
    public static string GetEnvironmentFilePath(string defaultPath)
    {
        return Environment.GetEnvironmentVariable("APP_ENV_FILE") ?? defaultPath;
    }

    public static void Load(string path)
    {
        var resolvedPath = ResolvePath(path);
        if (resolvedPath is null)
        {
            return;
        }

        foreach (var rawLine in File.ReadLines(resolvedPath))
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var name = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim('"');
            Environment.SetEnvironmentVariable(name, value);
        }
    }


    private static string? ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return File.Exists(path) ? path : null;
        }

        if (File.Exists(path))
        {
            return path;
        }

        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, path);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}