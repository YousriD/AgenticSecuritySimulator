namespace AgenticSecuritySimulator.Web.Services;

public static class ContentPaths
{
    public static string ResolveRepoRoot(IWebHostEnvironment env)
    {
        var dir = new DirectoryInfo(env.ContentRootPath);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "data")))
                return dir.FullName;
            dir = dir.Parent;
        }

        return Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "..", ".."));
    }

    public static string DataDirectory(IWebHostEnvironment env) =>
        Path.Combine(ResolveRepoRoot(env), "data");

    public static string ScenariosDirectory(IWebHostEnvironment env)
    {
        var candidates = new[]
        {
            Path.Combine(DataDirectory(env), "scenarios"),
            Path.Combine(AppContext.BaseDirectory, "data", "scenarios")
        };
        foreach (var dir in candidates)
        {
            if (Directory.Exists(dir) && Directory.EnumerateFiles(dir, "*.json").Any())
                return dir;
        }
        return candidates[0];
    }

    public static string SampleCsvPath(IWebHostEnvironment env) =>
        Path.Combine(DataDirectory(env), "samples", "lansweeper-dummy-export.csv");

    public static string CompanyTwinCsvPath(IWebHostEnvironment env)
    {
        var root = ResolveRepoRoot(env);
        var rootFile = Path.Combine(root, "company_digital_twin_all_in_one.csv");
        if (File.Exists(rootFile))
            return rootFile;
        return Path.Combine(DataDirectory(env), "samples", "company_digital_twin_all_in_one.csv");
    }
}
