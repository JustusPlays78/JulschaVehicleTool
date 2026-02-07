using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JulschaVehicleTool.Core.Models;
using JulschaVehicleTool.Core.Services;
using Microsoft.Win32;

namespace JulschaVehicleTool.App.ViewModels;

public partial class SirenEditorViewModel : ObservableObject
{
    private readonly MetaXmlService _metaXmlService = new();

    [ObservableProperty] private string _statusMessage = "No carcols.meta loaded";
    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private CarColsData? _carCols;
    [ObservableProperty] private SirenSetting? _selectedSiren;

    private string? _currentFilePath;

    [RelayCommand]
    private void LoadFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CarCols Meta|carcols.meta;*.meta|All Files|*.*",
            Title = "Open carcols.meta"
        };
        if (dialog.ShowDialog() == true) LoadFromPath(dialog.FileName);
    }

    public void LoadFromPath(string path)
    {
        try
        {
            CarCols = _metaXmlService.LoadCarCols(path);
            _currentFilePath = path;
            IsLoaded = CarCols != null;
            if (CarCols?.SirenSettings.Count > 0)
                SelectedSiren = CarCols.SirenSettings[0];
            StatusMessage = IsLoaded ? $"Loaded: {path}" : "Failed to parse carcols.meta";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsLoaded = false;
        }
    }

    [RelayCommand]
    private void SaveFile()
    {
        if (CarCols == null) return;
        var path = _currentFilePath;
        if (string.IsNullOrEmpty(path))
        {
            var dialog = new SaveFileDialog { Filter = "Meta|*.meta", FileName = "carcols.meta" };
            if (dialog.ShowDialog() != true) return;
            path = dialog.FileName;
        }
        try { _metaXmlService.SaveCarCols(CarCols, path); StatusMessage = $"Saved: {path}"; }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
    }

    [RelayCommand]
    private void AddSirenSetting()
    {
        var setting = new SirenSetting { Id = 1000, Name = "new_siren" };
        CarCols?.SirenSettings.Add(setting);
        SelectedSiren = setting;
    }

    [RelayCommand]
    private void AddSirenLight()
    {
        if (SelectedSiren == null || SelectedSiren.Sirens.Count >= 20) return;
        SelectedSiren.Sirens.Add(new SirenLight());
    }
}
