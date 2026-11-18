namespace Catalog.DemoSeeder.Steps;

/// <summary>Brings each Demo tenant's Tenant profile, Inquiries switch and Logo to the dataset.</summary>
public static class ProfileStep
{
    public static async Task RunAsync(SeedContext context, TenantState state, CancellationToken ct)
    {
        if (state.Demo.Profile is not { } wanted)
        {
            return;
        }

        var current = await state.Admin.GetAsync<TenantProfileResponse>("/api/admin/tenant/profile", ct);
        var differs = current.Description != wanted.Description
            || current.ContactEmail != wanted.ContactEmail
            || current.Phone != wanted.Phone
            || current.Address != wanted.Address
            || current.WebsiteUrl != wanted.WebsiteUrl
            || !string.Equals(current.BrandColor, wanted.BrandColor, StringComparison.OrdinalIgnoreCase)
            || current.InquiriesEnabled != wanted.InquiriesEnabled;
        if (differs)
        {
            await state.Admin.PutAsync("/api/admin/tenant/profile", new
            {
                description = wanted.Description,
                contactEmail = wanted.ContactEmail,
                phone = wanted.Phone,
                address = wanted.Address,
                websiteUrl = wanted.WebsiteUrl,
                brandColor = wanted.BrandColor,
                inquiriesEnabled = wanted.InquiriesEnabled,
            }, ct);
            context.Log.Change(state.Slug, "profile updated", "Tenant profile");
        }

        if (wanted.Logo is { } logo)
        {
            // A Logo cannot be compared with the file, so it is uploaded when missing or when asked to.
            if (current.LogoUrl is null || context.Arguments.ForceImages)
            {
                await state.Admin.UploadAsync<object>(HttpMethod.Put, "/api/admin/tenant/logo", state.Demo.ImagePath(logo), ct);
                context.Log.Change(state.Slug, "logo uploaded", logo);
            }
        }
        else if (current.LogoUrl is not null)
        {
            await state.Admin.DeleteAsync("/api/admin/tenant/logo", ct);
            context.Log.Change(state.Slug, "logo deleted", "Logo");
        }
    }
}
