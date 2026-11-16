namespace Catalog.Api.Data;

public static class Timestamps
{
    /// <summary>Current UTC time truncated to PostgreSQL's microsecond precision, so responses match stored values.</summary>
    public static DateTimeOffset UtcNow()
    {
        var now = DateTimeOffset.UtcNow;
        return now.AddTicks(-(now.Ticks % 10));
    }
}
