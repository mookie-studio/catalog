namespace Catalog.Api.Data;

/// <summary>
/// How many times a Published product's public page was opened on one UTC day (ADR 0012). Nothing about the
/// Visitor is stored.
/// </summary>
public sealed class ProductViewCount : ITenantScoped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public Guid ProductId { get; init; }
    public DateOnly Day { get; init; }
    public int Count { get; set; }
}
