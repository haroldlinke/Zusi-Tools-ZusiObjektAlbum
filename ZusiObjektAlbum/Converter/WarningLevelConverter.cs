using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ZusiObjektAlbum.Converter
{
    [ValueConversion(typeof(int), typeof(string))]
    public class WarningLevelConverter : IValueConverter
    {
        private static readonly string[] values = new string[]
        {
            "",
            "LOD-3 ist für dieses Objekt nicht festgelegt",
            "LOD-2 und LOD-3 sind für dieses Objekt nicht festgelegt"
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int i && i >= 0 && i < values.Length ? values[i] : values[0];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
