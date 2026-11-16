using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Platform.Tenants;

public static class ChangeTenantSlug
{
    public sealed record Request(string? Slug);

    public static RouteGroupBuilder MapChangeTenantSlug(this RouteGroupBuilder platform)
    {
        platform.MapPut("/tenants/{id:guid}/slug", Handle).WithName("ChangeTenantSlug");
        return platform;
    }

    private static async Task<Results<Ok<TenantResponse>, ValidationProblem, NotFound, Conflict<ProblemDetails>>> Handle(
        Guid id, Request request, CatalogDbContext db, CancellationToken ct)
    {
        var errors = new ValidationErrors();
        TenantRules.ValidateSlug(request.Slug, errors);
        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        var tenant = await db.Tenants.FindAsync([id], ct);
        if (tenant is null)
        {
            return TypedResults.NotFound();
        }

        if (tenant.Slug == request.Slug)
        {
            return TypedResults.Ok(TenantResponse.From(tenant));
        }

        if (await db.Tenants.AnyAsync(t => t.Slug == request.Slug && t.Id != id, ct))
        {
            return SlugInUse();
        }

        tenant.Slug = request.Slug!;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.IsUniqueViolation())
        {
            return SlugInUse();
        }

        return TypedResults.Ok(TenantResponse.From(tenant));
    }

    private static Conflict<ProblemDetails> SlugInUse() =>
        TypedResults.Conflict(ProblemDetailsBody.Conflict("The Tenant slug is already in use."));
}
