using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JulschaVehicleTool.Core.Constants;
using JulschaVehicleTool.Core.Models;
using JulschaVehicleTool.Core.Services;
using Microsoft.Win32;

namespace JulschaVehicleTool.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private NavigationItem? _selectedNavItem;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _vehicleName = "No Vehicle";

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private string _titleSuffix = "";

    public ObservableCollection<NavigationItem> NavigationItems { get; } = new();
    public ObservableCollection<RecentProjectItem> RecentProjects { get; } = new();

    private readonly ModelViewerViewModel _modelViewerVm;
    private readonly HandlingEditorViewModel _handlingEditorVm;
    private readonly CarVariationsViewModel _carVariationsVm;
    private readonly SirenEditorViewModel _sirenEditorVm;
    private readonly VehicleMetaViewModel _vehicleMetaVm;
    private readonly ExportViewModel _exportVm;
    private readonly ProjectService _projectService = new();

    private VehicleProject _currentProject = new();

    public MainWindowViewModel(
        ModelViewerViewModel modelViewerVm,
        HandlingEditorViewModel handlingEditorVm,
        CarVariationsViewModel carVariationsVm,
        SirenEditorViewModel sirenEditorVm,
        VehicleMetaViewModel vehicleMetaVm,
        ExportViewModel exportVm)
    {
        _modelViewerVm = modelViewerVm;
        _handlingEditorVm = handlingEditorVm;
        _carVariationsVm = carVariationsVm;
        _sirenEditorVm = sirenEditorVm;
        _vehicleMetaVm = vehicleMetaVm;
        _exportVm = exportVm;

        NavigationItems.Add(new NavigationItem("3D Viewer", "Monitor", nameof(ModelViewerViewModel)));
        NavigationItems.Add(new NavigationItem("Handling", "Gauge", nameof(HandlingEditorViewModel)));
        NavigationItems.Add(new NavigationItem("Variations", "Palette", nameof(CarVariationsViewModel)));
        NavigationItems.Add(new NavigationItem("Sirens", "Lightbulb", nameof(SirenEditorViewModel)));
        NavigationItems.Add(new NavigationItem("Vehicle", "FileDocument", nameof(VehicleMetaViewModel)));
        NavigationItems.Add(new NavigationItem("Export", "PackageVariant", nameof(ExportViewModel)));

        SelectedNavItem = NavigationItems[0];
        LoadRecentProjects();
    }

    partial void OnSelectedNavItemChanged(NavigationItem? value)
    {
        if (value is null) return;

        CurrentViewModel = value.ViewModelKey switch
        {
            nameof(ModelViewerViewModel) => _modelViewerVm,
            nameof(HandlingEditorViewModel) => _handlingEditorVm,
            nameof(CarVariationsViewModel) => _carVariationsVm,
            nameof(SirenEditorViewModel) => _sirenEditorVm,
            nameof(VehicleMetaViewModel) => _vehicleMetaVm,
            nameof(ExportViewModel) => _exportVm,
            _ => null
        };
    }

    #region Project Commands

    [RelayCommand]
    private void NewProject()
    {
        _currentProject = _projectService.CreateNew();
        VehicleName = _currentProject.ProjectName;
        IsDirty = false;
        TitleSuffix = "";
        StatusText = "New project created";
    }

    [RelayCommand]
    private void OpenProject()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Vehicle Project|*.julveh|All Files|*.*",
            Title = "Open Vehicle Project"
        };
        if (dialog.ShowDialog() == true)
            OpenProjectFromPath(dialog.FileName);
    }

    public void OpenProjectFromPath(string path)
    {
        try
        {
            _currentProject = _projectService.Load(path);
            VehicleName = _currentProject.ProjectName;
            IsDirty = false;
            TitleSuffix = $" - {Path.GetFileName(path)}";
            _projectService.AddToRecentProjects(path);
            LoadRecentProjects();

            // Load associated files into editors
            if (!string.IsNullOrEmpty(_currentProject.HandlingMetaPath) && File.Exists(_currentProject.HandlingMetaPath))
                _handlingEditorVm.LoadFromPath(_currentProject.HandlingMetaPath);
            if (!string.IsNullOrEmpty(_currentProject.CarVariationsMetaPath) && File.Exists(_currentProject.CarVariationsMetaPath))
                _carVariationsVm.LoadFromPath(_currentProject.CarVariationsMetaPath);
            if (!string.IsNullOrEmpty(_currentProject.CarColsMetaPath) && File.Exists(_currentProject.CarColsMetaPath))
                _sirenEditorVm.LoadFromPath(_currentProject.CarColsMetaPath);
            if (!string.IsNullOrEmpty(_currentProject.VehiclesMetaPath) && File.Exists(_currentProject.VehiclesMetaPath))
                _vehicleMetaVm.LoadFromPath(_currentProject.VehiclesMetaPath);
            if (!string.IsNullOrEmpty(_currentProject.YftFilePath) && File.Exists(_currentProject.YftFilePath))
                _modelViewerVm.LoadFromPath(_currentProject.YftFilePath, _currentProject.YtdFilePath);

            // Populate export view with project paths
            _exportVm.ResourceName = _currentProject.ProjectName;
            _exportVm.YftPath = _currentProject.YftFilePath ?? "";
            _exportVm.YftHiPath = _currentProject.YftHiFilePath ?? "";
            _exportVm.YtdPath = _currentProject.YtdFilePath ?? "";
            _exportVm.YtdHiPath = _currentProject.YtdHiFilePath ?? "";
            _exportVm.HandlingMetaPath = _currentProject.HandlingMetaPath ?? "";
            _exportVm.VehiclesMetaPath = _currentProject.VehiclesMetaPath ?? "";
            _exportVm.CarVariationsMetaPath = _currentProject.CarVariationsMetaPath ?? "";
            _exportVm.CarColsMetaPath = _currentProject.CarColsMetaPath ?? "";
            _exportVm.VehicleLayoutsMetaPath = _currentProject.VehicleLayoutsMetaPath ?? "";

            StatusText = $"Opened: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error opening project: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SaveProject()
    {
        if (string.IsNullOrEmpty(_currentProject.ProjectFilePath))
        {
            SaveProjectAs();
            return;
        }

        try
        {
            _projectService.Save(_currentProject, _currentProject.ProjectFilePath);
            IsDirty = false;
            StatusText = $"Saved: {Path.GetFileName(_currentProject.ProjectFilePath)}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error saving: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SaveProjectAs()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Vehicle Project|*.julveh|All Files|*.*",
            FileName = $"{_currentProject.ProjectName}.julveh",
            Title = "Save Vehicle Project"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            _projectService.Save(_currentProject, dialog.FileName);
            _projectService.AddToRecentProjects(dialog.FileName);
            LoadRecentProjects();
            IsDirty = false;
            TitleSuffix = $" - {Path.GetFileName(dialog.FileName)}";
            StatusText = $"Saved: {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error saving: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenRecentProject(string? path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            OpenProjectFromPath(path);
        else
            StatusText = "Recent project file not found.";
    }

    #endregion

    #region Handling Presets

    [RelayCommand]
    private void ApplyHandlingPreset(string? presetName)
    {
        if (string.IsNullOrEmpty(presetName)) return;

        var preset = HandlingPresets.All.FirstOrDefault(p => p.Name == presetName);
        if (preset.Create == null) return;

        var handling = preset.Create();
        _handlingEditorVm.Handling = handling;
        _handlingEditorVm.IsLoaded = true;
        _handlingEditorVm.StatusMessage = $"Applied preset: {presetName}";
        IsDirty = true;

        // Navigate to handling editor
        SelectedNavItem = NavigationItems[1];
        StatusText = $"Handling preset applied: {presetName}";
    }

    public string[] HandlingPresetNames => HandlingPresets.All.Select(p => p.Name).ToArray();

    #endregion

    #region Recent Projects

    private void LoadRecentProjects()
    {
        RecentProjects.Clear();
        foreach (var path in _projectService.LoadRecentProjects())
        {
            RecentProjects.Add(new RecentProjectItem
            {
                FilePath = path,
                DisplayName = Path.GetFileNameWithoutExtension(path),
                FolderPath = Path.GetDirectoryName(path) ?? ""
            });
        }
    }

    #endregion
}

public partial class NavigationItem : ObservableObject
{
    public string Label { get; }
    public string Icon { get; }
    public string ViewModelKey { get; }

    public NavigationItem(string label, string icon, string viewModelKey)
    {
        Label = label;
        Icon = icon;
        ViewModelKey = viewModelKey;
    }
}

public class RecentProjectItem
{
    public string FilePath { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string FolderPath { get; set; } = "";
}
