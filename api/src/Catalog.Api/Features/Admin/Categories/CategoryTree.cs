using Catalog.Api.Data;
using Catalog.Api.Infrastructure;

namespace Catalog.Api.Features.Admin.Categories;

public sealed record CategoryNode(Guid Id, string Name, IReadOnlyList<CategoryNode> Children);

/// <summary>Helpers shared by the Category slices.</summary>
public static class CategoryTree
{
    /// <summary>Builds the nested tree, siblings ordered by Position.</summary>
    public static List<CategoryNode> Build(IReadOnlyCollection<Category> categories, Guid? parentId = null)
    {
        var byParent = categories.ToLookup(c => c.ParentId);
        return BuildLevel(byParent, parentId);
    }

    private static List<CategoryNode> BuildLevel(ILookup<Guid?, Category> byParent, Guid? parentId) =>
        byParent[parentId]
            .OrderBy(c => c.Position).ThenBy(c => c.Id)
            .Select(c => new CategoryNode(c.Id, c.Name, BuildLevel(byParent, c.Id)))
            .ToList();

    /// <summary>The ids of a Category and all of its descendants.</summary>
    public static HashSet<Guid> SubtreeIds(IReadOnlyCollection<Category> categories, Guid rootId)
    {
        var byParent = categories.ToLookup(c => c.ParentId);
        var result = new HashSet<Guid>();
        var pending = new Stack<Guid>([rootId]);
        while (pending.TryPop(out var id))
        {
            if (result.Add(id))
            {
                foreach (var child in byParent[id])
                {
                    pending.Push(child.Id);
                }
            }
        }

        return result;
    }

    /// <summary>Renumbers siblings 0..n-1 in their current order.</summary>
    public static void Compact(IEnumerable<Category> siblings)
    {
        var position = 0;
        foreach (var sibling in siblings.OrderBy(c => c.Position).ThenBy(c => c.Id))
        {
            sibling.Position = position++;
        }
    }

    public static void ValidateName(string? name, ValidationErrors errors)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("name", "The name is required.");
        }
        else if (name.Trim().Length > Category.NameMaxLength)
        {
            errors.Add("name", $"The name must be at most {Category.NameMaxLength} characters.");
        }
    }
}
