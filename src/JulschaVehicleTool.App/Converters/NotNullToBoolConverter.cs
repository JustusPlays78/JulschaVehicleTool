using System.Globalization;
using System.Windows.Data;

namespace JulschaVehicleTool.App.Converters;

/// <summary>true if the value is not null.</summary>
public class NotNullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value != null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
