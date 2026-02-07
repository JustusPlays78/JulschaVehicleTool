using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JulschaVehicleTool.Core.Models;
using JulschaVehicleTool.Core.Services;
using Microsoft.Win32;

namespace JulschaVehicleTool.App.ViewModels;

public partial class VehicleMetaViewModel : ObservableObject
{
    private readonly MetaXmlService _metaXmlService = new();

    [ObservableProperty] private string _statusMessage = "No vehicles.meta loaded";
    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private VehicleMetaData? _vehicle;

    private string? _currentFilePath;

    [RelayCommand]
    private void LoadFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Vehicles Meta|vehicles.meta;*.meta|All Files|*.*",
            Title = "Open vehicles.meta"
        };
        if (dialog.ShowDialog() == true) LoadFromPath(dialog.FileName);
    }

    public void LoadFromPath(string path)
    {
        try
        {
            Vehicle = _metaXmlService.LoadVehicleMeta(path);
            _currentFilePath = path;
            IsLoaded = true;
            StatusMessage = $"Loaded: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            Vehicle = null;
            IsLoaded = false;
        }
    }

    [RelayCommand]
    private void SaveFile()
    {
        if (Vehicle == null) return;
        var path = _currentFilePath;
        if (string.IsNullOrEmpty(path))
        {
            var dialog = new SaveFileDialog { Filter = "Meta|*.meta", FileName = "vehicles.meta" };
            if (dialog.ShowDialog() != true) return;
            path = dialog.FileName;
        }
        try { _metaXmlService.SaveVehicleMeta(Vehicle, path); _currentFilePath = path; StatusMessage = $"Saved: {path}"; }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
    }
}
