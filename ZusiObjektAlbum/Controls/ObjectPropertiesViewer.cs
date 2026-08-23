using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ZusiKlassenLib;
using ZusiObjektAlbum.MVVM;
using ZusiKlassenLib.Common;

namespace ZusiObjektAlbum.Controls
{
    public class ObjectPropertiesViewer : Control
    {
#if false
        public static readonly DependencyProperty ObjectSourceProperty = DependencyProperty.Register(
            "ObjectSource",
            typeof(Zusi3DModel),
            typeof(ObjectPropertiesViewer),
            new PropertyMetadata(null/*, OnObjectSourceChanged*/));
        public Zusi3DModel ObjectSource
        {
            get => (Zusi3DModel)GetValue(ObjectSourceProperty);
            set => SetValue(ObjectSourceProperty, value);
        }
#endif

        static ObjectPropertiesViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ObjectPropertiesViewer), new FrameworkPropertyMetadata(typeof(ObjectPropertiesViewer)));
        }

#if false
        public ObjectPropertiesViewer()
        {
            Binding b = new Binding("ObjectSource");
            b.Mode = BindingMode.OneWay;
            b.Source = this;
            BindingOperations.SetBinding(this, DataContextProperty, b);
        }
#endif
    }

    public class AuthorsHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<AutorEintrag> collection)
            {
                int n = collection.Count();
                return n == 1 ? "Autor" : "Autoren";
            }

            return "Kein Autor";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
