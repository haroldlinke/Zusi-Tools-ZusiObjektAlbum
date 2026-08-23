using System;
using System.Collections.Generic;
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

namespace ZusiObjektAlbum.Controls
{
    public class DisplayControl : ContentControl
    {
        //---------------------------------------------------------------------
        public static readonly DependencyProperty CanExpandProperty = DependencyProperty.Register(
            "CanExpand",
            typeof(bool),
            typeof(DisplayControl),
            new PropertyMetadata(false));
        public bool CanExpand
        {
            get => (bool)GetValue(CanExpandProperty);
            set => SetValue(CanExpandProperty, value);
        }

        //---------------------------------------------------------------------
        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
            "IsExpanded",
            typeof(bool),
            typeof(DisplayControl),
            new PropertyMetadata(true));
        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        //---------------------------------------------------------------------
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            "Title",
            typeof(string),
            typeof(DisplayControl),
            new PropertyMetadata("Title"));
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        //---------------------------------------------------------------------
        public static readonly DependencyProperty TitleBackgroundProperty = DependencyProperty.Register(
            "TitleBackground",
            typeof(Brush),
            typeof(DisplayControl),
            new PropertyMetadata(Brushes.Brown));
        public Brush TitleBackground
        {
            get => (Brush)GetValue(TitleBackgroundProperty);
            set => SetValue(TitleBackgroundProperty, value);
        }

        //---------------------------------------------------------------------
        public static readonly DependencyProperty TitleForegroundProperty = DependencyProperty.Register(
            "TitleForeground",
            typeof(Brush),
            typeof(DisplayControl),
            new PropertyMetadata(Brushes.WhiteSmoke));
        public Brush TitleForeground
        {
            get => (Brush)GetValue(TitleForegroundProperty);
            set => SetValue(TitleForegroundProperty, value);
        }

        //---------------------------------------------------------------------
        static DisplayControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DisplayControl), new FrameworkPropertyMetadata(typeof(DisplayControl)));
        }
    }
}
