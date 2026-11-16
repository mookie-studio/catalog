namespace Catalog.Api.Data;

/// <summary>
/// An isolated business inside the platform (see GLOSSARY.md).
/// </summary>
public sealed class Tenant
{
    public const int NameMaxLength = 100;
    public const int SlugMaxLength = 50;

    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; init; } = Timestamps.UtcNow();
}
