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

    public ResourceTreeNode(Resource resource)
    {
        Resource = resource;
        DisplayName = resource.Name;
    }
}

public partial class VehicleTreeNode : TreeNodeViewModel
{
    public Vehicle Vehicle { get; }

    public VehicleTreeNode(Vehicle vehicle, string? projectFolderPath = null)
    {
        Vehicle = vehicle;
        DisplayName = vehicle.Name;
        HasWarning = vehicle.HasMissingFiles(projectFolderPath);
    }

    public void UpdateWarningState(string? projectFolderPath)
    {
        HasWarning = Vehicle.HasMissingFiles(projectFolderPath);
    }
}
