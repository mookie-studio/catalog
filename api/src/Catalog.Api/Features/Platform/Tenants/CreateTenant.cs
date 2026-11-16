using Catalog.Api.Data;
using Catalog.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Features.Platform.Tenants;

public static class CreateTenant
{
    public sealed record Request(string? Name, string? Slug);

    public static RouteGroupBuilder MapCreateTenant(this RouteGroupBuilder platform)
    {
        platform.MapPost("/tenants", Handle).WithName("CreateTenant");
        return platform;
    }

    private static async Task<Results<Created<TenantResponse>, ValidationProblem, Conflict<ProblemDetails>>> Handle(
        Request request, CatalogDbContext db, CancellationToken ct)
    {
        var errors = new ValidationErrors();
        TenantRules.ValidateName(request.Name, errors);
        TenantRules.ValidateSlug(request.Slug, errors);
        if (errors.HasErrors)
        {
            return TypedResults.ValidationProblem(errors.ToDictionary());
        }

        if (await db.Tenants.AnyAsync(t => t.Slug == request.Slug, ct))
        {
            return SlugInUse();
        }

        var tenant = new Tenant { Name = request.Name!.Trim(), Slug = request.Slug! };
        db.Tenants.Add(tenant);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.IsUniqueViolation())
        {
            return SlugInUse();
        }

        return TypedResults.Created($"/api/platform/tenants/{tenant.Id}", TenantResponse.From(tenant));
    }

    private static Conflict<ProblemDetails> SlugInUse() =>
        TypedResults.Conflict(ProblemDetailsBody.Conflict("The Tenant slug is already in use."));
}
