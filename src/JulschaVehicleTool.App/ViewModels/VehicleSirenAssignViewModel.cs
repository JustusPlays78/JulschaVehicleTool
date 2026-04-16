using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JulschaVehicleTool.Core.Models;

namespace JulschaVehicleTool.App.ViewModels;

public partial class VehicleSirenAssignViewModel : ObservableObject
{
    [ObservableProperty] private CarColsData? _projectCarCols;
    [ObservableProperty] private Vehicle? _vehicle;
    [ObservableProperty] private SirenSetting? _selectedSirenSetting;
    [ObservableProperty] private bool _hasNoSirenGroups = true;
    [ObservableProperty] private string _statusMessage = "No vehicle selected";

    partial void OnProjectCarColsChanged(CarColsData? value)
    {
        HasNoSirenGroups = value == null || value.SirenSettings.Count == 0;
        UpdateSelectedFromVehicle();
    }

    partial void OnVehicleChanged(Vehicle? value)
    {
        HasNoSirenGroups = ProjectCarCols == null || ProjectCarCols.SirenSettings.Count == 0;
        UpdateSelectedFromVehicle();
        StatusMessage = value != null ? $"Siren assignment: {value.Name}" : "No vehicle selected";
    }

    partial void OnSelectedSirenSettingChanged(SirenSetting? value)
    {
        if (Vehicle?.CarVariation != null)
            Vehicle.CarVariation.SirenSettings = value?.Id ?? 0;
    }

    private void UpdateSelectedFromVehicle()
    {
        if (Vehicle?.CarVariation == null || ProjectCarCols == null)
        {
            SelectedSirenSetting = null;
            return;
        }

        var sirenId = Vehicle.CarVariation.SirenSettings;
        SelectedSirenSetting = sirenId == 0
            ? null
            : ProjectCarCols.SirenSettings.FirstOrDefault(s => s.Id == sirenId);
    }

    [RelayCommand]
    private void ClearAssignment()
    {
        SelectedSirenSetting = null;
        if (Vehicle?.CarVariation != null)
            Vehicle.CarVariation.SirenSettings = 0;
    }
}
