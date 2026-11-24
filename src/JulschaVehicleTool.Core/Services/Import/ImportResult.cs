using JulschaVehicleTool.Core.Models;

namespace JulschaVehicleTool.Core.Services.Import;

public class ImportResult
{
    public List<Vehicle> ImportedVehicles { get; } = new();
    public List<SirenSetting> SirensToAdd { get; } = new();
    public List<int> ImportedSirenIds { get; } = new();
    public List<string> Warnings { get; } = new();
}
