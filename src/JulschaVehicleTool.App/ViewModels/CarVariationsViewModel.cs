using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JulschaVehicleTool.Core.Models;
using JulschaVehicleTool.Core.Services;
using Microsoft.Win32;

namespace JulschaVehicleTool.App.ViewModels;

public partial class CarVariationsViewModel : ObservableObject
{
    private readonly MetaXmlService _metaXmlService = new();

    [ObservableProperty] private string _statusMessage = "No carvariations.meta loaded";
    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private CarVariationData? _variation;

    private string? _currentFilePath;

    [RelayCommand]
    private void LoadFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CarVariations Meta|carvariations.meta;*.meta|All Files|*.*",
            Title = "Open carvariations.meta"
        };
        if (dialog.ShowDialog() == true) LoadFromPath(dialog.FileName);
    }

    public void LoadFromPath(string path)
    {
        try
        {
            Variation = _metaXmlService.LoadCarVariations(path);
            _currentFilePath = path;
            IsLoaded = true;
            StatusMessage = $"Loaded: {path}";
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
        if (Variation == null) return;
        var path = _currentFilePath;
        if (string.IsNullOrEmpty(path))
        {
            var dialog = new SaveFileDialog { Filter = "Meta|*.meta", FileName = "carvariations.meta" };
            if (dialog.ShowDialog() != true) return;
            path = dialog.FileName;
        }
        try { _metaXmlService.SaveCarVariations(Variation, path); StatusMessage = $"Saved: {path}"; }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
    }

    [RelayCommand]
    private void AddColorCombination() => Variation?.Colors.Add(new ColorCombination());

    [RelayCommand]
    private void RemoveColorCombination(ColorCombination? combo)
    {
        if (combo != null) Variation?.Colors.Remove(combo);
    }
}
