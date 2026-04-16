using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using JulschaVehicleTool.Core.Models;

namespace JulschaVehicleTool.App.ViewModels;

public partial class TreeNodeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _displayName = "";

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _hasWarning;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private bool _isChecked;

    public ObservableCollection<TreeNodeViewModel> Children { get; } = new();
}

public partial class ProjectTreeNode : TreeNodeViewModel
{
    public Project Project { get; }

    public ProjectTreeNode(Project project)
    {
        Project = project;
        DisplayName = project.Name;
    }
}

public partial class ResourceTreeNode : TreeNodeViewModel
{
    public Resource Resource { get; }

    public int VehicleCount => Resource.Vehicles.Count;

    public ResourceTreeNode(Resource resource)
    {
        Resource = resource;
        DisplayName = resource.Name;
    }
}

public partial class VehicleTreeNode : TreeNodeViewModel
{
    public Vehicle Vehicle { get; }

    [ObservableProperty]
    private string _warningMessage = "";

    public VehicleTreeNode(Vehicle vehicle, string? projectFolderPath = null)
    {
        Vehicle = vehicle;
        DisplayName = vehicle.Name;
        UpdateWarningState(projectFolderPath);
    }

    public void UpdateWarningState(string? projectFolderPath)
    {
        var issues = new System.Collections.Generic.List<string>();

        if (Vehicle.HasMissingFiles(projectFolderPath))
            issues.Add("Missing .yft or .ytd files");
        if (Vehicle.VehicleMeta == null)
            issues.Add("No vehicles.meta data");
        else if (string.IsNullOrEmpty(Vehicle.VehicleMeta.ModelName))
            issues.Add("Model name is empty");
        if (Vehicle.Handling == null)
            issues.Add("No handling.meta data");

        HasWarning = issues.Count > 0;
        WarningMessage = issues.Count > 0
            ? "Warnings:\n• " + string.Join("\n• ", issues)
            : "";
    }
}
