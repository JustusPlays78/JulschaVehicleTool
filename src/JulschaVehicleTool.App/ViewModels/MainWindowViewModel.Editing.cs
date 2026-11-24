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

// Inline rename, resources and handling presets.
public partial class MainWindowViewModel
{
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
                try
                {
                    // Renames folder + files + meta names; throws (and rolls back) on conflicts
                    if (_currentProject != null)
                        _projectService.RenameVehicle(_currentProject, vn.Vehicle, oldVehicleName, trimmed);
                    else
                        vn.Vehicle.Name = trimmed;
                }
                catch (InvalidOperationException ex)
                {
                    vn.DisplayName = oldVehicleName;
                    StatusText = $"Rename failed: {ex.Message}";
                    System.Windows.MessageBox.Show(ex.Message, "Rename failed",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                vn.DisplayName = trimmed;
                _modelCache.Clear();
                LoadVehicleIntoEditors(vn.Vehicle); // meta names changed
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
}
