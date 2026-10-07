using System;
using System.Globalization;
using System.Windows.Data;

namespace VisualCommander.Converters
{
    /// <summary>Convierte true ↔ false. Útil para bindings tipo IsEnabled = !EstaBloqueado.</summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : true;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : false;
    }
}