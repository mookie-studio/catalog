using Catalog.DemoSeeder.Dataset;

namespace Catalog.DemoSeeder.Steps;

/// <summary>
/// Brings a Demo tenant's Categories (matched by full path of names) and Attribute Definitions and Options
/// (matched by name and text) to the dataset: creates, moves, updates and deletes.
/// </summary>
public static class CatalogStep
{
    public static async Task RunAsync(SeedContext context, TenantState state, CancellationToken ct)
    {
        await CategoriesAsync(context, state, ct);
        await DefinitionsAsync(context, state, ct);
    }

    private static async Task CategoriesAsync(SeedContext context, TenantState state, CancellationToken ct)
    {
        var wanted = state.Demo.Catalog.CategoryList;
        var tree = await state.Admin.GetAsync<List<CategoryNode>>("/api/admin/categories", ct);

        // 1. Delete Categories whose path is not in the dataset (deepest first; deleting one drops its product links).
        var wantedPaths = state.Demo.Catalog.CategoryPaths().ToHashSet(StringComparer.Ordinal);
        foreach (var (node, path) in Flatten(tree, null).Reverse())
        {
            if (!wantedPaths.Contains(path))
            {
                await state.Admin.DeleteAsync($"/api/admin/categories/{node.Id}", ct);
                context.Log.Change(state.Slug, "category deleted", path);
            }
        }

        // 2. Create the missing ones, parents first.
        var ids = Flatten(tree, null).Where(x => wantedPaths.Contains(x.Path)).ToDictionary(x => x.Path, x => x.Node.Id, StringComparer.Ordinal);
        await CreateMissingAsync(wanted, null, null);

        // 3. Put every level in dataset order.
        tree = await state.Admin.GetAsync<List<CategoryNode>>("/api/admin/categories", ct);
        await OrderAsync(wanted, tree, null, null);

        foreach (var (path, id) in ids)
        {
            state.Categories[path] = id;
        }

        async Task CreateMissingAsync(IReadOnlyList<CategoryEntry> entries, string? parentPath, Guid? parentId)
        {
            foreach (var entry in entries)
            {
                var path = parentPath is null ? entry.Name : $"{parentPath}/{entry.Name}";
                if (!ids.TryGetValue(path, out var id))
                {
                    var created = await state.Admin.PostAsync<CategoryNode>("/api/admin/categories", new { name = entry.Name, parentId }, ct);
                    id = ids[path] = created.Id;
                    context.Log.Change(state.Slug, "category created", path);
                }

                await CreateMissingAsync(entry.ChildList, path, id);
            }
        }

        async Task OrderAsync(IReadOnlyList<CategoryEntry> entries, List<CategoryNode> nodes, string? parentPath, Guid? parentId)
        {
            var current = nodes.Select(n => n.Name).ToList();
            var order = entries.Select(e => e.Name).ToList();
            if (!current.SequenceEqual(order, StringComparer.Ordinal))
            {
                for (var i = 0; i < entries.Count; i++)
                {
                    var path = parentPath is null ? entries[i].Name : $"{parentPath}/{entries[i].Name}";
                    await state.Admin.PostAsync($"/api/admin/categories/{ids[path]}/move", new { parentId, position = i }, ct);
                }

                context.Log.Change(state.Slug, "categories reordered", parentPath ?? "(root)");
            }

            foreach (var entry in entries)
            {
                var path = parentPath is null ? entry.Name : $"{parentPath}/{entry.Name}";
                var node = nodes.FirstOrDefault(n => n.Id == ids[path]);
                await OrderAsync(entry.ChildList, node?.Children ?? [], path, ids[path]);
            }
        }
    }

    private static IEnumerable<(CategoryNode Node, string Path)> Flatten(IEnumerable<CategoryNode> nodes, string? parent)
    {
        foreach (var node in nodes)
        {
            var path = parent is null ? node.Name : $"{parent}/{node.Name}";
            yield return (node, path);
            foreach (var child in Flatten(node.Children, path))
            {
                yield return child;
            }
        }
    }

    private static async Task DefinitionsAsync(SeedContext context, TenantState state, CancellationToken ct)
    {
        var existing = await state.Admin.GetAllPagesAsync<DefinitionResponse>("/api/admin/attribute-definitions", ct);
        var wanted = state.Demo.Catalog.AttributeList;

        foreach (var definition in existing)
        {
            var match = wanted.FirstOrDefault(w => string.Equals(w.Name, definition.Name, StringComparison.OrdinalIgnoreCase));
            if (match is null || match.Type != definition.Type || match.AllowsMultiple != definition.AllowsMultiple)
            {
                // The type of a definition cannot change: delete it (and its values) and create it again.
                await state.Admin.DeleteAsync($"/api/admin/attribute-definitions/{definition.Id}?confirm=true", ct);
                context.Log.Change(state.Slug, "attribute deleted", definition.Name);
                continue;
            }

            state.Definitions[definition.Name] = definition;
        }

        foreach (var attribute in wanted)
        {
            if (!state.Definitions.TryGetValue(attribute.Name, out var definition))
            {
                definition = await state.Admin.PostAsync<DefinitionResponse>("/api/admin/attribute-definitions", new
                {
                    name = attribute.Name,
                    type = attribute.Type,
                    isFilterable = attribute.IsFilterable,
                    allowsMultiple = attribute.AllowsMultiple,
                }, ct);
                definition = definition with { Options = definition.Options ?? [] };
                context.Log.Change(state.Slug, "attribute created", attribute.Name);
            }
            else if (definition.IsFilterable != attribute.IsFilterable || definition.Name != attribute.Name)
            {
                definition = await state.Admin.PutAsync<DefinitionResponse>(
                    $"/api/admin/attribute-definitions/{definition.Id}", new { name = attribute.Name, isFilterable = attribute.IsFilterable }, ct);
                context.Log.Change(state.Slug, "attribute updated", attribute.Name);
            }

            var options = new List<OptionResponse>();
            foreach (var option in definition.Options)
            {
                if (attribute.OptionList.Contains(option.Value, StringComparer.Ordinal))
                {
                    options.Add(option);
                    continue;
                }

                await state.Admin.DeleteAsync($"/api/admin/attribute-definitions/{definition.Id}/options/{option.Id}?confirm=true", ct);
                context.Log.Change(state.Slug, "option deleted", $"{attribute.Name}: {option.Value}");
            }

            foreach (var value in attribute.OptionList.Where(v => options.All(o => o.Value != v)))
            {
                options.Add(await state.Admin.PostAsync<OptionResponse>(
                    $"/api/admin/attribute-definitions/{definition.Id}/options", new { value }, ct));
                context.Log.Change(state.Slug, "option created", $"{attribute.Name}: {value}");
            }

            state.Definitions[attribute.Name] = definition with { Options = options };
        }
    }
}
