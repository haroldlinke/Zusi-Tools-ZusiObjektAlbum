using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ZusiKlassenLib.Landscape;
using ZusiKlassenLib.Texture;

namespace ZusiObjektAlbum.Controls
{
    public class GeometryViewer : Control
    {
        //---------------------------------------------------------------------
        static GeometryViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GeometryViewer), new FrameworkPropertyMetadata(typeof(GeometryViewer)));
        }
    }

    [ValueConversion(typeof(TextureSize), typeof(string))]
    public class TextureSizeConverter : IValueConverter
    {
        //---------------------------------------------------------------------
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is TextureSize ts && ts.Width > 0 && ts.Height > 0 ? string.Format("{0} x {1}", ts.Width, ts.Height) : null;
        }

        //---------------------------------------------------------------------
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
