using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JulschaVehicleTool.Core.Services;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace JulschaVehicleTool.App.ViewModels;

public partial class ExportViewModel : ObservableObject
{
    private readonly FiveMExportService _exportService = new();

    [ObservableProperty] private string _resourceName = "";
    [ObservableProperty] private string _author = "Julscha Vehicle Tool";
    [ObservableProperty] private string _version = "1.0.0";
    [ObservableProperty] private string _outputPath = "";
    [ObservableProperty] private bool _includeVehicleNames = true;
    [ObservableProperty] private string _vehicleModelName = "";
    [ObservableProperty] private string _vehicleDisplayName = "";
    [ObservableProperty] private string _statusMessage = "";

    // Stream file paths
    [ObservableProperty] private string _yftPath = "";
    [ObservableProperty] private string _yftHiPath = "";
    [ObservableProperty] private string _ytdPath = "";
    [ObservableProperty] private string _ytdHiPath = "";

    // Meta file paths
    [ObservableProperty] private string _handlingMetaPath = "";
    [ObservableProperty] private string _vehiclesMetaPath = "";
    [ObservableProperty] private string _carVariationsMetaPath = "";
    [ObservableProperty] private string _carColsMetaPath = "";
    [ObservableProperty] private string _vehicleLayoutsMetaPath = "";

    [RelayCommand]
    private void BrowseOutputPath()
    {
        var dialog = new VistaFolderBrowserDialog
        {
            Description = "Select output folder for FiveM resource",
            UseDescriptionForTitle = true
        };
        if (dialog.ShowDialog() == true)
            OutputPath = dialog.SelectedPath;
    }

    [RelayCommand]
    private void BrowseYft() => BrowseFile("YFT Files|*.yft", path => YftPath = path);

    [RelayCommand]
    private void BrowseYftHi() => BrowseFile("YFT Files|*.yft", path => YftHiPath = path);

    [RelayCommand]
    private void BrowseYtd() => BrowseFile("YTD Files|*.ytd", path => YtdPath = path);

    [RelayCommand]
    private void BrowseYtdHi() => BrowseFile("YTD Files|*.ytd", path => YtdHiPath = path);

    [RelayCommand]
    private void BrowseHandlingMeta() => BrowseFile("Meta Files|*.meta|All Files|*.*", path => HandlingMetaPath = path);

    [RelayCommand]
    private void BrowseVehiclesMeta() => BrowseFile("Meta Files|*.meta|All Files|*.*", path => VehiclesMetaPath = path);

    [RelayCommand]
    private void BrowseCarVariationsMeta() => BrowseFile("Meta Files|*.meta|All Files|*.*", path => CarVariationsMetaPath = path);

    [RelayCommand]
    private void BrowseCarColsMeta() => BrowseFile("Meta Files|*.meta|All Files|*.*", path => CarColsMetaPath = path);

    [RelayCommand]
    private void BrowseVehicleLayoutsMeta() => BrowseFile("Meta Files|*.meta|All Files|*.*", path => VehicleLayoutsMetaPath = path);

    private static void BrowseFile(string filter, Action<string> setter)
    {
        var dialog = new OpenFileDialog { Filter = filter };
        if (dialog.ShowDialog() == true)
            setter(dialog.FileName);
    }

    [RelayCommand]
    private void Export()
    {
        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            StatusMessage = "Please select an output path.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ResourceName))
        {
            StatusMessage = "Please enter a resource name.";
            return;
        }

        try
        {
            var options = new ExportOptions
            {
                ResourceName = ResourceName,
                Author = Author,
                Version = Version,
                OutputPath = System.IO.Path.Combine(OutputPath, ResourceName),
                IncludeVehicleNames = IncludeVehicleNames,
                VehicleModelName = VehicleModelName,
                VehicleDisplayName = VehicleDisplayName,
                YftPath = string.IsNullOrWhiteSpace(YftPath) ? null : YftPath,
                YftHiPath = string.IsNullOrWhiteSpace(YftHiPath) ? null : YftHiPath,
                YtdPath = string.IsNullOrWhiteSpace(YtdPath) ? null : YtdPath,
                YtdHiPath = string.IsNullOrWhiteSpace(YtdHiPath) ? null : YtdHiPath,
                HandlingMetaPath = string.IsNullOrWhiteSpace(HandlingMetaPath) ? null : HandlingMetaPath,
                VehiclesMetaPath = string.IsNullOrWhiteSpace(VehiclesMetaPath) ? null : VehiclesMetaPath,
                CarVariationsMetaPath = string.IsNullOrWhiteSpace(CarVariationsMetaPath) ? null : CarVariationsMetaPath,
                CarColsMetaPath = string.IsNullOrWhiteSpace(CarColsMetaPath) ? null : CarColsMetaPath,
                VehicleLayoutsMetaPath = string.IsNullOrWhiteSpace(VehicleLayoutsMetaPath) ? null : VehicleLayoutsMetaPath
            };

            var result = _exportService.Export(options);

            if (result.Success)
            {
                var msg = $"Export successful: {result.OutputPath}";
                if (result.Warnings.Count > 0)
                    msg += $"\n{result.Warnings.Count} warning(s):\n" + string.Join("\n", result.Warnings);
                StatusMessage = msg;
            }
            else
            {
                StatusMessage = $"Export failed: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }
}
