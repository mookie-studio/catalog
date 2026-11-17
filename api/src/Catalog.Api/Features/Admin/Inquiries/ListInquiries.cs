using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Admin.Inquiries;

/// <summary>The tenant's Inquiries, newest first, optionally only Open or Handled ones.</summary>
public static class ListInquiries
{
    public static RouteGroupBuilder MapListInquiries(this RouteGroupBuilder admin)
    {
        admin.MapGet("/inquiries", Handle).WithName("ListInquiries");
        return admin;
    }

    private static async Task<Ok<PagedResponse<InquiryResponse>>> Handle(
        int? page, int? pageSize, InquiryStatus? status, CatalogDbContext db, CancellationToken ct)
    {
        var paging = Paging.From(page, pageSize);
        var query = db.Inquiries.AsQueryable();
        if (status is { } s)
        {
            query = query.Where(i => i.Status == s);
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(i => i.CreatedAt).ThenByDescending(i => i.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Join(db.Products, i => i.ProductId, p => p.Id, (i, p) => new InquiryResponse(
                i.Id, i.Name, i.Email, i.Message, i.Status, i.CreatedAt, new InquiryProduct(p.Id, p.Name, p.Slug)))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResponse<InquiryResponse>(items, paging.Page, paging.PageSize, totalCount));
    }
}
