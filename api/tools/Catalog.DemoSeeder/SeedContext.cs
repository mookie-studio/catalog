using Catalog.DemoSeeder.Dataset;

namespace Catalog.DemoSeeder;

/// <summary>Everything a run shares between steps.</summary>
public sealed class SeedContext(
    DemoOptions options, SeedArguments arguments, DemoDataset dataset, IDemoIdentity identity,
    Func<HttpClient> apiHttp, SeedLog log)
{
    public DemoOptions Options { get; } = options;
    public SeedArguments Arguments { get; } = arguments;
    public DemoDataset Dataset { get; } = dataset;
    public IDemoIdentity Identity { get; } = identity;
    public SeedLog Log { get; } = log;

    public ApiClient Platform { get; set; } = null!;

    public Guid PlatformAdminUserId { get; set; }

    /// <summary>The state of each Demo tenant, in dataset order.</summary>
    public List<TenantState> Tenants { get; } = [];

    public ApiClient Api(string accessToken) => new(apiHttp(), accessToken);
}

/// <summary>One Demo tenant during a run.</summary>
public sealed class TenantState(DemoTenant demo)
{
    public DemoTenant Demo { get; } = demo;
    public string Slug => Demo.Slug;

    public Guid AdminUserId { get; set; }
    public TenantResponse? Existing { get; set; }
    public Guid Id { get; set; }
    public ApiClient Admin { get; set; } = null!;

    /// <summary>Category id by full path ("Root/Child").</summary>
    public Dictionary<string, Guid> Categories { get; } = new(StringComparer.Ordinal);

    /// <summary>Attribute Definition by name.</summary>
    public Dictionary<string, DefinitionResponse> Definitions { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Each dataset product as the API last returned it, by SKU.</summary>
    public Dictionary<string, ProductResponse> Products { get; } = new(StringComparer.OrdinalIgnoreCase);
}
