using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace JulschaVehicleTool.App.Controls;

public partial class SirenColorPickerControl : UserControl
{
    // GTA V siren color format: "0xAARRGGBB"
    public static readonly DependencyProperty ColorHexProperty =
        DependencyProperty.Register(nameof(ColorHex), typeof(string), typeof(SirenColorPickerControl),
            new FrameworkPropertyMetadata("0xFFFF0A0A", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColorHexChanged));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(SirenColorPickerControl),
            new PropertyMetadata("Color"));

    public string ColorHex { get => (string)GetValue(ColorHexProperty); set => SetValue(ColorHexProperty, value); }
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    // Common GTA V siren colors
    private static readonly (string Name, string Hex)[] Presets =
    {
        ("Red", "0xFFFF0A0A"),
        ("Blue", "0xFF0A0AFF"),
        ("White", "0xFFFFFFFF"),
        ("Amber", "0xFFFF8C00"),
        ("Green", "0xFF00FF00"),
        ("Purple", "0xFFBB00FF"),
    };

    public SirenColorPickerControl()
    {
        InitializeComponent();
        BuildPresets();
        UpdatePreview();
    }

    private void BuildPresets()
    {
        foreach (var (name, hex) in Presets)
        {
            var color = ParseGtaColor(hex);
            var btn = new Border
            {
                Width = 18, Height = 18,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(color),
                BorderBrush = Application.Current.TryFindResource("BorderBrush") as Brush ?? Brushes.Gray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(1, 0, 1, 0),
                Cursor = Cursors.Hand,
                ToolTip = name,
                Tag = hex,
            };
            btn.MouseLeftButtonDown += (_, _) =>
            {
                ColorHex = (string)btn.Tag;
            };
            PresetList.Items.Add(btn);
        }
    }

    private void UpdatePreview()
    {
        var color = ParseGtaColor(ColorHex);
        ColorPreview.Background = new SolidColorBrush(color);
    }

    private static void OnColorHexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SirenColorPickerControl ctrl)
            ctrl.UpdatePreview();
    }

    /// <summary>
    /// Parse GTA V color format "0xAARRGGBB" to WPF Color.
    /// </summary>
    public static Color ParseGtaColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return Colors.Red;

        var clean = hex.Replace("0x", "").Replace("#", "");
        if (uint.TryParse(clean, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var val))
        {
            byte a = (byte)((val >> 24) & 0xFF);
            byte r = (byte)((val >> 16) & 0xFF);
            byte g = (byte)((val >> 8) & 0xFF);
            byte b = (byte)(val & 0xFF);
            return Color.FromArgb(a, r, g, b);
        }
        return Colors.Red;
    }

    /// <summary>
    /// Convert WPF Color to GTA V hex string.
    /// </summary>
    public static string ToGtaHex(Color c)
    {
        return $"0x{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
