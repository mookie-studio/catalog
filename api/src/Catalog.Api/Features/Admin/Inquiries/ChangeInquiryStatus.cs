using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Inquiries;

/// <summary>Marks an Inquiry as Handled or back to Open.</summary>
public static class ChangeInquiryStatus
{
    public sealed record Request(string? Status);

    public static RouteGroupBuilder MapChangeInquiryStatus(this RouteGroupBuilder admin)
    {
        admin.MapPut("/inquiries/{id:guid}/status", Handle).WithName("ChangeInquiryStatus");
        return admin;
    }

    private static async Task<Results<Ok<InquiryResponse>, ValidationProblem, NotFound>> Handle(
        Guid id, Request request, CatalogDbContext db, CancellationToken ct)
    {
        if (!Enum.TryParse<InquiryStatus>(request.Status, ignoreCase: false, out var status) || !Enum.IsDefined(status))
        {
            return TypedResults.ValidationProblem(
                new ValidationErrors().Add("status", "The status must be Open or Handled.").ToDictionary());
        }

        var inquiry = await db.Inquiries.SingleOrDefaultAsync(i => i.Id == id, ct);
        if (inquiry is null)
        {
            return TypedResults.NotFound();
        }

        inquiry.Status = status;
        await db.SaveChangesAsync(ct);
        var product = await db.Products.Where(p => p.Id == inquiry.ProductId)
            .Select(p => new InquiryProduct(p.Id, p.Name, p.Slug)).SingleAsync(ct);
        return TypedResults.Ok(new InquiryResponse(
            inquiry.Id, inquiry.Name, inquiry.Email, inquiry.Message, inquiry.Status, inquiry.CreatedAt, product));
    }
}
