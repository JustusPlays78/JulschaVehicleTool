using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace JulschaVehicleTool.Core.Models;

public partial class CarVariationData : ObservableObject
{
    [ObservableProperty] private string _modelName = "";

    public ObservableCollection<ColorCombination> Colors { get; } = new();
    public ObservableCollection<string> Kits { get; } = new();
    public ObservableCollection<PlateProbability> PlateProbabilities { get; } = new();

    [ObservableProperty] private int _lightSettings;
    [ObservableProperty] private int _sirenSettings;
}

public partial class ColorCombination : ObservableObject
{
    [ObservableProperty] private int _primaryColor;
    [ObservableProperty] private int _secondaryColor;
    [ObservableProperty] private int _pearlescentColor;
    [ObservableProperty] private int _rimColor;
    [ObservableProperty] private int _interiorTrimColor;
    [ObservableProperty] private int _dashboardColor;

    public ObservableCollection<bool> Liveries { get; } = new();
}

public partial class PlateProbability : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private int _value = 100;
}
