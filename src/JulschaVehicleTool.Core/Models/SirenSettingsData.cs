using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace JulschaVehicleTool.Core.Models;

public partial class CarColsData : ObservableObject
{
    public ObservableCollection<SirenSetting> SirenSettings { get; } = new();
}

public partial class SirenSetting : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private float _timeMultiplier = 1f;
    [ObservableProperty] private float _lightFalloffMax = 10f;
    [ObservableProperty] private float _lightFalloffExponent = 10f;
    [ObservableProperty] private float _lightInnerConeAngle = 2.29f;
    [ObservableProperty] private float _lightOuterConeAngle = 70f;
    [ObservableProperty] private float _lightOffset;
    [ObservableProperty] private string _textureName = "VehicleLight_sirenlight";
    [ObservableProperty] private uint _sequencerBpm = 220;
    [ObservableProperty] private bool _useRealLights = true;

    // Headlights / Taillights sequencers
    [ObservableProperty] private uint _leftHeadLightSequencer;
    [ObservableProperty] private uint _rightHeadLightSequencer;
    [ObservableProperty] private uint _leftTailLightSequencer;
    [ObservableProperty] private uint _rightTailLightSequencer;
    [ObservableProperty] private int _leftHeadLightMultiples = 1;
    [ObservableProperty] private int _rightHeadLightMultiples = 1;
    [ObservableProperty] private int _leftTailLightMultiples = 1;
    [ObservableProperty] private int _rightTailLightMultiples = 1;

    // Siren lights (max 20)
    public ObservableCollection<SirenLight> Sirens { get; } = new();
}

public partial class SirenLight : ObservableObject
{
    // Rotation
    [ObservableProperty] private float _rotationDelta;
    [ObservableProperty] private float _rotationStart;
    [ObservableProperty] private float _rotationSpeed = 3f;
    [ObservableProperty] private uint _rotationSequencer = uint.MaxValue;
    [ObservableProperty] private int _rotationMultiples = 1;
    [ObservableProperty] private bool _rotationDirection;
    [ObservableProperty] private bool _rotationSyncToBpm = true;

    // Flashiness
    [ObservableProperty] private float _flashinessDelta;
    [ObservableProperty] private float _flashinessStart;
    [ObservableProperty] private float _flashinessSpeed = 1f;
    [ObservableProperty] private uint _flashinessSequencer;
    [ObservableProperty] private int _flashinessMultiples = 1;
    [ObservableProperty] private bool _flashinessDirection;
    [ObservableProperty] private bool _flashinessSyncToBpm = true;

    // Corona
    [ObservableProperty] private float _coronaIntensity = 50f;
    [ObservableProperty] private float _coronaSize = 1.1f;
    [ObservableProperty] private float _coronaPull = 0.2f;
    [ObservableProperty] private bool _coronaFaceCamera;

    // Light properties
    [ObservableProperty] private string _color = "0xFFFF0A0A";
    [ObservableProperty] private float _intensity = 1f;
    [ObservableProperty] private int _lightGroup;
    [ObservableProperty] private bool _rotate;
    [ObservableProperty] private bool _scale = true;
    [ObservableProperty] private float _scaleFactor = 2f;
    [ObservableProperty] private bool _flash = true;
    [ObservableProperty] private bool _light = true;
    [ObservableProperty] private bool _spotLight = true;
    [ObservableProperty] private bool _castShadows;
}
