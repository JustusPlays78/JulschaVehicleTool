using System.Text.RegularExpressions;
using JulschaVehicleTool.Core.Models;

namespace JulschaVehicleTool.Core.Services;

/// <summary>
/// Rules for vehicle (model) names and helpers to keep file names and meta names in sync
/// when a vehicle is renamed, cloned or imported under a different name.
/// </summary>
public static partial class VehicleNaming
{
    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex ValidName();

    public static bool IsValidName(string name, out string error)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Name must not be empty.";
            return false;
        }
        if (!ValidName().IsMatch(name))
        {
            error = $"'{name}' contains invalid characters — only letters, digits, '_' and '-' are allowed.";
            return false;
        }
        error = "";
        return true;
    }

    /// <summary>All vehicle names in the project (case-insensitive), optionally ignoring one vehicle.</summary>
    public static HashSet<string> UsedNames(Project project, Vehicle? except = null) =>
        new(project.Resources.SelectMany(r => r.Vehicles).Where(v => v != except).Select(v => v.Name),
            StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns <paramref name="baseName"/> + suffix (+ _2, _3, ...) that is not used anywhere in the project.</summary>
    public static string UniqueName(Project project, string baseName, string suffix = "")
        => UniqueName(UsedNames(project), baseName, suffix);

    public static string UniqueName(ISet<string> used, string baseName, string suffix = "")
    {
        var candidate = baseName + suffix;
        if (!used.Contains(candidate)) return candidate;
        int n = 2;
        while (used.Contains($"{baseName}{suffix}_{n}")) n++;
        return $"{baseName}{suffix}_{n}";
    }

    /// <summary>
    /// Maps a stream file name to the new vehicle name if it belongs to the vehicle:
    /// police1.yft → police2.yft, police1_hi.yft → police2_hi.yft, police1+hi.ytd → police2+hi.ytd,
    /// police1_bumf.yft → police2_bumf.yft. Other names are returned unchanged.
    /// </summary>
    public static string RenameFileName(string fileName, string oldName, string newName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);

        if (stem.Equals(oldName, StringComparison.OrdinalIgnoreCase))
            return newName + ext;

        if (stem.Length > oldName.Length
            && stem.StartsWith(oldName, StringComparison.OrdinalIgnoreCase)
            && stem[oldName.Length] is '_' or '+')
            return newName + stem[oldName.Length..] + ext;

        return fileName;
    }

    /// <summary>
    /// Updates all meta names that refer to the old vehicle name (modelName, txdName, handling name,
    /// carvariations modelName, txd relationships, gameName label). Values that were customised to
    /// something else are left alone. Handling names keep their style (POLICE1 → POLICE2).
    /// </summary>
    public static void ApplyNameToMetas(Vehicle vehicle, string oldName, string newName)
    {
        string Map(string value) =>
            value.Equals(oldName, StringComparison.Ordinal) ? newName
            : value.Equals(oldName.ToUpperInvariant(), StringComparison.Ordinal) ? newName.ToUpperInvariant()
            : value.Equals(oldName, StringComparison.OrdinalIgnoreCase) ? newName
            : value;

        if (vehicle.VehicleMeta is { } vm)
        {
            vm.ModelName = Map(vm.ModelName);
            vm.TxdName = Map(vm.TxdName);
            vm.HandlingId = Map(vm.HandlingId);
            vm.GameName = Map(vm.GameName);
            foreach (var rel in vm.TxdRelationships)
            {
                rel.Child = Map(rel.Child);
                rel.Parent = Map(rel.Parent);
            }
        }

        if (vehicle.Handling is { } h)
            h.HandlingName = Map(h.HandlingName);

        if (vehicle.CarVariation is { } cv)
            cv.ModelName = Map(cv.ModelName);
    }
}
