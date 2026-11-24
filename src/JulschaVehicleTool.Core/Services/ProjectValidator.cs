using JulschaVehicleTool.Core.Models;
using static JulschaVehicleTool.Core.Services.VehicleValidator;

namespace JulschaVehicleTool.Core.Services;

/// <summary>
/// Checks that span several vehicles: duplicate names (which would overwrite each other in game),
/// stream file collisions, siren ID range/references and model/file name mismatches.
/// </summary>
public static class ProjectValidator
{
    public record VehicleIssue(Vehicle? Vehicle, ValidationIssue Issue)
    {
        public override string ToString() =>
            Vehicle != null ? $"[{Vehicle.Name}] {Issue.Severity}: {Issue.Message}" : $"{Issue.Severity}: {Issue.Message}";
    }

    /// <summary>Validates the vehicles of <paramref name="resource"/> against each other and the whole project.</summary>
    public static List<VehicleIssue> Validate(Project project, Resource resource)
    {
        var issues = new List<VehicleIssue>();
        var all = project.Resources.SelectMany(r => r.Vehicles.Select(v => (r, v))).ToList();

        foreach (var v in resource.Vehicles)
        {
            foreach (var i in VehicleValidator.Validate(v))
                issues.Add(new(v, i));

            // ---- duplicates across the project (all resources run on the same server) ----
            void Dup(string field, Func<Vehicle, string?> get, string severity, string what)
            {
                var value = get(v);
                if (string.IsNullOrEmpty(value)) return;
                var others = all.Where(x => x.v != v && value.Equals(get(x.v), StringComparison.OrdinalIgnoreCase))
                                .Select(x => x.r == resource ? x.v.Name : $"{x.v.Name} ({x.r.Name})").ToList();
                if (others.Count > 0)
                    issues.Add(new(v, new(severity, field, $"{what} '{value}' is also used by {string.Join(", ", others)}")));
            }

            Dup("ModelName", x => x.VehicleMeta?.ModelName, "Error", "modelName");
            Dup("HandlingName", x => x.Handling?.HandlingName, "Warning", "handlingName");
            Dup("TxdName", x => x.VehicleMeta?.TxdName, "Warning", "txdName");

            // ---- model name must match the .yft file name, otherwise the vehicle won't spawn ----
            var model = v.VehicleMeta?.ModelName;
            if (!string.IsNullOrEmpty(model) && v.YftRelativePath != null
                && !Path.GetFileNameWithoutExtension(v.YftRelativePath).Equals(model, StringComparison.OrdinalIgnoreCase))
                issues.Add(new(v, new("Error", "ModelName",
                    $"modelName '{model}' does not match the model file '{Path.GetFileName(v.YftRelativePath)}' — the vehicle won't spawn")));

            if (v.YftRelativePath == null)
                issues.Add(new(v, new("Warning", "Files", "No model file (.yft) imported")));

            // ---- sirens ----
            var siren = v.CarVariation?.SirenSettings ?? 0;
            if (siren != 0)
            {
                if (!SirenIds.IsValid(siren))
                    issues.Add(new(v, new("Error", "SirenSettings", $"Siren setting {siren} is outside 1–255")));
                else if (project.CarCols.SirenSettings.All(s => s.Id != siren))
                    issues.Add(new(v, new("Warning", "SirenSettings", $"Siren setting {siren} does not exist in the project siren pool")));
            }
        }

        // ---- stream file collisions inside the resource (same file name from different vehicles) ----
        var byFile = resource.Vehicles
            .SelectMany(v => v.AllStreamFiles.Select(f => (file: Path.GetFileName(f), v)))
            .GroupBy(x => x.file, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Select(x => x.v).Distinct().Count() > 1);
        foreach (var g in byFile)
            issues.Add(new(null, new("Error", "Files",
                $"Stream file '{g.Key}' is used by {string.Join(", ", g.Select(x => x.v.Name).Distinct())} — they would overwrite each other")));

        // ---- siren pool ----
        var reserved = SirenIds.ParseList(project.ReservedSirenIds);
        var usedByResource = resource.Vehicles.Select(v => v.CarVariation?.SirenSettings ?? 0).ToHashSet();
        foreach (var s in project.CarCols.SirenSettings.Where(s => usedByResource.Contains(s.Id)))
        {
            if (!SirenIds.IsValid(s.Id))
                issues.Add(new(null, new("Error", "Sirens", $"Siren group '{s.Name}' has ID {s.Id} — must be 1–255")));
            else if (reserved.Contains(s.Id))
                issues.Add(new(null, new("Error", "Sirens", $"Siren group '{s.Name}' uses ID {s.Id}, which is reserved on the server")));
        }

        return issues;
    }

    public static bool HasErrors(IEnumerable<VehicleIssue> issues) => issues.Any(i => i.Issue.Severity == "Error");
}
