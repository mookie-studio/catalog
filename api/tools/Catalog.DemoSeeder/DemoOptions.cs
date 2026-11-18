namespace Catalog.DemoSeeder;

/// <summary>
/// Settings of the Demo seeder, bound from the configuration section "Demo" (environment variables
/// <c>Demo__*</c> or user secrets). Every setting is required; none is ever committed.
/// </summary>
public sealed class DemoOptions
{
    public const string SectionName = "Demo";
    public const int MinPasswordLength = 12;

    public string? ApiUrl { get; set; }
    public string? SupabaseUrl { get; set; }
    public string? SupabaseAnonKey { get; set; }
    public string? SupabaseServiceRoleKey { get; set; }
    public string? DatabaseConnectionString { get; set; }
    public string? PlatformAdminEmail { get; set; }
    public string? PlatformAdminPassword { get; set; }
    public string? AdminPassword { get; set; }

    /// <summary>Every problem with the settings, each naming the setting as its environment variable.</summary>
    public List<string> Validate()
    {
        var errors = new List<string>();
        Require(ApiUrl, nameof(ApiUrl));
        Require(SupabaseUrl, nameof(SupabaseUrl));
        Require(SupabaseAnonKey, nameof(SupabaseAnonKey));
        Require(SupabaseServiceRoleKey, nameof(SupabaseServiceRoleKey));
        Require(DatabaseConnectionString, nameof(DatabaseConnectionString));
        Require(PlatformAdminEmail, nameof(PlatformAdminEmail));
        Password(PlatformAdminPassword, nameof(PlatformAdminPassword));
        Password(AdminPassword, nameof(AdminPassword));
        return errors;

        void Require(string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"Demo__{name} is not set.");
            }
        }

        void Password(string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"Demo__{name} is not set.");
            }
            else if (value.Length < MinPasswordLength)
            {
                errors.Add($"Demo__{name} must be at least {MinPasswordLength} characters long.");
            }
        }
    }
}

/// <summary>Command-line options of <c>seed</c>.</summary>
public sealed record SeedArguments(string? DatasetDirectory, bool SkipActivity, bool ForceImages)
{
    public const string Usage = """
        Usage: Catalog.DemoSeeder seed [--dataset <dir>] [--skip-activity] [--force-images]

          --dataset <dir>   Demo dataset folder (default: the first 'demo' folder found walking up from here).
          --skip-activity   Do not rewrite the Demo activity (Inquiries and Product views).
          --force-images    Upload every product Image and Logo again.
        """;

    /// <summary>Parses the arguments; null when they are not valid.</summary>
    public static SeedArguments? Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0 || args[0] != "seed")
        {
            return null;
        }

        string? dataset = null;
        bool skipActivity = false, forceImages = false;
        for (var i = 1; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--dataset" when i + 1 < args.Count:
                    dataset = args[++i];
                    break;
                case "--skip-activity":
                    skipActivity = true;
                    break;
                case "--force-images":
                    forceImages = true;
                    break;
                default:
                    return null;
            }
        }

        return new SeedArguments(dataset, skipActivity, forceImages);
    }
}
