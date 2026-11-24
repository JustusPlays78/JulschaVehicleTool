using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JulschaVehicleTool.App.Views;
using JulschaVehicleTool.Core.Constants;
using JulschaVehicleTool.Core.Models;
using JulschaVehicleTool.Core.Services;
using Microsoft.Win32;

namespace JulschaVehicleTool.App.ViewModels;

// Adding, removing, cloning and importing vehicles and meta files.
public partial class MainWindowViewModel
{
    #region Vehicle Management

    [RelayCommand]
    private void AddVehicle()
    {
        Resource? resource = SelectedTreeNode switch
        {
            ResourceTreeNode rn => rn.Resource,
            VehicleTreeNode vn => FindResourceForVehicle(vn.Vehicle),
            _ => null
        };

        if (resource == null)
        {
            StatusText = "Select a resource first.";
            return;
        }

        var used = VehicleNaming.UsedNames(_currentProject!);
        int n = 1;
        while (used.Contains($"vehicle_{n}")) n++;
        var vehicle = VehicleDefaults.CreateDefault($"vehicle_{n}");
        resource.Vehicles.Add(vehicle);
        _currentProject!.IsDirty = true;
        RebuildTree();

        // Select the new vehicle
        SelectVehicleInTree(vehicle);
        SaveProject(); // persist immediately
        StatusText = $"Added vehicle: {vehicle.Name}";
    }

    [RelayCommand]
    private void RemoveVehicle()
    {
        if (SelectedTreeNode is not VehicleTreeNode vehicleNode) return;

        var result = System.Windows.MessageBox.Show(
            $"Delete vehicle \"{vehicleNode.Vehicle.Name}\"?",
            "Confirm Delete", System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        var resource = FindResourceForVehicle(vehicleNode.Vehicle);
        if (resource == null) return;

        resource.Vehicles.Remove(vehicleNode.Vehicle);
        _currentProject!.IsDirty = true;
        RebuildTree();
        StatusText = $"Removed vehicle: {vehicleNode.Vehicle.Name}";
    }

    [RelayCommand]
    private void CloneVehicle()
    {
        if (_currentProject == null || SelectedTreeNode is not VehicleTreeNode vehicleNode) return;
        var resource = FindResourceForVehicle(vehicleNode.Vehicle);
        if (resource == null) return;

        Vehicle clone;
        try
        {
            var name = VehicleNaming.UniqueName(_currentProject, vehicleNode.Vehicle.Name, "_copy");
            clone = _projectService.CloneVehicle(_currentProject, vehicleNode.Vehicle, name);
        }
        catch (InvalidOperationException ex)
        {
            StatusText = $"Clone failed: {ex.Message}";
            return;
        }

        resource.Vehicles.Add(clone);
        _currentProject.IsDirty = true;
        RebuildTree();
        SelectVehicleInTree(clone);
        SaveProject();
        StatusText = $"Cloned: {clone.Name}";
    }

    [RelayCommand]
    private void ImportVehicleFiles()
    {
        if (_currentProject == null || SelectedTreeNode is not VehicleTreeNode vehicleNode) return;

        var dialog = new OpenFileDialog
        {
            Filter = "Vehicle Files|*.yft;*.ytd|YFT Models|*.yft|YTD Textures|*.ytd|All Files|*.*",
            Title = $"Import files for {vehicleNode.Vehicle.Name}",
            Multiselect = true
        };
        if (dialog.ShowDialog(Owner) != true) return;

        try
        {
            var vehicle = vehicleNode.Vehicle;
            Log($"[ImportFiles] Vehicle: {vehicle.Name}, Files: {string.Join(", ", dialog.FileNames.Select(Path.GetFileName))}");
            _projectService.ImportVehicleFiles(_currentProject, vehicle, dialog.FileNames);
            Log($"[ImportFiles] After import: YFT={vehicle.YftRelativePath ?? "null"}, YTD={vehicle.YtdRelativePath ?? "null"}, YFT_HI={vehicle.YftHiRelativePath ?? "null"}");
            _currentProject.IsDirty = true;
            RebuildTree();
            SelectVehicleInTree(vehicle); // triggers OnSelectedTreeNodeChanged → LoadVehicleIntoEditors
            SaveProject(); // persist immediately
            StatusText = $"Imported {dialog.FileNames.Length} file(s) for {vehicle.Name}";
        }
        catch (Exception ex)
        {
            StatusText = $"Import error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ImportVehicleFolderAsync()
    {
        if (_currentProject == null) return;

        var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
        {
            Description = "Select a vehicle folder or FiveM resource to import",
            UseDescriptionForTitle = true,
        };
        if (dialog.ShowDialog(Owner) != true) return;

        await ImportPathsAsync([dialog.SelectedPath]);
    }

    [RelayCommand]
    private async Task ImportVehicleZipAsync()
    {
        if (_currentProject == null) return;

        var dialog = new OpenFileDialog
        {
            Filter = "ZIP archives|*.zip",
            Title = "Import vehicles from ZIP",
            Multiselect = true,
        };
        if (dialog.ShowDialog(Owner) != true) return;

        await ImportPathsAsync(dialog.FileNames);
    }

    /// <summary>True while a scan/import is running (blocks a second drop).</summary>
    [ObservableProperty]
    private bool _isImporting;

    /// <summary>
    /// Durty-style import: each path (folder or .zip) is scanned, the detected vehicles are shown
    /// in a preview, and the confirmed ones are imported in the background.
    /// Target: the selected resource (or the resource of the selected vehicle); otherwise a new
    /// resource named after the folder/ZIP is created.
    /// </summary>
    public async Task ImportPathsAsync(IEnumerable<string> paths)
    {
        if (_currentProject == null || IsImporting) return;

        IsImporting = true;
        try
        {
            foreach (var path in paths)
                await ImportSinglePathAsync(path);
        }
        finally
        {
            IsImporting = false;
            IsProgressVisible = false;
        }
    }

    private async Task ImportSinglePathAsync(string path)
    {
        var project = _currentProject!;
        var isZip = File.Exists(path) && path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
        if (!isZip && !Directory.Exists(path))
        {
            StatusText = $"Not a folder or ZIP: {Path.GetFileName(path)}";
            return;
        }

        var sourceName = isZip ? Path.GetFileNameWithoutExtension(path) : new DirectoryInfo(path).Name;
        string? tempDir = null;

        try
        {
            IsProgressVisible = true;
            ProgressValue = 0;
            ProgressMax = 1;
            ProgressMessage = $"Scanning {sourceName}...";
            StatusText = ProgressMessage;

            var scanRoot = path;
            if (isZip)
            {
                tempDir = Path.Combine(Path.GetTempPath(), $"julveh_zipimport_{Guid.NewGuid():N}");
                await Task.Run(() => System.IO.Compression.ZipFile.ExtractToDirectory(path, tempDir));
                scanRoot = tempDir;
            }

            var scanner = new Core.Services.Import.VehicleImportScanner(_metaXmlService);
            var scan = await Task.Run(() => scanner.Scan(scanRoot));
            IsProgressVisible = false;
            Core.Services.Import.ImportConflicts.Annotate(project, scan);

            if (scan.Vehicles.Count == 0)
            {
                StatusText = $"No vehicles found in {sourceName}";
                System.Windows.MessageBox.Show(
                    $"No vehicles (.yft files or vehicles.meta entries) were found in \"{sourceName}\".",
                    "Import", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var target = SelectedTreeNode switch
            {
                ResourceTreeNode rn => rn.Resource,
                VehicleTreeNode vn => FindResourceForVehicle(vn.Vehicle),
                _ => null
            };
            var targetName = target?.Name ?? $"{SanitizeResourceName(sourceName)} (new)";

            var preview = new Views.ImportPreviewDialog(scan, sourceName, targetName) { Owner = Owner };
            if (preview.ShowDialog() != true)
            {
                StatusText = "Import cancelled";
                return;
            }

            IsProgressVisible = true;
            var progress = new Progress<(int current, int total, string message)>(p =>
            {
                ProgressValue = p.current;
                ProgressMax = p.total;
                ProgressMessage = p.message;
            });

            var result = await Task.Run(() => _projectService.PrepareImport(project, scan, progress));

            if (target == null)
            {
                target = new Resource { Name = UniqueResourceName(SanitizeResourceName(sourceName)) };
                project.Resources.Add(target);
            }
            _projectService.ApplyImport(project, target, result);

            RebuildTree();
            SaveProject();

            var first = result.ImportedVehicles.FirstOrDefault();
            if (first != null) SelectVehicleInTree(first);

            StatusText = $"Imported {result.ImportedVehicles.Count} vehicle(s) into {target.Name}"
                         + (result.ImportedSirenIds.Count > 0 ? $", {result.ImportedSirenIds.Count} siren group(s)" : "")
                         + (result.Warnings.Count > 0 ? $" — {result.Warnings.Count} warning(s)" : "");

            if (result.Warnings.Count > 0)
                System.Windows.MessageBox.Show(string.Join(Environment.NewLine, result.Warnings),
                    "Import warnings", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            StatusText = $"Import error: {ex.Message}";
            Log($"[Import] {ex}");
        }
        finally
        {
            IsProgressVisible = false;
            if (tempDir != null)
            {
                try { Directory.Delete(tempDir, true); } catch { /* best effort */ }
            }
        }
    }

    private static string SanitizeResourceName(string name)
    {
        var cleaned = new string(name.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) || c is '_' or '-' ? c : '_').ToArray()).Trim('_');
        return string.IsNullOrEmpty(cleaned) ? "imported" : cleaned;
    }

    private string UniqueResourceName(string baseName)
    {
        var existing = new HashSet<string>(_currentProject!.Resources.Select(r => r.Name), StringComparer.OrdinalIgnoreCase);
        if (!existing.Contains(baseName)) return baseName;
        int n = 2;
        while (existing.Contains($"{baseName}_{n}")) n++;
        return $"{baseName}_{n}";
    }

    [RelayCommand]
    private void ImportMetaFile()
    {
        if (_currentProject == null) return;

        // Determine target: single vehicle or all vehicles in a resource
        Vehicle? singleVehicle = null;
        Resource? targetResource = null;

        switch (SelectedTreeNode)
        {
            case VehicleTreeNode vn:
                singleVehicle = vn.Vehicle;
                targetResource = FindResourceForVehicle(vn.Vehicle);
                break;
            case ResourceTreeNode rn:
                targetResource = rn.Resource;
                break;
            default:
                StatusText = "Select a vehicle or resource first.";
                return;
        }

        if (targetResource == null) return;

        var dialog = new OpenFileDialog
        {
            Filter = "Meta Files|*.meta|All Files|*.*",
            Title = "Import meta file",
            Multiselect = true
        };
        if (dialog.ShowDialog(Owner) != true) return;

        var allVehicles = targetResource.Vehicles;
        int applied = 0;
        _skippedPartialMatches.Clear();

        foreach (var file in dialog.FileNames)
        {
            var fileName = Path.GetFileName(file).ToLowerInvariant();
            Log($"[ImportMeta] Processing: {fileName}");

            try
            {
                if (fileName.Contains("handling"))
                    applied += ImportMultiMeta(file, allVehicles, singleVehicle,
                        f => _metaXmlService.LoadAllHandlings(f),
                        (v, d) => v.Handling = d, "Handling");
                else if (fileName.Contains("vehicles"))
                    applied += ImportMultiMeta(file, allVehicles, singleVehicle,
                        f => _metaXmlService.LoadAllVehicleMetas(f),
                        (v, d) => v.VehicleMeta = d, "VehicleMeta");
                else if (fileName.Contains("carvariations"))
                    applied += ImportMultiMeta(file, allVehicles, singleVehicle,
                        f => _metaXmlService.LoadAllCarVariations(f),
                        (v, d) => v.CarVariation = d, "CarVariation");
                else if (fileName.Contains("carcols"))
                {
                    // CarCols is project-level — merge imported siren groups into the project pool
                    var data = _metaXmlService.LoadCarCols(file);
                    if (data != null && _currentProject != null)
                    {
                        var existingIds = new HashSet<int>(_currentProject.CarCols.SirenSettings.Select(s => s.Id));
                        int merged = 0;
                        foreach (var siren in data.SirenSettings)
                        {
                            if (existingIds.Add(siren.Id))
                            {
                                _currentProject.CarCols.SirenSettings.Add(siren);
                                merged++;
                            }
                        }
                        Log($"  [CarCols] Merged {merged} siren group(s) into project pool (skipped {data.SirenSettings.Count - merged} duplicates)");
                        applied += merged;
                    }
                }
                else
                {
                    Log($"  [Skip] Unknown meta type: {fileName}");
                }
            }
            catch (Exception ex)
            {
                Log($"  [ERROR] {fileName}: {ex.Message}");
                StatusText = $"Error importing {fileName}: {ex.Message}";
                return;
            }
        }

        _currentProject!.IsDirty = true;
        if (singleVehicle != null)
            LoadVehicleIntoEditors(singleVehicle);
        SaveProject();
        StatusText = $"Imported {applied} assignment(s) from {dialog.FileNames.Length} file(s)"
            + (_skippedPartialMatches.Count > 0
                ? $" — not applied (only partial name match): {string.Join(", ", _skippedPartialMatches)}"
                : "");
    }

    /// <summary>
    /// Import a multi-vehicle meta file. Uses AutoMatch to assign entries to vehicles.
    /// When only 1 entry exists and a single vehicle is selected, assigns directly.
    /// When multiple entries exist, always auto-matches across ALL vehicles in the resource.
    /// </summary>
    private int ImportMultiMeta<T>(
        string filePath,
        IList<Vehicle> allVehicles,
        Vehicle? singleVehicle,
        Func<string, Dictionary<string, T>> loadAll,
        Action<Vehicle, T> assign,
        string metaType)
    {
        var parsed = loadAll(filePath);
        Log($"  [{metaType}] Found {parsed.Count} entry/entries: {string.Join(", ", parsed.Keys)}");

        if (parsed.Count == 0) return 0;

        // Only 1 entry + single vehicle selected → assign directly (no ambiguity)
        if (parsed.Count == 1 && singleVehicle != null)
        {
            var entry = parsed.First();
            assign(singleVehicle, entry.Value);
            Log($"  [{metaType}] Single entry '{entry.Key}' → assigned to {singleVehicle.Name}");
            return 1;
        }

        // Multiple entries → always auto-match across ALL vehicles in the resource
        var matches = _matchService.AutoMatch(parsed, allVehicles);
        int count = 0;

        foreach (var match in matches)
        {
            if (match.IsAutoApplicable)
            {
                assign(match.MatchedVehicle!, match.Data);
                Log($"  [{metaType}] Matched '{match.ParsedName}' → {match.MatchedVehicle!.Name} (confidence: {match.Confidence})");
                count++;
            }
            else if (match.MatchedVehicle != null)
            {
                Log($"  [{metaType}] Not applied: '{match.ParsedName}' only partially matches {match.MatchedVehicle.Name} — rename the vehicle to apply it");
                _skippedPartialMatches.Add($"{match.ParsedName} → {match.MatchedVehicle.Name}?");
            }
            else
            {
                Log($"  [{metaType}] No match for '{match.ParsedName}' — skipped");
            }
        }

        if (count == 0)
        {
            Log($"  [{metaType}] WARNING: No matches found! Vehicle names: {string.Join(", ", allVehicles.Select(v => v.Name))}");
            Log($"  [{metaType}] Meta entry names: {string.Join(", ", parsed.Keys)}");
            Log($"  [{metaType}] Rename your vehicles to match the meta entry names (e.g., police1, police2, ambulance)");
        }

        return count;
    }

    [RelayCommand]
    private void ExportSingleMeta(string? metaType)
    {
        if (string.IsNullOrEmpty(metaType) || SelectedTreeNode is not VehicleTreeNode vehicleNode) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Meta Files|*.meta|All Files|*.*",
            FileName = $"{metaType}.meta",
            Title = $"Export {metaType}.meta"
        };
        if (dialog.ShowDialog(Owner) != true) return;

        try
        {
            if (_currentProject == null) return;
            _exportService.ExportSingleMeta(_currentProject, vehicleNode.Vehicle, metaType, dialog.FileName, _metaXmlService);
            StatusText = $"Exported {metaType}.meta";
        }
        catch (Exception ex)
        {
            StatusText = $"Export error: {ex.Message}";
        }
    }

    #endregion
}
