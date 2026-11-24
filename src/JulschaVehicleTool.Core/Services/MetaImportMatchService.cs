using JulschaVehicleTool.Core.Models;

namespace JulschaVehicleTool.Core.Services;

/// <summary>
/// Matches parsed meta file entries (keyed by modelName / handlingName) to existing vehicles.
/// Only <see cref="MatchConfidence.Exact"/> and <see cref="MatchConfidence.High"/> matches are meant
/// to be applied automatically; <see cref="MatchConfidence.Partial"/> matches are suggestions.
/// Every vehicle is matched at most once (best confidence wins, then file order).
/// </summary>
public class MetaImportMatchService
{
    public List<MatchResult<T>> AutoMatch<T>(Dictionary<string, T> parsedEntries, IList<Vehicle> vehicles)
    {
        var results = parsedEntries
            .Select(kv => new MatchResult<T> { ParsedName = kv.Key, Data = kv.Value })
            .ToList();

        var taken = new HashSet<Vehicle>();

        // Resolve in rounds of decreasing confidence so an exact match is never
        // stolen by an earlier fuzzy one.
        foreach (var level in new[] { MatchConfidence.Exact, MatchConfidence.High, MatchConfidence.Partial })
        {
            foreach (var result in results.Where(r => r.MatchedVehicle == null))
            {
                var candidates = vehicles.Where(v => !taken.Contains(v) && Rate(result.ParsedName, v) == level).ToList();

                // Ambiguous partial matches (police → police1 and police2) are not suggested at all
                if (level == MatchConfidence.Partial && candidates.Count != 1) continue;

                var vehicle = candidates.FirstOrDefault();
                if (vehicle == null) continue;

                result.MatchedVehicle = vehicle;
                result.Confidence = level;
                taken.Add(vehicle);
            }
        }

        foreach (var r in results.Where(r => r.MatchedVehicle == null))
            r.Confidence = MatchConfidence.None;

        return results;
    }

    private static MatchConfidence Rate(string parsedName, Vehicle v)
    {
        if (v.Name.Equals(parsedName, StringComparison.OrdinalIgnoreCase))
            return MatchConfidence.Exact;

        if (Eq(v.VehicleMeta?.ModelName, parsedName) || Eq(v.VehicleMeta?.HandlingId, parsedName)
            || Eq(v.Handling?.HandlingName, parsedName) || Eq(v.CarVariation?.ModelName, parsedName))
            return MatchConfidence.High;

        // Partial only on a word boundary: "police1" ~ "police1_lspd", but not "police1" ~ "police10"
        if (IsBoundaryPrefix(parsedName, v.Name) || IsBoundaryPrefix(v.Name, parsedName))
            return MatchConfidence.Partial;

        return MatchConfidence.None;
    }

    private static bool Eq(string? a, string b) =>
        !string.IsNullOrEmpty(a) && a.Equals(b, StringComparison.OrdinalIgnoreCase);

    private static bool IsBoundaryPrefix(string prefix, string full) =>
        full.Length > prefix.Length
        && full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
        && full[prefix.Length] is '_' or '-' or '+';
}

public class MatchResult<T>
{
    public string ParsedName { get; set; } = "";
    public T Data { get; set; } = default!;
    public Vehicle? MatchedVehicle { get; set; }
    public MatchConfidence Confidence { get; set; }

    /// <summary>True if the match is safe to apply without asking the user.</summary>
    public bool IsAutoApplicable => MatchedVehicle != null && Confidence is MatchConfidence.Exact or MatchConfidence.High;
}

public enum MatchConfidence
{
    Exact,
    High,
    Partial,
    None
}
