using JulschaVehicleTool.Core.Models;

namespace JulschaVehicleTool.Core.Services;

/// <summary>
/// Siren setting IDs. carvariations.meta stores sirenSettings as a byte, so valid IDs are 1–255
/// (0 means "no siren"). IDs must be unique across all resources running on the server.
/// </summary>
public static class SirenIds
{
    public const int Min = 1;
    public const int Max = 255;

    public static bool IsValid(int id) => id is >= Min and <= Max;

    /// <summary>
    /// Parses a list like "1-50, 99, 120-130" (also ';' or whitespace separated). Invalid parts are ignored.
    /// </summary>
    public static HashSet<int> ParseList(string? text)
    {
        var result = new HashSet<int>();
        if (string.IsNullOrWhiteSpace(text)) return result;

        foreach (var part in text.Split([',', ';', ' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries))
        {
            var range = part.Split('-', 2);
            if (range.Length == 2 && int.TryParse(range[0], out var from) && int.TryParse(range[1], out var to))
            {
                for (int i = Math.Max(Min, Math.Min(from, to)); i <= Math.Min(Max, Math.Max(from, to)); i++)
                    result.Add(i);
            }
            else if (int.TryParse(part, out var single) && IsValid(single))
            {
                result.Add(single);
            }
        }
        return result;
    }

    /// <summary>IDs that may not be used for new siren settings in this project.</summary>
    public static HashSet<int> Used(Project project, IEnumerable<int>? additional = null)
    {
        var used = ParseList(project.ReservedSirenIds);
        used.UnionWith(project.CarCols.SirenSettings.Select(s => s.Id));
        if (additional != null) used.UnionWith(additional);
        return used;
    }

    /// <summary>Lowest free ID in 1–255, or null if all are taken.</summary>
    public static int? NextFree(ISet<int> used)
    {
        for (int id = Min; id <= Max; id++)
            if (!used.Contains(id))
                return id;
        return null;
    }

    public static int? NextFree(Project project, IEnumerable<int>? additional = null)
        => NextFree(Used(project, additional));
}
