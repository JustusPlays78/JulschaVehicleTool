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

    #region Project Commands

    private void OnCreateProjectRequested(string folderPath)
    {
        Log($"[CreateProject] Folder: {folderPath}");
        try
        {
            _currentProject = _projectService.CreateNew(folderPath);
            _projectService.AddToRecentProjects(folderPath);
            OpenProjectInternal();
            StatusText = $"Created new project: {_currentProject.Name}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error creating project: {ex.Message}";
        }
    }

    private void OnOpenProjectRequested(string folderPath)
    {
        Log($"[OpenProject] Folder: {folderPath}");
        try
        {
            _currentProject = _projectService.Open(folderPath);
            _projectService.AddToRecentProjects(folderPath);
            OpenProjectInternal();
            StatusText = $"Opened: {_currentProject.Name} ({_currentProject.Resources.Count} resource(s))";
        }
        catch (Exception ex)
        {
            StatusText = $"Error opening project: {ex.Message}";
        }
    }

    private void OpenProjectInternal()
    {
        Log($"[OpenInternal] Project: {_currentProject!.Name}, Resources: {_currentProject.Resources.Count}, Vehicles: {_currentProject.Resources.Sum(r => r.Vehicles.Count)}");
        IsProjectOpen = true;
        TitleSuffix = $" - {_currentProject!.Name}";
        _modelCache.Clear();

        // Set up project-level dirty tracking for the siren pool
        SubscribeProjectCarColsDirtyTracking(_currentProject);

        RebuildTree();
        _autoSaveTimer.Start();

        // Select first vehicle if available
        if (TreeNodes.Count > 0 && TreeNodes[0].Children.Count > 0)
        {
            var firstResource = TreeNodes[0].Children[0];
            if (firstResource.Children.Count > 0)
                SelectedTreeNode = firstResource.Children[0];
            else
                SelectedTreeNode = firstResource;
        }
    }

    private static System.Windows.Window? Owner
        => System.Windows.Application.Current.MainWindow;

    [RelayCommand]
    private void NewProject()
    {
        var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
        {
            Description = "Select folder for new project",
            UseDescriptionForTitle = true,
        };
        if (dialog.ShowDialog(Owner) == true)
            OnCreateProjectRequested(dialog.SelectedPath);
    }

    [RelayCommand]
    private void OpenProject()
    {
        var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
        {
            Description = "Open project folder",
            UseDescriptionForTitle = true,
        };
        if (dialog.ShowDialog(Owner) == true)
            OnOpenProjectRequested(dialog.SelectedPath);
    }

    [RelayCommand]
    private void SaveProject()
    {
        if (_currentProject == null) return;

        Log($"[Save] Saving project: {_currentProject.Name}, Path: {_currentProject.FolderPath}");
        Log($"[Save] Resources: {_currentProject.Resources.Count}");
        foreach (var res in _currentProject.Resources)
        {
            Log($"[Save]   Resource '{res.Name}': {res.Vehicles.Count} vehicle(s)");
            foreach (var v in res.Vehicles)
            {
                Log($"[Save]     Vehicle '{v.Name}': YFT={v.YftRelativePath ?? "null"}, Handling={v.Handling?.HandlingName ?? "null"}, Meta={v.VehicleMeta?.GameName ?? "null"}, CarVar={v.CarVariation?.ModelName ?? "null"}, SirenId={v.CarVariation?.SirenSettings}");
            }
        }
        Log($"[Save] Project siren pool: {_currentProject.CarCols.SirenSettings.Count} group(s)");

        try
        {
            _projectService.Save(_currentProject);
            _currentProject.IsDirty = false;
            AutoSaveIndicator = $"Saved {DateTime.Now:HH:mm}";
            Log("[Save] OK");
            StatusText = $"Project saved";
        }
        catch (Exception ex)
        {
            Log($"[Save] ERROR: {ex.Message}");
            StatusText = $"Error saving: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SaveProjectAs()
    {
        if (_currentProject == null) return;

        var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
        {
            Description = "Save project to folder",
            UseDescriptionForTitle = true,
        };
        if (dialog.ShowDialog(Owner) != true) return;

        try
        {
            _projectService.SaveAs(_currentProject, dialog.SelectedPath);
            _projectService.AddToRecentProjects(dialog.SelectedPath);
            _currentProject.IsDirty = false;
            TitleSuffix = $" - {_currentProject.Name}";
            StatusText = $"Project saved as: {dialog.SelectedPath}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error saving: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CloseProject()
    {
        if (_currentProject != null && _currentProject.IsDirty)
            SaveProject();

        UnsubscribeDirtyTracking();
        UnsubscribeProjectCarColsDirtyTracking();
        _currentProject = null;
        _autoSaveTimer.Stop();
        IsProjectOpen = false;
        IsVehicleSelected = false;
        TreeNodes.Clear();
        _modelCache.Clear();
        TitleSuffix = "";
        StatusText = "Ready";

        _welcomeVm.RefreshRecentProjects();
        CurrentViewModel = _welcomeVm;
    }

    private void AutoSave()
    {
        if (_currentProject == null || !_currentProject.IsDirty) return;

        Log("[AutoSave] Triggering auto-save...");
        try
        {
            _projectService.Save(_currentProject);
            _currentProject.IsDirty = false;
            AutoSaveIndicator = $"Auto-saved {DateTime.Now:HH:mm}";
            Log("[AutoSave] OK");
        }
        catch (Exception ex)
        {
            Log($"[AutoSave] ERROR: {ex.Message}");
        }
    }

    #endregion

    #region Rename

    [RelayCommand]
    private void RenameSelected()
    {
        if (SelectedTreeNode is ResourceTreeNode or VehicleTreeNode)
        {
            SelectedTreeNode.IsEditing = true;
        }
    }

    /// <summary>
    /// Called from code-behind when the user commits the rename (Enter or LostFocus).
    /// </summary>
    public void CommitRename(TreeNodeViewModel node, string newName)
    {
        node.IsEditing = false;

        var trimmed = newName.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        // Compare against model name (not DisplayName, which binding already updated)
        switch (node)
        {
            case ResourceTreeNode rn:
                if (trimmed == rn.Resource.Name) return;
                rn.Resource.Name = trimmed;
                rn.DisplayName = trimmed;
                Log($"[Rename] Resource → '{trimmed}'");
                break;
            case VehicleTreeNode vn:
                if (trimmed == vn.Vehicle.Name) return;
                var oldVehicleName = vn.Vehicle.Name;
                vn.Vehicle.Name = trimmed;
                vn.DisplayName = trimmed;
                if (_currentProject != null)
                    _projectService.RenameVehicle(_currentProject, vn.Vehicle, oldVehicleName, trimmed);
                _modelCache.Clear();
                Log($"[Rename] Vehicle '{oldVehicleName}' → '{trimmed}'");
                break;
            default:
                return;
        }

        if (_currentProject != null)
        {
            _currentProject.IsDirty = true;
            SaveProject();
        }
        StatusText = $"Renamed to: {trimmed}";
    }

    /// <summary>
    /// Called from code-behind when the user cancels the rename (Escape).
    /// </summary>
    public void CancelRename(TreeNodeViewModel node)
    {
        node.IsEditing = false;

        // Restore DisplayName from model (binding may have changed it)
        switch (node)
        {
            case ResourceTreeNode rn:
                rn.DisplayName = rn.Resource.Name;
                break;
            case VehicleTreeNode vn:
                vn.DisplayName = vn.Vehicle.Name;
                break;
        }
    }

    #endregion

    #region Resource Management

    [RelayCommand]
    private void AddResource()
    {
        if (_currentProject == null) return;

        var resource = new Resource { Name = $"resource_{_currentProject.Resources.Count + 1}" };
        _currentProject.Resources.Add(resource);
        _currentProject.IsDirty = true;
        RebuildTree();

        // Select the new resource
        var projectNode = TreeNodes.FirstOrDefault();
        if (projectNode != null)
            SelectedTreeNode = projectNode.Children.LastOrDefault();

        SaveProject(); // persist immediately
        StatusText = $"Added resource: {resource.Name}";
    }

    [RelayCommand]
    private void RemoveResource()
    {
        if (_currentProject == null || SelectedTreeNode is not ResourceTreeNode resourceNode) return;

        var result = System.Windows.MessageBox.Show(
            $"Delete resource \"{resourceNode.Resource.Name}\" and all its vehicles?",
            "Confirm Delete", System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        _currentProject.Resources.Remove(resourceNode.Resource);
        _currentProject.IsDirty = true;
        RebuildTree();
        StatusText = $"Removed resource: {resourceNode.Resource.Name}";
    }

    #endregion

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

        var vehicle = VehicleDefaults.CreateDefault($"vehicle_{resource.Vehicles.Count + 1}");
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

        var clone = _projectService.CloneVehicle(vehicleNode.Vehicle);
        clone.Name = GenerateUniqueName(vehicleNode.Vehicle.Name, resource);

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
    private void ImportVehicleFolder()
    {
        Resource? resource = SelectedTreeNode switch
        {
            ResourceTreeNode rn => rn.Resource,
            VehicleTreeNode vn => FindResourceForVehicle(vn.Vehicle),
            _ => null
        };

        if (_currentProject == null || resource == null)
        {
            StatusText = "Select a resource first.";
            return;
        }

        var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
        {
            Description = "Select vehicle folder to import",
            UseDescriptionForTitle = true,
        };
        if (dialog.ShowDialog(Owner) != true) return;

        try
        {
            IsProgressVisible = true;
            var progress = new Progress<(int current, int total, string message)>(p =>
            {
                ProgressValue = p.current;
                ProgressMax = p.total;
                ProgressMessage = p.message;
            });

            _projectService.ImportVehicleFolder(_currentProject, resource, dialog.SelectedPath, progress);
            _currentProject.IsDirty = true;
            RebuildTree();
            SaveProject(); // persist immediately

            // Select the first imported vehicle so editors and dirty tracking are initialized
            var firstImported = resource.Vehicles.LastOrDefault();
            if (firstImported != null)
                SelectVehicleInTree(firstImported); // triggers OnSelectedTreeNodeChanged → LoadVehicleIntoEditors

            StatusText = $"Imported vehicle(s) from: {Path.GetFileName(dialog.SelectedPath)}";
        }
        catch (Exception ex)
        {
            StatusText = $"Import error: {ex.Message}";
        }
        finally
        {
            IsProgressVisible = false;
        }
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
        StatusText = $"Imported {applied} assignment(s) from {dialog.FileNames.Length} file(s)";
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
            if (match.MatchedVehicle != null)
            {
                assign(match.MatchedVehicle, match.Data);
                Log($"  [{metaType}] Matched '{match.ParsedName}' → {match.MatchedVehicle.Name} (confidence: {match.Confidence})");
                count++;
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
            _exportService.ExportSingleMeta(vehicleNode.Vehicle, metaType, dialog.FileName, _metaXmlService);
            StatusText = $"Exported {metaType}.meta";
        }
        catch (Exception ex)
        {
            StatusText = $"Export error: {ex.Message}";
        }
    }

    #endregion

    #region Handling Presets

    [RelayCommand]
    private void ApplyHandlingPreset(string? presetName)
    {
        if (string.IsNullOrEmpty(presetName)) return;

        var preset = HandlingPresets.All.FirstOrDefault(p => p.Name == presetName);
        if (preset.Create == null) return;

        // Collect checked vehicles; fall back to the currently selected vehicle
        var targets = GetAllVehicleNodes()
            .Where(n => n.IsChecked)
            .Select(n => n.Vehicle)
            .ToList();

        if (targets.Count == 0)
        {
            if (SelectedTreeNode is not VehicleTreeNode vn) return;
            targets.Add(vn.Vehicle);
        }

        foreach (var vehicle in targets)
            vehicle.Handling = preset.Create();

        _currentProject!.IsDirty = true;

        // Reload current vehicle editors + re-subscribe dirty tracking
        if (SelectedTreeNode is VehicleTreeNode selected && targets.Contains(selected.Vehicle))
            LoadVehicleIntoEditors(selected.Vehicle);

        SelectedTab = TabItems[1];
        SaveProject();
        StatusText = $"Preset '{presetName}' applied to {targets.Count} vehicle(s)";
    }

    #endregion

    #region ZIP Export/Import

    [RelayCommand]
    private async Task ExportProjectZipAsync()
    {
        if (_currentProject == null) return;

        var dialog = new SaveFileDialog
        {
            Filter = "ZIP Archive|*.zip",
            FileName = $"{_currentProject.Name}.zip",
            Title = "Export project as ZIP"
        };
        if (dialog.ShowDialog(Owner) != true) return;

        var pwdDlg = new PasswordDialog("Enter a password to protect the ZIP export:", confirm: true) { Owner = Owner };
        if (pwdDlg.ShowDialog() != true || string.IsNullOrEmpty(pwdDlg.Password)) return;
        var password = pwdDlg.Password;

        try
        {
            IsProgressVisible = true;
            var progress = new Progress<(int current, int total, string message)>(p =>
            {
                ProgressValue = p.current;
                ProgressMax = p.total;
                ProgressMessage = p.message;
            });

            await Task.Run(() => _projectService.ExportProjectZip(_currentProject, dialog.FileName, password, progress));
            StatusText = $"Project exported: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusText = $"Export error: {ex.Message}";
        }
        finally
        {
            IsProgressVisible = false;
        }
    }

    [RelayCommand]
    private async Task ImportProjectZipAsync()
    {
        var openDialog = new OpenFileDialog
        {
            Filter = "ZIP Archive|*.zip",
            Title = "Import project from ZIP"
        };
        if (openDialog.ShowDialog(Owner) != true) return;

        var folderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
        {
            Description = "Select target folder for imported project",
            UseDescriptionForTitle = true,
        };
        if (folderDialog.ShowDialog(Owner) != true) return;

        var pwdDlg = new PasswordDialog("Enter the ZIP archive password:") { Owner = Owner };
        if (pwdDlg.ShowDialog() != true || string.IsNullOrEmpty(pwdDlg.Password)) return;
        var password = pwdDlg.Password;

        try
        {
            IsProgressVisible = true;
            var progress = new Progress<(int current, int total, string message)>(p =>
            {
                ProgressValue = p.current;
                ProgressMax = p.total;
                ProgressMessage = p.message;
            });

            _currentProject = await Task.Run(() =>
                _projectService.ImportProjectZip(openDialog.FileName, password, folderDialog.SelectedPath, progress));

            _projectService.AddToRecentProjects(folderDialog.SelectedPath);
            OpenProjectInternal();
            StatusText = $"Imported project: {_currentProject.Name}";
        }
        catch (Exception ex)
        {
            StatusText = $"Import error: {ex.Message}";
        }
        finally
        {
            IsProgressVisible = false;
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
    private static string GenerateUniqueName(string baseName, Resource resource)
    {
        var existing = new HashSet<string>(resource.Vehicles.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);
        var candidate = baseName + "_copy";
        if (!existing.Contains(candidate)) return candidate;
        int n = 2;
        while (existing.Contains(baseName + "_copy_" + n)) n++;
        return baseName + "_copy_" + n;
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

public class InvertBoolConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => value is bool b && !b;
}
