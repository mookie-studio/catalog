using Catalog.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Inquiries;

/// <summary>Deletes an Inquiry, so the tenant keeps no personal data it no longer needs.</summary>
public static class DeleteInquiry
{
    public static RouteGroupBuilder MapDeleteInquiry(this RouteGroupBuilder admin)
    {
        admin.MapDelete("/inquiries/{id:guid}", Handle).WithName("DeleteInquiry");
        return admin;
    }

    private static async Task<Results<NoContent, NotFound>> Handle(Guid id, CatalogDbContext db, CancellationToken ct)
    {
        var deleted = await db.Inquiries.Where(i => i.Id == id).ExecuteDeleteAsync(ct);
        return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }
}
