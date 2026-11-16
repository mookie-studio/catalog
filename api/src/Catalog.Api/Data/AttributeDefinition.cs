namespace Catalog.Api.Data;

public enum AttributeType
{
    Text,
    Number,
    Boolean,
    Date,
    OptionList,
}

/// <summary>An attribute declared by an Admin for the whole Tenant (ADR 0002). Its type cannot change.</summary>
public sealed class AttributeDefinition : ITenantScoped
{
    public const int NameMaxLength = 100;

    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public required string Name { get; set; }

    /// <summary>Lower-cased name, for case-insensitive uniqueness per Tenant.</summary>
    public string NormalizedName { get; private set; } = "";

    public required AttributeType Type { get; init; }
    public bool IsFilterable { get; set; }

    /// <summary>Only meaningful for OptionList: single-select (false) or multi-select (true).</summary>
    public bool AllowsMultiple { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = Timestamps.UtcNow();

    public List<AttributeOption> Options { get; init; } = [];

    public void Rename(string name)
    {
        Name = name;
        NormalizedName = Normalize(name);
    }

    public static string Normalize(string name) => name.Trim().ToLowerInvariant();
}

/// <summary>An allowed value of an option-list Attribute Definition.</summary>
public sealed class AttributeOption : ITenantScoped
{
    public const int ValueMaxLength = 100;

    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public Guid AttributeDefinitionId { get; init; }
    public required string Value { get; init; }
    public string NormalizedValue { get; init; } = "";
    public int Position { get; set; }
}
