namespace Catalog.DemoSeeder.Steps;

/// <summary>
/// Brings each dataset product's Images to the dataset. An Image cannot be compared with its file, so a
/// product keeps its Images when it has as many as the dataset lists (unless --force-images); otherwise all
/// are deleted and the files uploaded in order, the first being the Main image.
/// </summary>
public static class ImagesStep
{
    public static async Task RunAsync(SeedContext context, TenantState state, CancellationToken ct)
    {
        foreach (var wanted in state.Demo.Products)
        {
            var product = state.Products[wanted.Sku];
            if (product.Images.Count == wanted.ImageList.Count && !context.Arguments.ForceImages)
            {
                continue;
            }

            foreach (var image in product.Images)
            {
                await state.Admin.DeleteAsync($"/api/admin/products/{product.Id}/images/{image.Id}", ct);
                context.Log.Change(state.Slug, "image deleted", wanted.Sku);
            }

            foreach (var file in wanted.ImageList)
            {
                await state.Admin.UploadAsync<ProductImageResponse>(
                    HttpMethod.Post, $"/api/admin/products/{product.Id}/images", state.Demo.ImagePath(file), ct);
                context.Log.Change(state.Slug, "image uploaded", $"{wanted.Sku} {file}");
            }
        }
    }
}
