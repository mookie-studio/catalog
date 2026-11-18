namespace Catalog.DemoSeeder;

/// <summary>
/// What a run changed, per Demo tenant. A second run on an unchanged environment reports "Total changes: 0"
/// (Demo activity is rewritten on every run and is reported apart, not as a change).
/// </summary>
public sealed class SeedLog(TextWriter output)
{
    private readonly SortedDictionary<string, SortedDictionary<string, int>> _changes = new(StringComparer.Ordinal);

    public int TotalChanges => _changes.Values.Sum(c => c.Values.Sum());

    public void Info(string message) => output.WriteLine(message);

    /// <summary>Records one change, for example ("forja-hardware", "products created").</summary>
    public void Change(string scope, string kind, string detail)
    {
        output.WriteLine($"  {scope}: {kind}: {detail}");
        var counts = _changes.TryGetValue(scope, out var existing) ? existing : _changes[scope] = new(StringComparer.Ordinal);
        counts[kind] = counts.GetValueOrDefault(kind) + 1;
    }

    public void Summary()
    {
        output.WriteLine();
        foreach (var (scope, counts) in _changes)
        {
            output.WriteLine($"{scope}: {string.Join(", ", counts.Select(c => $"{c.Key} {c.Value}"))}");
        }

        output.WriteLine($"Total changes: {TotalChanges}");
    }
}
