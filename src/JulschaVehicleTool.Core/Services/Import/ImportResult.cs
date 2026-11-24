using JulschaVehicleTool.Core.Models;

namespace JulschaVehicleTool.Core.Services.Import;

public class ImportResult
{
    public List<Vehicle> ImportedVehicles { get; } = new();
    public List<SirenSetting> SirensToAdd { get; } = new();
    public List<CarColsEntry> KitsToAdd { get; } = new();
    public List<CarColsEntry> LightsToAdd { get; } = new();
    public List<PassthroughMeta> ExtraMetasToAdd { get; } = new();
    public List<int> ImportedSirenIds { get; } = new();
    public List<string> Warnings { get; } = new();

    /// <summary>Full paths of the files written by PrepareImport (protected from cleanup until applied).</summary>
    public List<string> StoredFiles { get; } = new();
}
