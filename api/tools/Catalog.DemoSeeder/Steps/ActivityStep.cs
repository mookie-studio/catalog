using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace Catalog.DemoSeeder.Steps;

/// <summary>
/// Rewrites a Demo tenant's Demo activity straight in the database (ADR 0014), in one transaction filtered by
/// that Tenant's id: deletes its Inquiries and Product views, then inserts the dataset Inquiries (back-dated)
/// and a deterministic number of views per day for every Published product (Featured products ×3).
/// </summary>
public static class ActivityStep
{
    public const int FeaturedFactor = 3;

    public static async Task RunAsync(SeedContext context, TenantState state, CancellationToken ct)
    {
        var activity = state.Demo.Activity;
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        await using var connection = new NpgsqlConnection(context.Options.DatabaseConnectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        foreach (var table in new[] { "Inquiries", "ProductViewCounts" })
        {
            await using var delete = new NpgsqlCommand($"""DELETE FROM "{table}" WHERE "TenantId" = @tenant""", connection, transaction);
            delete.Parameters.AddWithValue("tenant", state.Id);
            await delete.ExecuteNonQueryAsync(ct);
        }

        foreach (var inquiry in activity.InquiryList)
        {
            await using var insert = new NpgsqlCommand("""
                INSERT INTO "Inquiries" ("Id", "TenantId", "ProductId", "Name", "Email", "Message", "Status", "CreatedAt")
                VALUES (@id, @tenant, @product, @name, @email, @message, @status, @createdAt)
                """, connection, transaction);
            insert.Parameters.AddWithValue("id", Guid.CreateVersion7());
            insert.Parameters.AddWithValue("tenant", state.Id);
            insert.Parameters.AddWithValue("product", state.Products[inquiry.Sku].Id);
            insert.Parameters.AddWithValue("name", inquiry.Name);
            insert.Parameters.AddWithValue("email", inquiry.Email);
            insert.Parameters.AddWithValue("message", inquiry.Message);
            insert.Parameters.AddWithValue("status", inquiry.Status);
            // Spread over the day so the inbox order is stable and looks natural.
            insert.Parameters.AddWithValue("createdAt", now.AddDays(-inquiry.DaysAgo).AddMinutes(-StableNumber(inquiry.Email + inquiry.Sku, 0, 600)));
            await insert.ExecuteNonQueryAsync(ct);
        }

        var views = 0;
        if (activity.Views is { } plan)
        {
            foreach (var product in state.Demo.Products.Where(p => p.Status == "Published"))
            {
                for (var daysAgo = 0; daysAgo < plan.Days; daysAgo++)
                {
                    var day = today.AddDays(-daysAgo);
                    var count = StableNumber($"{product.Sku}|{day:yyyy-MM-dd}", plan.MinPerDay, plan.MaxPerDay) * (product.Featured ? FeaturedFactor : 1);
                    if (count == 0)
                    {
                        continue;
                    }

                    await using var insert = new NpgsqlCommand("""
                        INSERT INTO "ProductViewCounts" ("Id", "TenantId", "ProductId", "Day", "Count")
                        VALUES (@id, @tenant, @product, @day, @count)
                        """, connection, transaction);
                    insert.Parameters.AddWithValue("id", Guid.CreateVersion7());
                    insert.Parameters.AddWithValue("tenant", state.Id);
                    insert.Parameters.AddWithValue("product", state.Products[product.Sku].Id);
                    insert.Parameters.AddWithValue("day", day);
                    insert.Parameters.AddWithValue("count", count);
                    await insert.ExecuteNonQueryAsync(ct);
                    views += count;
                }
            }
        }

        await transaction.CommitAsync(ct);
        context.Log.Info($"  {state.Slug}: Demo activity written: {activity.InquiryList.Count} Inquiries, {views} Product views.");
    }

    /// <summary>A number in [min, max] that depends only on <paramref name="key"/> (same on every run and machine).</summary>
    public static int StableNumber(string key, int min, int max)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        var value = BitConverter.ToUInt32(hash, 0);
        return min + (int)(value % (uint)(max - min + 1));
    }
}
