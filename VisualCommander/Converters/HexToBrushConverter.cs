using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace VisualCommander.Converters
{
    /// <summary>
    /// Convierte un string hexadecimal (ej. "F4B400" o "#F4B400") en un SolidColorBrush.
    /// Si el valor es nulo, vacío o inválido, devuelve un gris claro por defecto.
    /// </summary>
    public class HexToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Si no es un string válido, devolvemos un gris neutro.
            if (value is not string hex || string.IsNullOrWhiteSpace(hex))
                return Brushes.LightGray;

            try
            {
                // Acepta tanto "F4B400" como "#F4B400"
                var s = hex.StartsWith("#") ? hex : "#" + hex;
                var color = (Color)ColorConverter.ConvertFromString(s);
                return new SolidColorBrush(color);
            }
            catch
            {
                // Si el string no es un color válido, no rompemos la UI.
                return Brushes.LightGray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException("No se usa en una sola dirección.");
    }
}