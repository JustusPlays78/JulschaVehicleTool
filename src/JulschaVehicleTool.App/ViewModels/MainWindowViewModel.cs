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

    [ObservableProperty]
    private VehicleEntry? _selectedVehicle;

    public ObservableCollection<NavigationItem> NavigationItems { get; } = new();
    public ObservableCollection<RecentProjectItem> RecentProjects { get; } = new();
    public ObservableCollection<VehicleEntry> Vehicles => _currentProject.Vehicles;

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
        _exportVm.Vehicles = _currentProject.Vehicles;
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

    #region Vehicle Management

    partial void OnSelectedVehicleChanged(VehicleEntry? value)
    {
        if (value is null)
        {
            VehicleName = "No Vehicle";
            ClearEditors();
            return;
        }

        VehicleName = value.Name;
        LoadVehicleIntoEditors(value);
    }

    private void ClearEditors()
    {
        _handlingEditorVm.Handling = null;
        _handlingEditorVm.IsLoaded = false;
        _handlingEditorVm.StatusMessage = "No handling.meta loaded";

        _carVariationsVm.Variation = null;
        _carVariationsVm.IsLoaded = false;
        _carVariationsVm.StatusMessage = "No carvariations.meta loaded";

        _sirenEditorVm.CarCols = null;
        _sirenEditorVm.SelectedSiren = null;
        _sirenEditorVm.IsLoaded = false;
        _sirenEditorVm.StatusMessage = "No carcols.meta loaded";

        _vehicleMetaVm.Vehicle = null;
        _vehicleMetaVm.IsLoaded = false;
        _vehicleMetaVm.StatusMessage = "No vehicles.meta loaded";
    }

    private void LoadVehicleIntoEditors(VehicleEntry vehicle)
    {
        // Load meta files into editors
        if (!string.IsNullOrEmpty(vehicle.HandlingMetaPath) && File.Exists(vehicle.HandlingMetaPath))
            _handlingEditorVm.LoadFromPath(vehicle.HandlingMetaPath);

        if (!string.IsNullOrEmpty(vehicle.CarVariationsMetaPath) && File.Exists(vehicle.CarVariationsMetaPath))
            _carVariationsVm.LoadFromPath(vehicle.CarVariationsMetaPath);

        if (!string.IsNullOrEmpty(vehicle.CarColsMetaPath) && File.Exists(vehicle.CarColsMetaPath))
            _sirenEditorVm.LoadFromPath(vehicle.CarColsMetaPath);

        if (!string.IsNullOrEmpty(vehicle.VehiclesMetaPath) && File.Exists(vehicle.VehiclesMetaPath))
            _vehicleMetaVm.LoadFromPath(vehicle.VehiclesMetaPath);

        if (!string.IsNullOrEmpty(vehicle.YftFilePath) && File.Exists(vehicle.YftFilePath))
            _modelViewerVm.LoadFromPath(vehicle.YftFilePath, vehicle.YtdFilePath);

        StatusText = $"Loaded vehicle: {vehicle.Name}";
    }

    [RelayCommand]
    private void AddVehicle()
    {
        var vehicle = new VehicleEntry { Name = $"Vehicle {_currentProject.Vehicles.Count + 1}" };
        _currentProject.Vehicles.Add(vehicle);
        SelectedVehicle = vehicle;
        IsDirty = true;
        StatusText = $"Added: {vehicle.Name}";
    }

    [RelayCommand]
    private void RemoveVehicle()
    {
        if (SelectedVehicle == null) return;

        var name = SelectedVehicle.Name;
        var idx = _currentProject.Vehicles.IndexOf(SelectedVehicle);
        _currentProject.Vehicles.Remove(SelectedVehicle);

        if (_currentProject.Vehicles.Count > 0)
            SelectedVehicle = _currentProject.Vehicles[Math.Max(0, idx - 1)];
        else
            SelectedVehicle = null;

        IsDirty = true;
        StatusText = $"Removed: {name}";
    }

    [RelayCommand]
    private void ImportVehicleFiles()
    {
        if (SelectedVehicle == null)
        {
            StatusText = "Select a vehicle first.";
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "Vehicle Files|*.yft;*.ytd;*.meta|YFT Models|*.yft|YTD Textures|*.ytd|Meta Files|*.meta|All Files|*.*",
            Title = "Import files for " + SelectedVehicle.Name,
            Multiselect = true
        };
        if (dialog.ShowDialog() != true) return;

        foreach (var file in dialog.FileNames)
        {
            var fileName = Path.GetFileName(file).ToLowerInvariant();
            var ext = Path.GetExtension(file).ToLowerInvariant();

            if (ext == ".yft")
            {
                if (fileName.Contains("_hi"))
                    SelectedVehicle.YftHiFilePath = file;
                else
                    SelectedVehicle.YftFilePath = file;
            }
            else if (ext == ".ytd")
            {
                if (fileName.Contains("+hi"))
                    SelectedVehicle.YtdHiFilePath = file;
                else
                    SelectedVehicle.YtdFilePath = file;
            }
            else if (ext == ".meta")
            {
                if (fileName.Contains("handling"))
                    SelectedVehicle.HandlingMetaPath = file;
                else if (fileName.Contains("vehicles"))
                    SelectedVehicle.VehiclesMetaPath = file;
                else if (fileName.Contains("carvariations"))
                    SelectedVehicle.CarVariationsMetaPath = file;
                else if (fileName.Contains("carcols"))
                    SelectedVehicle.CarColsMetaPath = file;
                else if (fileName.Contains("vehiclelayouts"))
                    SelectedVehicle.VehicleLayoutsMetaPath = file;
            }
        }

        // Auto-detect vehicle name from YFT filename
        if (!string.IsNullOrEmpty(SelectedVehicle.YftFilePath))
        {
            var modelName = Path.GetFileNameWithoutExtension(SelectedVehicle.YftFilePath);
            if (SelectedVehicle.Name.StartsWith("Vehicle "))
                SelectedVehicle.Name = modelName;
        }

        IsDirty = true;
        LoadVehicleIntoEditors(SelectedVehicle);
        StatusText = $"Imported {dialog.FileNames.Length} file(s) for {SelectedVehicle.Name}";
    }

    #endregion

    #region Project Commands

    [RelayCommand]
    private void NewProject()
    {
        _currentProject = _projectService.CreateNew();
        OnPropertyChanged(nameof(Vehicles));
        _exportVm.Vehicles = _currentProject.Vehicles;
        _exportVm.ResourceName = "";
        SelectedVehicle = null;
        VehicleName = "No Vehicle";
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
            OnPropertyChanged(nameof(Vehicles));
            IsDirty = false;
            TitleSuffix = $" - {Path.GetFileName(path)}";
            _projectService.AddToRecentProjects(path);
            LoadRecentProjects();

            // Select first vehicle if available
            if (_currentProject.Vehicles.Count > 0)
                SelectedVehicle = _currentProject.Vehicles[0];
            else
                SelectedVehicle = null;

            // Populate export view
            _exportVm.Vehicles = _currentProject.Vehicles;
            _exportVm.ResourceName = _currentProject.ProjectName;

            StatusText = $"Opened: {Path.GetFileName(path)} ({_currentProject.Vehicles.Count} vehicle(s))";
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
