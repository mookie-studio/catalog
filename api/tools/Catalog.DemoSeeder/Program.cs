using Catalog.DemoSeeder.Dataset;
using Microsoft.Extensions.Configuration;

namespace Catalog.DemoSeeder;

/// <summary>The Demo seeder (ADR 0014). See docs/demo.md.</summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var arguments = SeedArguments.Parse(args);
        if (arguments is null)
        {
            Console.Error.WriteLine(SeedArguments.Usage);
            return 1;
        }

        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(Seeder).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();
        var options = configuration.GetSection(DemoOptions.SectionName).Get<DemoOptions>() ?? new DemoOptions();

        var problems = options.Validate();
        var root = arguments.DatasetDirectory ?? DatasetLoader.FindDefault(Directory.GetCurrentDirectory());
        if (root is null || !Directory.Exists(root))
        {
            problems.Add(root is null
                ? "No 'demo' folder with a tenants.json was found walking up from the current directory; pass --dataset <dir>."
                : $"The dataset folder {root} does not exist.");
        }

        if (problems.Count > 0)
        {
            Console.Error.WriteLine("The Demo seeder cannot start:");
            problems.ForEach(p => Console.Error.WriteLine("  - " + p));
            return 1;
        }

        try
        {
            var dataset = DatasetLoader.Load(root!);
            Console.WriteLine($"Demo dataset {root}: {dataset.Tenants.Count} Demo tenants, {dataset.Tenants.Sum(t => t.Products.Count)} products.");

            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
            var identity = new SupabaseDemoIdentity(http, options.SupabaseUrl!, options.SupabaseServiceRoleKey!, options.SupabaseAnonKey!);
            var apiBase = new Uri(options.ApiUrl!.TrimEnd('/') + "/");
            var context = new SeedContext(options, arguments, dataset, identity,
                () => new HttpClient { BaseAddress = apiBase, Timeout = TimeSpan.FromMinutes(2) }, new SeedLog(Console.Out));
            await Seeder.RunAsync(context, CancellationToken.None);
            return 0;
        }
        catch (Exception e) when (e is DatasetException or SeedException or ApiException or InvalidOperationException or HttpRequestException)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }
    }
}
