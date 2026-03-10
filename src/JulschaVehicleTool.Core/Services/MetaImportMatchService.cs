using JulschaVehicleTool.Core.Models;

namespace JulschaVehicleTool.Core.Services;

/// <summary>
/// Service for auto-matching parsed meta file entries to existing vehicles in a resource.
/// </summary>
public class MetaImportMatchService
{
    /// <summary>
    /// Auto-matches parsed entries (keyed by name from meta file) to vehicles in the resource.
    /// Returns a list of match results that can be presented to the user for confirmation.
    /// </summary>
    public List<MatchResult<T>> AutoMatch<T>(Dictionary<string, T> parsedEntries, IList<Vehicle> vehicles)
    {
        var results = new List<MatchResult<T>>();

        foreach (var (parsedName, data) in parsedEntries)
        {
            var match = FindBestMatch(parsedName, vehicles);
            results.Add(new MatchResult<T>
            {
                ParsedName = parsedName,
                Data = data,
                MatchedVehicle = match.vehicle,
                Confidence = match.confidence,
            });
        }

        return results;
    }

    private static (Vehicle? vehicle, MatchConfidence confidence) FindBestMatch(string parsedName, IList<Vehicle> vehicles)
    {
        // 1. Exact match on vehicle name (case-insensitive)
        var exact = vehicles.FirstOrDefault(v =>
            v.Name.Equals(parsedName, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
            return (exact, MatchConfidence.Exact);

        // 2. Handling name matches (handling names are often uppercase versions of vehicle names)
        var upperMatch = vehicles.FirstOrDefault(v =>
            v.Name.Equals(parsedName, StringComparison.OrdinalIgnoreCase) ||
            v.Handling?.HandlingName.Equals(parsedName, StringComparison.OrdinalIgnoreCase) == true);
        if (upperMatch != null)
            return (upperMatch, MatchConfidence.High);

        // 3. Contains match (parsed name is contained in vehicle name or vice versa)
        var containsMatch = vehicles.FirstOrDefault(v =>
            v.Name.Contains(parsedName, StringComparison.OrdinalIgnoreCase) ||
            parsedName.Contains(v.Name, StringComparison.OrdinalIgnoreCase));
        if (containsMatch != null)
            return (containsMatch, MatchConfidence.Partial);

        // 4. No match
        return (null, MatchConfidence.None);
    }
}

public class MatchResult<T>
{
    public string ParsedName { get; set; } = "";
    public T Data { get; set; } = default!;
    public Vehicle? MatchedVehicle { get; set; }
    public MatchConfidence Confidence { get; set; }
}

public enum MatchConfidence
{
    Exact,
    High,
    Partial,
    None
}
