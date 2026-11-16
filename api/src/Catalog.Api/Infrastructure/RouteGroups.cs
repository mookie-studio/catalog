using Catalog.Api.Features.Admin;
using Catalog.Api.Features.Admin.Admins;
using Catalog.Api.Features.Admin.Attributes;
using Catalog.Api.Features.Admin.Products;
using Catalog.Api.Features.Platform.Admins;
using Catalog.Api.Features.Platform.Tenants;
using Catalog.Api.Infrastructure.Auth;

namespace Catalog.Api.Infrastructure;

/// <summary>
/// The three route groups of the API (ADR 0007). Slices map themselves onto these groups.
/// </summary>
public static class RouteGroups
{
    public const string Public = "/api/public/{tenantSlug}";
    public const string Admin = "/api/admin";
    public const string Platform = "/api/platform";

    public static IEndpointRouteBuilder MapRouteGroups(this IEndpointRouteBuilder app)
    {
        app.MapGroup(Public).WithTags("Public");

        app.MapGroup(Admin).WithTags("Admin")
            .RequireAuthorization(AuthServiceCollectionExtensions.AdminPolicy)
            .MapGetCurrentTenant()
            .MapListAdmins()
            .MapInviteAdmin()
            .MapRemoveAdmin()
            .MapCreateProduct()
            .MapGetProduct()
            .MapListProducts()
            .MapUpdateProduct()
            .MapChangeProductStatus()
            .MapSetProductAttributes()
            .MapCreateAttributeDefinition()
            .MapListAttributeDefinitions()
            .MapUpdateAttributeDefinition()
            .MapAddAttributeOption()
            .MapDeleteAttributeDefinition();

        app.MapGroup(Platform).WithTags("Platform")
            .RequireAuthorization(AuthServiceCollectionExtensions.PlatformAdminPolicy)
            .MapCreateTenant()
            .MapListTenants()
            .MapChangeTenantStatus()
            .MapChangeTenantSlug()
            .MapInviteFirstAdmin()
            .MapListTenantAdmins();

        return app;
    }
}
