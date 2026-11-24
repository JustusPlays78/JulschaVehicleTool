using JulschaVehicleTool.Core.Models;

namespace JulschaVehicleTool.Core.Services.Import;

/// <summary>Checks a scan against the project before the preview is shown.</summary>
public static class ImportConflicts
{
    /// <summary>
    /// For every candidate whose name is already used in the project (or by an earlier candidate)
    /// a free name is proposed in <see cref="VehicleCandidate.ImportAs"/> and a note is added.
    /// Also notes handling names and siren IDs that collide with the project.
    /// </summary>
    public static void Annotate(Project project, ImportScan scan)
    {
        var used = VehicleNaming.UsedNames(project);
        var handlingNames = project.Resources.SelectMany(r => r.Vehicles)
            .Select(v => v.Handling?.HandlingName).Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var reserved = SirenIds.ParseList(project.ReservedSirenIds);

        foreach (var c in scan.Vehicles)
        {
            if (used.Contains(c.Name))
            {
                c.ImportAs = VehicleNaming.UniqueName(used, c.Name);
                c.Notes.Add($"'{c.Name}' already exists in the project — will be imported as '{c.ImportAs}' (files and meta names are renamed). Uncheck to skip.");
            }
            used.Add(c.ImportAs ?? c.Name);

            var handling = c.Handling?.HandlingName;
            if (c.ImportAs == null && !string.IsNullOrEmpty(handling) && handlingNames.Contains(handling))
                c.Notes.Add($"Handling name '{handling}' is already used by another vehicle in the project.");

            var siren = c.CarVariation?.SirenSettings ?? 0;
            if (siren != 0)
            {
                if (!SirenIds.IsValid(siren))
                    c.Notes.Add($"Siren setting {siren} is outside 1–255 — it will be renumbered.");
                else if (reserved.Contains(siren))
                    c.Notes.Add($"Siren setting {siren} is reserved on the server — it will be renumbered.");
                else if (project.CarCols.SirenSettings.Any(s => s.Id == siren))
                    c.Notes.Add($"Siren setting {siren} already exists in the project — it is renumbered if the content differs.");
            }
        }
    }
}
