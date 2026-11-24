using System.Xml.Linq;
using JulschaVehicleTool.Core.Models;

namespace JulschaVehicleTool.Core.Services.Import;

public enum MetaKind { Handling, Vehicles, CarVariations, CarCols, Other }

public class ScannedMetaFile
{
    public required string Path { get; init; }
    public required MetaKind Kind { get; init; }
    public string RootElement { get; init; } = "";
}

/// <summary>One vehicle detected in a dropped folder/ZIP, ready to be confirmed by the user.</summary>
public class VehicleCandidate
{
    public required string Name { get; init; }

    /// <summary>
    /// Name to import the vehicle as (null/empty = <see cref="Name"/>). Files and meta names are renamed
    /// accordingly. Set by <see cref="ImportConflicts.Annotate"/> when the name is already taken.
    /// </summary>
    public string? ImportAs { get; set; }

    public string? Model { get; set; }
    public string? ModelHi { get; set; }
    public string? Texture { get; set; }
    public string? TextureHi { get; set; }
    /// <summary>Mod kit parts (name_bumf.yft, ...).</summary>
    public List<string> ModParts { get; } = new();
    /// <summary>Additional texture dictionaries belonging to this vehicle (name_*.ytd, name+hi*.ytd).</summary>
    public List<string> ExtraTextures { get; } = new();

    public VehicleMetaData? VehicleMeta { get; set; }
    public HandlingData? Handling { get; set; }
    public CarVariationData? CarVariation { get; set; }

    /// <summary>Whether the vehicle should be imported (user can toggle in the preview).</summary>
    public bool Include { get; set; } = true;

    /// <summary>Human-readable hints shown in the preview (missing parts, ...).</summary>
    public List<string> Notes { get; } = new();

    public IEnumerable<string> AllFiles =>
        new[] { Model, ModelHi, Texture, TextureHi }.Where(f => f != null).Select(f => f!)
            .Concat(ModParts).Concat(ExtraTextures);
}

public class ImportScan
{
    public required string RootPath { get; init; }
    public List<VehicleCandidate> Vehicles { get; } = new();
    public List<ScannedMetaFile> MetaFiles { get; } = new();
    /// <summary>Siren groups found in carcols.meta files.</summary>
    public CarColsData CarCols { get; } = new();
    /// <summary>Stream files that could not be assigned to any vehicle.</summary>
    public List<string> UnassignedFiles { get; } = new();
    public List<string> Warnings { get; } = new();
}

/// <summary>
/// Scans a folder (e.g. a downloaded add-on vehicle or a whole FiveM resource) and groups
/// stream files and meta entries into vehicles. Detection rules:
/// <list type="bullet">
/// <item>Meta files are identified by their XML root element, not their file name.</item>
/// <item>Vehicle names come from vehicles.meta modelNames; without metas from *.yft names.</item>
/// <item>name.yft / name_hi.yft / name.ytd / name+hi.ytd are the main slots.</item>
/// <item>name_*.yft are mod kit parts; name_*.ytd and name+hi*.ytd extra textures. If several vehicle
/// names match (police / police_bike), the longest one wins.</item>
/// </list>
/// </summary>
public class VehicleImportScanner
{
    private readonly MetaXmlService _meta;

    public VehicleImportScanner(MetaXmlService meta) => _meta = meta;

    private static readonly string[] StreamExtensions = [".yft", ".ytd"];

    public ImportScan Scan(string rootPath)
    {
        var scan = new ImportScan { RootPath = rootPath };

        var allFiles = Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
            .Where(f => !IsHidden(rootPath, f))
            .OrderBy(f => f.Length) // prefer shallower copies on duplicates
            .ThenBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // ---- stream files, keyed by lower-case file name ----
        var streamFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in allFiles.Where(f => StreamExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())))
        {
            var name = Path.GetFileName(file);
            if (!streamFiles.TryAdd(name, file))
                scan.Warnings.Add($"Duplicate file '{name}' — using {Rel(rootPath, streamFiles[name])}, ignoring {Rel(rootPath, file)}");
        }

        // ---- meta files ----
        var vehicleMetas = new Dictionary<string, VehicleMetaData>(StringComparer.OrdinalIgnoreCase);
        var handlings = new Dictionary<string, HandlingData>(StringComparer.OrdinalIgnoreCase);
        var variations = new Dictionary<string, CarVariationData>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in allFiles.Where(f => f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)
                                              || f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
        {
            var root = TryGetRootElement(file);
            if (root == null) continue;

            var kind = root switch
            {
                "CHandlingDataMgr" => MetaKind.Handling,
                "CVehicleModelInfo__InitDataList" => MetaKind.Vehicles,
                "CVehicleModelInfoVariation" => MetaKind.CarVariations,
                "CVehicleModelInfoVarGlobal" => MetaKind.CarCols,
                _ => MetaKind.Other,
            };
            if (kind == MetaKind.Other && file.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                continue; // random XML, not a GTA meta

            scan.MetaFiles.Add(new ScannedMetaFile { Path = file, Kind = kind, RootElement = root });

            try
            {
                switch (kind)
                {
                    case MetaKind.Vehicles:
                        foreach (var (k, v) in _meta.LoadAllVehicleMetas(file)) AddUnique(vehicleMetas, k, v, file, scan);
                        break;
                    case MetaKind.Handling:
                        foreach (var (k, v) in _meta.LoadAllHandlings(file)) AddUnique(handlings, k, v, file, scan);
                        break;
                    case MetaKind.CarVariations:
                        foreach (var (k, v) in _meta.LoadAllCarVariations(file)) AddUnique(variations, k, v, file, scan);
                        break;
                    case MetaKind.CarCols:
                        MergeCarCols(_meta.LoadCarCols(file), file, scan);
                        if (HasModKitsOrLights(file))
                            scan.Warnings.Add($"{Rel(rootPath, file)} contains mod kits/lights — these are not imported yet (only sirens).");
                        break;
                    case MetaKind.Other:
                        scan.Warnings.Add($"{Rel(rootPath, file)} ({root}) is not supported yet and will be ignored.");
                        break;
                }
            }
            catch (Exception ex)
            {
                scan.Warnings.Add($"Could not read {Rel(rootPath, file)}: {ex.Message}");
            }
        }

        // ---- vehicle names ----
        var yftStems = streamFiles.Keys
            .Where(n => n.EndsWith(".yft", StringComparison.OrdinalIgnoreCase))
            .Select(n => Path.GetFileNameWithoutExtension(n).ToLowerInvariant())
            .ToHashSet();
        var ytdStems = streamFiles.Keys
            .Where(n => n.EndsWith(".ytd", StringComparison.OrdinalIgnoreCase))
            .Select(n => Path.GetFileNameWithoutExtension(n).ToLowerInvariant())
            .ToHashSet();

        // Names from vehicles.meta are authoritative. In addition, every yft that looks like a
        // standalone vehicle (has its own ytd/_hi, or is not "<other>_<part>") becomes a vehicle,
        // so packs where some vehicles lack a meta entry still work.
        var names = new HashSet<string>(vehicleMetas.Keys.Select(k => k.ToLowerInvariant()));
        var yftCandidates = yftStems.Where(s => !s.EndsWith("_hi")).ToList();
        var prefixCandidates = yftCandidates.Concat(names).Distinct().ToList();
        foreach (var s in yftCandidates)
        {
            if (names.Contains(s)) continue;
            bool hasOwnFiles = ytdStems.Contains(s) || yftStems.Contains(s + "_hi");
            bool isPartOfOther = prefixCandidates.Any(b => b != s && s.StartsWith(b + "_"));
            if (hasOwnFiles || !isPartOfOther)
                names.Add(s);
        }

        // Longest names first so that police_bike claims its files before police
        var ordered = names.OrderByDescending(n => n.Length).ThenBy(n => n).ToList();
        var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string? Take(string fileName)
        {
            if (!streamFiles.TryGetValue(fileName, out var path) || !claimed.Add(fileName)) return null;
            return path;
        }

        var byName = new Dictionary<string, VehicleCandidate>();
        foreach (var n in ordered)
        {
            var c = new VehicleCandidate
            {
                Name = n,
                Model = Take(n + ".yft"),
                ModelHi = Take(n + "_hi.yft"),
                Texture = Take(n + ".ytd"),
                TextureHi = Take(n + "+hi.ytd"),
            };

            foreach (var (fileName, path) in streamFiles.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (claimed.Contains(fileName)) continue;
                var stem = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
                var ext = Path.GetExtension(fileName).ToLowerInvariant();

                if (ext == ".yft" && stem.StartsWith(n + "_"))
                {
                    claimed.Add(fileName);
                    c.ModParts.Add(path);
                }
                else if (ext == ".ytd" && (stem.StartsWith(n + "_") || stem.StartsWith(n + "+")))
                {
                    claimed.Add(fileName);
                    c.ExtraTextures.Add(path);
                }
            }

            byName[n] = c;
        }

        // ---- attach metas ----
        foreach (var n in names.OrderBy(n => n))
        {
            var c = byName[n];

            if (vehicleMetas.TryGetValue(n, out var vm)) c.VehicleMeta = vm;
            if (variations.TryGetValue(n, out var cv)) c.CarVariation = cv;

            var handlingId = c.VehicleMeta?.HandlingId;
            if (!string.IsNullOrEmpty(handlingId) && handlings.TryGetValue(handlingId, out var h)) c.Handling = h;
            else if (handlings.TryGetValue(n, out var h2)) c.Handling = h2;

            if (c.Model == null)
            {
                c.Notes.Add("No model file (.yft) found");
                c.Include = false;
            }
            if (c.Texture == null) c.Notes.Add("No texture dictionary (.ytd)");
            if (c.VehicleMeta == null) c.Notes.Add("No vehicles.meta entry — defaults will be used");
            if (c.Handling == null)
                c.Notes.Add(string.IsNullOrEmpty(handlingId)
                    ? "No handling.meta entry — defaults will be used"
                    : $"Handling '{handlingId}' not found — defaults will be used");

            var sirenId = c.CarVariation?.SirenSettings ?? 0;
            if (sirenId != 0 && scan.CarCols.SirenSettings.All(s => s.Id != sirenId))
                c.Notes.Add($"Siren setting {sirenId} is referenced but not part of this import");

            scan.Vehicles.Add(c);
        }

        foreach (var (fileName, path) in streamFiles)
            if (!claimed.Contains(fileName))
                scan.UnassignedFiles.Add(path);

        foreach (var name in handlings.Keys.Where(h => scan.Vehicles.All(v => v.Handling?.HandlingName.Equals(h, StringComparison.OrdinalIgnoreCase) != true)))
            scan.Warnings.Add($"handling '{name}' is not used by any detected vehicle");

        return scan;
    }

    private static void AddUnique<T>(Dictionary<string, T> dict, string key, T value, string file, ImportScan scan)
    {
        if (!dict.TryAdd(key, value))
            scan.Warnings.Add($"'{key}' is defined more than once — ignoring the entry in {Rel(scan.RootPath, file)}");
    }

    private void MergeCarCols(CarColsData data, string file, ImportScan scan)
    {
        foreach (var siren in data.SirenSettings)
        {
            if (scan.CarCols.SirenSettings.Any(s => s.Id == siren.Id))
                scan.Warnings.Add($"Siren setting {siren.Id} is defined more than once — ignoring the one in {Rel(scan.RootPath, file)}");
            else
                scan.CarCols.SirenSettings.Add(siren);
        }
    }

    private static bool HasModKitsOrLights(string file)
    {
        try
        {
            var root = XDocument.Load(file).Root;
            return root?.Element("Kits")?.HasElements == true || root?.Element("Lights")?.HasElements == true;
        }
        catch { return false; }
    }

    private static string? TryGetRootElement(string file)
    {
        try
        {
            using var reader = System.Xml.XmlReader.Create(file, new System.Xml.XmlReaderSettings
            {
                DtdProcessing = System.Xml.DtdProcessing.Ignore,
                IgnoreComments = true,
            });
            return reader.MoveToContent() == System.Xml.XmlNodeType.Element ? reader.LocalName : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsHidden(string root, string file)
    {
        var rel = Path.GetRelativePath(root, file);
        return rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                  .Any(part => part.StartsWith('.') || part.Equals("__MACOSX", StringComparison.OrdinalIgnoreCase));
    }

    internal static string Rel(string root, string path) => Path.GetRelativePath(root, path);
}
