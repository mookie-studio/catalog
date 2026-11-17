namespace Catalog.Api.Data;

/// <summary>Open (new) or Handled (an Admin dealt with it). Either direction is allowed.</summary>
public enum InquiryStatus
{
    Open,
    Handled,
}

/// <summary>A message a Visitor sends to a Tenant about one Published product (see GLOSSARY.md).</summary>
public sealed class Inquiry : ITenantScoped
{
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 254;
    public const int MessageMaxLength = 2000;

    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public Guid ProductId { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string Message { get; init; }
    public InquiryStatus Status { get; set; } = InquiryStatus.Open;
    public DateTimeOffset CreatedAt { get; init; } = Timestamps.UtcNow();
}
