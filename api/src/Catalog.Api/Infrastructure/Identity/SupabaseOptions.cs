namespace Catalog.Api.Infrastructure.Identity;

/// <summary>Supabase settings, bound from the "Supabase" section. The service role key never reaches the browser.</summary>
public sealed class SupabaseOptions
{
    public const string SectionName = "Supabase";

    public string? Url { get; set; }
    public string? ServiceRoleKey { get; set; }
    public string? InviteRedirectUrl { get; set; }

    // Keeps the service role key out of logs and debugger displays.
    public override string ToString() => $"SupabaseOptions {{ Url = {Url}, InviteRedirectUrl = {InviteRedirectUrl} }}";
}
