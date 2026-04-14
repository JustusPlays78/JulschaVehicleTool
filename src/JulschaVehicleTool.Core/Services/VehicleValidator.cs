using JulschaVehicleTool.Core.Models;

namespace JulschaVehicleTool.Core.Services;

public static class VehicleValidator
{
    public record ValidationIssue(string Severity, string Field, string Message);

    public static List<ValidationIssue> Validate(Vehicle vehicle)
    {
        var issues = new List<ValidationIssue>();

        if (vehicle.Handling == null)
            issues.Add(new("Warning", "Handling", "No handling data"));

        if (vehicle.VehicleMeta == null)
        {
            issues.Add(new("Error", "VehicleMeta", "No vehicle metadata — vehicle won't spawn"));
        }
        else
        {
            if (string.IsNullOrEmpty(vehicle.VehicleMeta.ModelName))
                issues.Add(new("Error", "ModelName", "ModelName is empty"));
            if (string.IsNullOrEmpty(vehicle.VehicleMeta.GameName))
                issues.Add(new("Warning", "GameName", "GameName empty — no display name in game"));
        }

        if (vehicle.CarVariation == null)
            issues.Add(new("Warning", "CarVariation", "No color variation data"));

        // Cross-consistency check
        if (vehicle.VehicleMeta != null && vehicle.Handling != null)
        {
            var handlingId   = vehicle.VehicleMeta.HandlingId?.ToUpperInvariant();
            var handlingName = vehicle.Handling.HandlingName?.ToUpperInvariant();
            if (!string.IsNullOrEmpty(handlingId) && !string.IsNullOrEmpty(handlingName)
                && handlingId != handlingName)
                issues.Add(new("Warning", "HandlingId",
                    $"VehicleMeta.HandlingId '{vehicle.VehicleMeta.HandlingId}' ≠ Handling.HandlingName '{vehicle.Handling.HandlingName}'"));
        }

        return issues;
    }

    public static bool HasErrors(List<ValidationIssue> issues)
        => issues.Any(i => i.Severity == "Error");
}
