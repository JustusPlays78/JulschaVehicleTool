using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JulschaVehicleTool.Core.Models;
using JulschaVehicleTool.Core.Services;
using Microsoft.Win32;

namespace JulschaVehicleTool.App.ViewModels;

public partial class HandlingEditorViewModel : ObservableObject
{
    private readonly MetaXmlService _metaXmlService = new();

    [ObservableProperty] private string _statusMessage = "No handling.meta loaded. Use File > Import or drop a file.";
    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private HandlingData? _handling;

    private string? _currentFilePath;

    [RelayCommand]
    private void LoadFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Handling Meta|handling.meta;*.meta|All Files|*.*",
            Title = "Open handling.meta"
        };

        if (dialog.ShowDialog() == true)
            LoadFromPath(dialog.FileName);
    }

    public void LoadFromPath(string path)
    {
        try
        {
            Handling = _metaXmlService.LoadHandling(path);
            _currentFilePath = path;
            IsLoaded = true;
            StatusMessage = $"Loaded: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading: {ex.Message}";
            Handling = null;
            IsLoaded = false;
        }
    }

    [RelayCommand]
    private void SaveFile()
    {
        if (Handling == null) return;

        var path = _currentFilePath;
        if (string.IsNullOrEmpty(path))
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Handling Meta|*.meta|All Files|*.*",
                FileName = "handling.meta"
            };
            if (dialog.ShowDialog() != true) return;
            path = dialog.FileName;
        }

        try
        {
            _metaXmlService.SaveHandling(Handling, path);
            _currentFilePath = path;
            StatusMessage = $"Saved: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SaveFileAs()
    {
        if (Handling == null) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Handling Meta|*.meta|All Files|*.*",
            FileName = "handling.meta"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            _metaXmlService.SaveHandling(Handling, dialog.FileName);
            _currentFilePath = dialog.FileName;
            StatusMessage = $"Saved: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
    }
}
