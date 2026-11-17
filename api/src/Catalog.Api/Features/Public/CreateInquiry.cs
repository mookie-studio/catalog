using Catalog.Api.Data;
using Catalog.Api.Features.Admin.Inquiries;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Public;

/// <summary>
/// A Visitor sends an Inquiry about a Published product. <c>website</c> is a honeypot: humans never fill it,
/// so a request with it is answered like a valid one and discarded.
/// </summary>
public static class CreateInquiry
{
    public const string RateLimitPolicy = "inquiries";

    public sealed record Request(string? Name, string? Email, string? Message, string? Website);

    public static RouteGroupBuilder MapCreateInquiry(this RouteGroupBuilder publicGroup)
    {
        publicGroup.MapPost("/products/{productSlug}/inquiries", Handle)
            .WithName("CreateInquiry")
            .RequireRateLimiting(RateLimitPolicy);
        return publicGroup;
    }

    private static async Task<Results<Accepted, ValidationProblem, NotFound>> Handle(
        string productSlug, Request request, ICurrentTenant currentTenant, CatalogDbContext db, CancellationToken ct)
    {
        var enabled = await db.Tenants.Where(t => t.Id == currentTenant.TenantId).Select(t => t.InquiriesEnabled).SingleAsync(ct);
        var productId = await db.Products
            .Where(p => p.Slug == productSlug && p.Status == ProductStatus.Published)
            .Select(p => (Guid?)p.Id)
            .SingleOrDefaultAsync(ct);
        if (!enabled || productId is null)
        {
            return TypedResults.NotFound();
        }

        if (!string.IsNullOrEmpty(request.Website))
        {
            return TypedResults.Accepted((string?)null);
        }

        var name = request.Name?.Trim();
        var email = request.Email?.Trim();
        var message = request.Message?.Trim();
        var errors = new ValidationErrors();
        Required(name, Inquiry.NameMaxLength, "name", "Your name", errors);
        Required(message, Inquiry.MessageMaxLength, "message", "The message", errors);
        if (string.IsNullOrEmpty(email))
        {
            errors.Add("email", "Your email is required.");
        }
        else if (!InquiryRules.IsValidEmail(email))
        {
            errors.Add("email", "Your email is not a valid email address.");
        }

        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        db.Inquiries.Add(new Inquiry { ProductId = productId.Value, Name = name!, Email = email!, Message = message! });
        await db.SaveChangesAsync(ct);
        return TypedResults.Accepted((string?)null);
    }

    private static void Required(string? value, int max, string field, string label, ValidationErrors errors)
    {
        if (string.IsNullOrEmpty(value))
        {
            errors.Add(field, $"{label} is required.");
        }
        else if (value.Length > max)
        {
            errors.Add(field, $"{label} must be at most {max} characters.");
        }
    }
}
