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

public partial class MainWindowViewModel : ObservableObject
{
    // Services
    private readonly IProjectService _projectService;
    private readonly MetaXmlService _metaXmlService;
    private readonly FiveMExportService _exportService;
    private readonly MetaImportMatchService _matchService;
    private readonly BinaryFileService _binaryFileService;
    private readonly MeshConversionService _meshConversionService;

    // Child ViewModels
    private readonly ModelViewerViewModel _modelViewerVm;
    private readonly HandlingEditorViewModel _handlingEditorVm;
    private readonly CarVariationsViewModel _carVariationsVm;
    private readonly SirenEditorViewModel _sirenEditorVm;
    private readonly VehicleSirenAssignViewModel _vehicleSirenAssignVm;
    private readonly VehicleMetaViewModel _vehicleMetaVm;
    private readonly ResourceSettingsViewModel _resourceSettingsVm;
    private readonly WelcomeViewModel _welcomeVm;

    // Auto-save timer
    private readonly DispatcherTimer _autoSaveTimer;
    private readonly List<string> _skippedPartialMatches = new();

    // Model cache for 3D viewer
    private readonly Dictionary<string, VehicleModelData> _modelCache = new();

    // Dirty tracking — subscribed data objects (per-vehicle, cleared on vehicle switch)
    private readonly List<INotifyPropertyChanged> _dirtySubscriptions = new();
    private readonly List<System.Collections.Specialized.INotifyCollectionChanged> _dirtyCollectionSubscriptions = new();

    // Dirty tracking — project-level (set up on project open, cleared on project close)
    private readonly List<System.Collections.Specialized.INotifyCollectionChanged> _projectDirtyCollections = new();

    // Flag to suppress selection-change logic during tree rebuild
    private bool _isRebuildingTree;

    // State
    private Project? _currentProject;

    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private TreeNodeViewModel? _selectedTreeNode;

    [ObservableProperty]
    private NavigationItem? _selectedTab;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private StatusSeverity _statusSeverity = StatusSeverity.Info;

    partial void OnStatusTextChanged(string value)
    {
        // Auto-detect severity from message content so all 30+ call sites stay clean
        StatusSeverity = value switch
        {
            var s when s.StartsWith("Error", StringComparison.OrdinalIgnoreCase)
                    || s.StartsWith("Import error", StringComparison.OrdinalIgnoreCase)
                    || s.StartsWith("Export error", StringComparison.OrdinalIgnoreCase) => StatusSeverity.Error,
            var s when s.Contains("saved", StringComparison.OrdinalIgnoreCase)
                    || s.StartsWith("Created", StringComparison.OrdinalIgnoreCase)
                    || s.StartsWith("Imported", StringComparison.OrdinalIgnoreCase)
                    || s.StartsWith("Exported", StringComparison.OrdinalIgnoreCase)
                    || s.StartsWith("Cloned", StringComparison.OrdinalIgnoreCase)
                    || s.StartsWith("Preset", StringComparison.OrdinalIgnoreCase)
                    || s.StartsWith("Renamed", StringComparison.OrdinalIgnoreCase)
                    || s.StartsWith("Added", StringComparison.OrdinalIgnoreCase) => StatusSeverity.Success,
            var s when s.StartsWith("Select", StringComparison.OrdinalIgnoreCase)
                    || s.StartsWith("No ", StringComparison.OrdinalIgnoreCase) => StatusSeverity.Warning,
            _ => StatusSeverity.Info
        };
    }

    [ObservableProperty]
    private string _titleSuffix = "";

    [ObservableProperty]
    private bool _isProjectOpen;

    [ObservableProperty]
    private bool _isVehicleSelected;

    [ObservableProperty]
    private int _progressValue;

    [ObservableProperty]
    private int _progressMax;

    [ObservableProperty]
    private string _progressMessage = "";

    [ObservableProperty]
    private bool _isProgressVisible;

    [ObservableProperty]
    private string _autoSaveIndicator = "";

    [ObservableProperty]
    private string _filterText = "";

    partial void OnFilterTextChanged(string value) => ApplyTreeFilter(value);

    [RelayCommand]
    private void ClearFilter() => FilterText = "";

    public ObservableCollection<TreeNodeViewModel> TreeNodes { get; } = new();
    public ObservableCollection<NavigationItem> TabItems { get; } = new();
    public string[] HandlingPresetNames => HandlingPresets.All.Select(p => p.Name).ToArray();

    public MainWindowViewModel(
        IProjectService projectService,
        MetaXmlService metaXmlService,
        FiveMExportService exportService,
        MetaImportMatchService matchService,
        BinaryFileService binaryFileService,
        MeshConversionService meshConversionService,
        ModelViewerViewModel modelViewerVm,
        HandlingEditorViewModel handlingEditorVm,
        CarVariationsViewModel carVariationsVm,
        SirenEditorViewModel sirenEditorVm,
        VehicleSirenAssignViewModel vehicleSirenAssignVm,
        VehicleMetaViewModel vehicleMetaVm,
        ResourceSettingsViewModel resourceSettingsVm,
        WelcomeViewModel welcomeVm)
    {
        _projectService = projectService;
        _metaXmlService = metaXmlService;
        _exportService = exportService;
        _matchService = matchService;
        _binaryFileService = binaryFileService;
        _meshConversionService = meshConversionService;
        _modelViewerVm = modelViewerVm;
        _handlingEditorVm = handlingEditorVm;
        _carVariationsVm = carVariationsVm;
        _sirenEditorVm = sirenEditorVm;
        _vehicleSirenAssignVm = vehicleSirenAssignVm;
        _vehicleMetaVm = vehicleMetaVm;
        _resourceSettingsVm = resourceSettingsVm;
        _welcomeVm = welcomeVm;

        // Tab items for vehicle editing
        TabItems.Add(new NavigationItem("3D Viewer", "Monitor", nameof(ModelViewerViewModel)));
        TabItems.Add(new NavigationItem("Handling", "Gauge", nameof(HandlingEditorViewModel)));
        TabItems.Add(new NavigationItem("Variations", "Palette", nameof(CarVariationsViewModel)));
        TabItems.Add(new NavigationItem("Sirens", "Lightbulb", nameof(VehicleSirenAssignViewModel)));
        TabItems.Add(new NavigationItem("Vehicle", "FileDocument", nameof(VehicleMetaViewModel)));

        SelectedTab = TabItems[0];

        // Welcome screen events
        _welcomeVm.CreateProjectRequested += OnCreateProjectRequested;
        _welcomeVm.OpenProjectRequested += OnOpenProjectRequested;

        // Auto-save timer (5 minutes)
        _autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(5)
        };
        _autoSaveTimer.Tick += (_, _) => AutoSave();

        // Start on welcome screen
        CurrentViewModel = _welcomeVm;
    }

    #region Debug Logging

    private static void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        Console.WriteLine($"[{timestamp}] {message}");
        System.Diagnostics.Debug.WriteLine($"[{timestamp}] {message}");
    }

    #endregion

    #region Tree Selection

    partial void OnSelectedTreeNodeChanged(TreeNodeViewModel? value)
    {
        Log($"[TreeSelect] Node: {value?.DisplayName ?? "null"} (type: {value?.GetType().Name ?? "null"}), rebuilding={_isRebuildingTree}");

        // Skip during tree rebuild — SelectVehicleInTree will handle selection
        if (_isRebuildingTree) return;

        // Auto-save when switching away from a vehicle
        AutoSave();

        switch (value)
        {
            case VehicleTreeNode vehicleNode:
                IsVehicleSelected = true;
                LoadVehicleIntoEditors(vehicleNode.Vehicle);
                OnSelectedTabChanged(SelectedTab);
                break;

            case ResourceTreeNode resourceNode:
                IsVehicleSelected = false;
                _resourceSettingsVm.Load(resourceNode.Resource, _currentProject!);
                CurrentViewModel = _resourceSettingsVm;
                break;

            case ProjectTreeNode:
                IsVehicleSelected = false;
                // Show the project-level siren pool editor
                if (_currentProject != null)
                {
                    _sirenEditorVm.CarCols = _currentProject.CarCols;
                    _sirenEditorVm.Project = _currentProject;
                    _sirenEditorVm.IsLoaded = true;
                    _sirenEditorVm.StatusMessage = $"Project siren pool — {_currentProject.CarCols.SirenSettings.Count} group(s)";
                    if (_sirenEditorVm.SelectedSiren == null && _currentProject.CarCols.SirenSettings.Count > 0)
                        _sirenEditorVm.SelectedSiren = _currentProject.CarCols.SirenSettings[0];
                }
                CurrentViewModel = _sirenEditorVm;
                break;

            default:
                IsVehicleSelected = false;
                CurrentViewModel = null;
                break;
        }
    }

    partial void OnSelectedTabChanged(NavigationItem? value)
    {
        if (!IsVehicleSelected || value == null) return;

        CurrentViewModel = value.ViewModelKey switch
        {
            nameof(ModelViewerViewModel) => _modelViewerVm,
            nameof(HandlingEditorViewModel) => _handlingEditorVm,
            nameof(CarVariationsViewModel) => _carVariationsVm,
            nameof(VehicleSirenAssignViewModel) => _vehicleSirenAssignVm,
            nameof(VehicleMetaViewModel) => _vehicleMetaVm,
            _ => null
        };
    }

    #endregion

    #region Vehicle Loading

    private void UnsubscribeDirtyTracking()
    {
        foreach (var obj in _dirtySubscriptions)
            obj.PropertyChanged -= OnDataPropertyChanged;
        _dirtySubscriptions.Clear();

        foreach (var col in _dirtyCollectionSubscriptions)
            col.CollectionChanged -= OnCollectionChanged;
        _dirtyCollectionSubscriptions.Clear();
    }

    private void SubscribeProjectCarColsDirtyTracking(Project project)
    {
        _projectDirtyCollections.Add(project.CarCols.SirenSettings);
        project.CarCols.SirenSettings.CollectionChanged += OnCollectionChanged;
    }

    private void UnsubscribeProjectCarColsDirtyTracking()
    {
        foreach (var col in _projectDirtyCollections)
            col.CollectionChanged -= OnCollectionChanged;
        _projectDirtyCollections.Clear();
    }

    private void SubscribeDirtyTracking(INotifyPropertyChanged? obj)
    {
        if (obj == null) return;
        obj.PropertyChanged += OnDataPropertyChanged;
        _dirtySubscriptions.Add(obj);
    }

    private void SubscribeDirtyTrackingCollection(System.Collections.Specialized.INotifyCollectionChanged? col)
    {
        if (col == null) return;
        col.CollectionChanged += OnCollectionChanged;
        _dirtyCollectionSubscriptions.Add(col);
    }

    private void OnDataPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_currentProject != null)
            _currentProject.IsDirty = true;
    }

    private void OnCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (_currentProject != null)
            _currentProject.IsDirty = true;
    }

    private void LoadVehicleIntoEditors(Vehicle vehicle)
    {
        UnsubscribeDirtyTracking();
        // Handling
        if (vehicle.Handling != null)
        {
            _handlingEditorVm.Handling = vehicle.Handling;
            _handlingEditorVm.IsLoaded = true;
            _handlingEditorVm.StatusMessage = $"Editing: {vehicle.Name}";
        }
        else
        {
            _handlingEditorVm.Handling = null;
            _handlingEditorVm.IsLoaded = false;
            _handlingEditorVm.StatusMessage = "No handling data";
        }

        // Car Variations
        if (vehicle.CarVariation != null)
        {
            _carVariationsVm.Variation = vehicle.CarVariation;
            _carVariationsVm.IsLoaded = true;
            _carVariationsVm.StatusMessage = $"Editing: {vehicle.Name}";
        }
        else
        {
            _carVariationsVm.Variation = null;
            _carVariationsVm.IsLoaded = false;
            _carVariationsVm.StatusMessage = "No car variation data";
        }

        // Sirens — vehicle siren assignment (references project-level pool by ID)
        _vehicleSirenAssignVm.ProjectCarCols = _currentProject?.CarCols;
        _vehicleSirenAssignVm.Vehicle = vehicle;

        // Vehicle Meta
        if (vehicle.VehicleMeta != null)
        {
            _vehicleMetaVm.Vehicle = vehicle.VehicleMeta;
            _vehicleMetaVm.IsLoaded = true;
            _vehicleMetaVm.StatusMessage = $"Editing: {vehicle.Name}";
        }
        else
        {
            _vehicleMetaVm.Vehicle = null;
            _vehicleMetaVm.IsLoaded = false;
            _vehicleMetaVm.StatusMessage = "No vehicle meta data";
        }

        // Subscribe to property changes for dirty tracking
        SubscribeDirtyTracking(vehicle.Handling);
        SubscribeDirtyTracking(vehicle.CarVariation);
        SubscribeDirtyTracking(vehicle.VehicleMeta);
        SubscribeDirtyTracking(vehicle);

        // Subscribe to collection changes (add/remove color combinations)
        SubscribeDirtyTrackingCollection(vehicle.CarVariation?.Colors);

        // 3D Viewer - load from encrypted project files
        LoadModelForVehicle(vehicle);

        StatusText = $"Vehicle: {vehicle.Name}";
    }

    private void LoadModelForVehicle(Vehicle vehicle)
    {
        if (_currentProject == null || vehicle.YftRelativePath == null) return;

        var cacheKey = $"{vehicle.Name}:{vehicle.YftRelativePath}";
        if (_modelCache.TryGetValue(cacheKey, out var cached))
        {
            _modelViewerVm.LoadFromModelData(cached);
            return;
        }

        try
        {
            var yftBytes = _projectService.DecryptVehicleFile(_currentProject, vehicle, vehicle.YftRelativePath);
            var yft = _binaryFileService.LoadYftFromBytes(yftBytes);

            CodeWalker.GameFiles.YtdFile? ytd = null;
            if (vehicle.YtdRelativePath != null)
            {
                var ytdBytes = _projectService.DecryptVehicleFile(_currentProject, vehicle, vehicle.YtdRelativePath);
                ytd = _binaryFileService.LoadYtdFromBytes(ytdBytes);
            }

            var modelData = _meshConversionService.ConvertYft(yft, ytd);
            _modelCache[cacheKey] = modelData;
            _modelViewerVm.LoadFromModelData(modelData);
        }
        catch (Exception ex)
        {
            _modelViewerVm.StatusMessage = $"Error loading 3D model: {ex.Message}";
            _modelViewerVm.IsLoaded = false;
        }
    }

    #endregion

    #region Tree Building

    private void RebuildTree()
    {
        _isRebuildingTree = true;
        try
        {
            TreeNodes.Clear();
            if (_currentProject == null) return;

            var projectNode = new ProjectTreeNode(_currentProject);

            foreach (var resource in _currentProject.Resources)
            {
                var resourceNode = new ResourceTreeNode(resource);
                foreach (var vehicle in resource.Vehicles)
                {
                    resourceNode.Children.Add(new VehicleTreeNode(vehicle, _currentProject.FolderPath));
                }
                projectNode.Children.Add(resourceNode);
            }

            TreeNodes.Add(projectNode);
        }
        finally
        {
            _isRebuildingTree = false;
        }

        ApplyTreeFilter(FilterText);
    }

    private Resource? FindResourceForVehicle(Vehicle vehicle)
    {
        return _currentProject?.Resources.FirstOrDefault(r => r.Vehicles.Contains(vehicle));
    }

    private void SelectVehicleInTree(Vehicle vehicle)
    {
        foreach (var projectNode in TreeNodes)
        {
            foreach (var resourceNode in projectNode.Children)
            {
                foreach (var vehicleNode in resourceNode.Children)
                {
                    if (vehicleNode is VehicleTreeNode vn && vn.Vehicle == vehicle)
                    {
                        SelectedTreeNode = vn;
                        return;
                    }
                }
            }
        }
    }

    private void ApplyTreeFilter(string searchText)
    {
        var q = searchText.Trim().ToLowerInvariant();
        foreach (var projectNode in TreeNodes)
        {
            bool anyResource = false;
            foreach (var resourceNode in projectNode.Children)
            {
                bool anyVehicle = false;
                foreach (var vehicleNode in resourceNode.Children)
                {
                    var v = string.IsNullOrEmpty(q) || vehicleNode.DisplayName.ToLowerInvariant().Contains(q);
                    vehicleNode.IsVisible = v;
                    if (v) anyVehicle = true;
                }
                var r = string.IsNullOrEmpty(q)
                    || resourceNode.DisplayName.ToLowerInvariant().Contains(q)
                    || anyVehicle;
                resourceNode.IsVisible = r;
                if (r) anyResource = true;
            }
            projectNode.IsVisible = string.IsNullOrEmpty(q) || anyResource;
        }
    }

    private IEnumerable<VehicleTreeNode> GetAllVehicleNodes()
    {
        foreach (var proj in TreeNodes)
            foreach (var res in proj.Children)
                foreach (var veh in res.Children)
                    if (veh is VehicleTreeNode vn) yield return vn;
    }

    /// <summary>
    /// Returns a unique vehicle name within the given resource.
    /// Tries: baseName_copy, baseName_copy_2, baseName_copy_3, ...
    /// </summary>
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

public class InvertBoolConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => value is bool b && !b;
}
