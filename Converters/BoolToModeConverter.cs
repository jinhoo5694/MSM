using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MSM
{
    public class BoolToModeConverter : IValueConverter
    {
        public static readonly BoolToModeConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isBarcodeScanningEnabled)
            {
                return isBarcodeScanningEnabled ? "스캔" : "검색";
            }
            return "검색";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
