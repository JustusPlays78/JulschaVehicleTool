using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace JulschaVehicleTool.App.Converters;

/// <summary>
/// Converts a hex color string (e.g. "FF0000" or "#FF0000") to a SolidColorBrush.
/// Returns a transparent brush on invalid input.
/// </summary>
public class ColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string hex)
            return new SolidColorBrush(Colors.Transparent);

        try
        {
            // Normalize: ensure leading #
            var normalized = hex.TrimStart('#');
            if (normalized.Length == 6)
                normalized = "FF" + normalized; // Add alpha

            if (normalized.Length == 8)
            {
                var a = System.Convert.ToByte(normalized[0..2], 16);
                var r = System.Convert.ToByte(normalized[2..4], 16);
                var g = System.Convert.ToByte(normalized[4..6], 16);
                var b = System.Convert.ToByte(normalized[6..8], 16);
                return new SolidColorBrush(Color.FromArgb(a, r, g, b));
            }
        }
        catch { }

        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
