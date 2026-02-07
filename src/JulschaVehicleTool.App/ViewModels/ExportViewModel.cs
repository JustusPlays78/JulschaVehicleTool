using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JulschaVehicleTool.Core.Models;
using JulschaVehicleTool.Core.Services;
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
    [ObservableProperty] private string _statusMessage = "";

    private ObservableCollection<VehicleEntry>? _vehicles;
    public ObservableCollection<VehicleEntry>? Vehicles
    {
        get => _vehicles;
        set => SetProperty(ref _vehicles, value);
    }

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

        if (Vehicles == null || Vehicles.Count == 0)
        {
            StatusMessage = "No vehicles in project. Add vehicles first.";
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
                Vehicles = Vehicles.ToList()
            };

            var result = _exportService.Export(options);

            if (result.Success)
            {
                var msg = $"Export successful: {result.OutputPath} ({Vehicles.Count} vehicle(s))";
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
