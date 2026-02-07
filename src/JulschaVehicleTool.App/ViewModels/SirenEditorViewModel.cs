using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JulschaVehicleTool.Core.Models;
using JulschaVehicleTool.Core.Services;
using Microsoft.Win32;

namespace JulschaVehicleTool.App.ViewModels;

public partial class SirenEditorViewModel : ObservableObject
{
    private readonly MetaXmlService _metaXmlService = new();

    [ObservableProperty] private string _statusMessage = "No carcols.meta loaded";
    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private CarColsData? _carCols;
    [ObservableProperty] private SirenSetting? _selectedSiren;
    [ObservableProperty] private SirenLight? _selectedLight;

    private string? _currentFilePath;

    [RelayCommand]
    private void LoadFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CarCols Meta|carcols.meta;*.meta|All Files|*.*",
            Title = "Open carcols.meta"
        };
        if (dialog.ShowDialog() == true) LoadFromPath(dialog.FileName);
    }

    public void LoadFromPath(string path)
    {
        try
        {
            CarCols = _metaXmlService.LoadCarCols(path);
            _currentFilePath = path;
            IsLoaded = CarCols != null;
            if (CarCols?.SirenSettings.Count > 0)
                SelectedSiren = CarCols.SirenSettings[0];
            StatusMessage = IsLoaded ? $"Loaded: {path}" : "Failed to parse carcols.meta";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            CarCols = null;
            SelectedSiren = null;
            SelectedLight = null;
            IsLoaded = false;
        }
    }

    [RelayCommand]
    private void SaveFile()
    {
        if (CarCols == null) return;
        var path = _currentFilePath;
        if (string.IsNullOrEmpty(path))
        {
            var dialog = new SaveFileDialog { Filter = "Meta|*.meta", FileName = "carcols.meta" };
            if (dialog.ShowDialog() != true) return;
            path = dialog.FileName;
        }
        try { _metaXmlService.SaveCarCols(CarCols, path); _currentFilePath = path; StatusMessage = $"Saved: {path}"; }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
    }

    [RelayCommand]
    private void NewFile()
    {
        CarCols = new CarColsData();
        _currentFilePath = null;
        IsLoaded = true;
        SelectedSiren = null;
        SelectedLight = null;
        StatusMessage = "New carcols.meta created";
    }

    [RelayCommand]
    private void AddSirenSetting()
    {
        if (CarCols == null) return;
        var id = CarCols.SirenSettings.Count > 0
            ? CarCols.SirenSettings.Max(s => s.Id) + 1
            : 1000;
        var setting = new SirenSetting { Id = id, Name = $"siren_{id}" };
        CarCols.SirenSettings.Add(setting);
        SelectedSiren = setting;
    }

    [RelayCommand]
    private void RemoveSirenSetting()
    {
        if (CarCols == null || SelectedSiren == null) return;
        var idx = CarCols.SirenSettings.IndexOf(SelectedSiren);
        CarCols.SirenSettings.Remove(SelectedSiren);
        if (CarCols.SirenSettings.Count > 0)
            SelectedSiren = CarCols.SirenSettings[Math.Max(0, idx - 1)];
        else
            SelectedSiren = null;
    }

    [RelayCommand]
    private void CloneSirenSetting()
    {
        if (CarCols == null || SelectedSiren == null) return;
        var src = SelectedSiren;
        var clone = new SirenSetting
        {
            Id = CarCols.SirenSettings.Max(s => s.Id) + 1,
            Name = src.Name + "_copy",
            TimeMultiplier = src.TimeMultiplier,
            LightFalloffMax = src.LightFalloffMax,
            LightFalloffExponent = src.LightFalloffExponent,
            LightInnerConeAngle = src.LightInnerConeAngle,
            LightOuterConeAngle = src.LightOuterConeAngle,
            LightOffset = src.LightOffset,
            TextureName = src.TextureName,
            SequencerBpm = src.SequencerBpm,
            UseRealLights = src.UseRealLights,
            LeftHeadLightSequencer = src.LeftHeadLightSequencer,
            RightHeadLightSequencer = src.RightHeadLightSequencer,
            LeftTailLightSequencer = src.LeftTailLightSequencer,
            RightTailLightSequencer = src.RightTailLightSequencer,
            LeftHeadLightMultiples = src.LeftHeadLightMultiples,
            RightHeadLightMultiples = src.RightHeadLightMultiples,
            LeftTailLightMultiples = src.LeftTailLightMultiples,
            RightTailLightMultiples = src.RightTailLightMultiples,
        };
        foreach (var light in src.Sirens)
        {
            clone.Sirens.Add(new SirenLight
            {
                Color = light.Color, Intensity = light.Intensity, LightGroup = light.LightGroup,
                Rotate = light.Rotate, Scale = light.Scale, Flash = light.Flash, Light = light.Light,
                SpotLight = light.SpotLight, CastShadows = light.CastShadows, ScaleFactor = light.ScaleFactor,
                RotationDelta = light.RotationDelta, RotationStart = light.RotationStart,
                RotationSpeed = light.RotationSpeed, RotationSequencer = light.RotationSequencer,
                RotationMultiples = light.RotationMultiples, RotationDirection = light.RotationDirection,
                RotationSyncToBpm = light.RotationSyncToBpm,
                FlashinessDelta = light.FlashinessDelta, FlashinessStart = light.FlashinessStart,
                FlashinessSpeed = light.FlashinessSpeed, FlashinessSequencer = light.FlashinessSequencer,
                FlashinessMultiples = light.FlashinessMultiples, FlashinessDirection = light.FlashinessDirection,
                FlashinessSyncToBpm = light.FlashinessSyncToBpm,
                CoronaIntensity = light.CoronaIntensity, CoronaSize = light.CoronaSize,
                CoronaPull = light.CoronaPull, CoronaFaceCamera = light.CoronaFaceCamera,
            });
        }
        CarCols.SirenSettings.Add(clone);
        SelectedSiren = clone;
    }

    [RelayCommand]
    private void AddSirenLight()
    {
        if (SelectedSiren == null || SelectedSiren.Sirens.Count >= 20) return;
        var light = new SirenLight();
        SelectedSiren.Sirens.Add(light);
        SelectedLight = light;
    }

    [RelayCommand]
    private void RemoveSirenLight(SirenLight? light)
    {
        if (SelectedSiren == null || light == null) return;
        var idx = SelectedSiren.Sirens.IndexOf(light);
        SelectedSiren.Sirens.Remove(light);
        if (SelectedSiren.Sirens.Count > 0)
            SelectedLight = SelectedSiren.Sirens[Math.Max(0, idx - 1)];
        else
            SelectedLight = null;
    }

    [RelayCommand]
    private void CloneSirenLight(SirenLight? src)
    {
        if (SelectedSiren == null || src == null || SelectedSiren.Sirens.Count >= 20) return;
        var clone = new SirenLight
        {
            Color = src.Color, Intensity = src.Intensity, LightGroup = src.LightGroup,
            Rotate = src.Rotate, Scale = src.Scale, Flash = src.Flash, Light = src.Light,
            SpotLight = src.SpotLight, CastShadows = src.CastShadows, ScaleFactor = src.ScaleFactor,
            RotationDelta = src.RotationDelta, RotationStart = src.RotationStart,
            RotationSpeed = src.RotationSpeed, RotationSequencer = src.RotationSequencer,
            RotationMultiples = src.RotationMultiples, RotationDirection = src.RotationDirection,
            RotationSyncToBpm = src.RotationSyncToBpm,
            FlashinessDelta = src.FlashinessDelta, FlashinessStart = src.FlashinessStart,
            FlashinessSpeed = src.FlashinessSpeed, FlashinessSequencer = src.FlashinessSequencer,
            FlashinessMultiples = src.FlashinessMultiples, FlashinessDirection = src.FlashinessDirection,
            FlashinessSyncToBpm = src.FlashinessSyncToBpm,
            CoronaIntensity = src.CoronaIntensity, CoronaSize = src.CoronaSize,
            CoronaPull = src.CoronaPull, CoronaFaceCamera = src.CoronaFaceCamera,
        };
        SelectedSiren.Sirens.Add(clone);
        SelectedLight = clone;
    }

    [RelayCommand]
    private void FillAllBits()
    {
        if (SelectedLight == null) return;
        SelectedLight.FlashinessSequencer = uint.MaxValue;
        SelectedLight.RotationSequencer = uint.MaxValue;
    }

    [RelayCommand]
    private void ClearAllBits()
    {
        if (SelectedLight == null) return;
        SelectedLight.FlashinessSequencer = 0;
        SelectedLight.RotationSequencer = 0;
    }
}
