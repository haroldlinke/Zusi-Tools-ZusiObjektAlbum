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
using ZusiKlassenLib.Landscape;

namespace ZusiObjektAlbum.Controls
{
    /// <summary>
    /// Interaktionslogik für LoDViewerControl.xaml
    /// </summary>
    public partial class LoDViewerControl : UserControl
    {
        private Style _captionFieldStyle;
        private Style _nameFieldStyle;
        private Style _rightTextFieldStyle;
        private Style _textFieldStyle;

        //---------------------------------------------------------------------
        public static readonly DependencyProperty HasCaptionProperty = DependencyProperty.Register(
            "HasCaption",
            typeof(bool),
            typeof(LoDViewerControl),
            new PropertyMetadata(false, OnHasCaptionChanged));
        public bool HasCaption
        {
            get => (bool)GetValue(HasCaptionProperty);
            set => SetValue(HasCaptionProperty, value);
        }

        public LoDViewerControl()
        {
            InitializeComponent();

            _captionFieldStyle = Application.Current.TryFindResource("PropertyCaptionStyle") as Style;
            _nameFieldStyle = Application.Current.TryFindResource("PropertyNameStyle") as Style;
            _rightTextFieldStyle = Application.Current.TryFindResource("PropertyRightTextStyle") as Style;
            _textFieldStyle = Application.Current.TryFindResource("PropertyTextStyle") as Style;

            DataContextChanged += LoDViewerControl_DataContextChanged;
        }

        private void LoDViewerControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DisplayInfo(e.NewValue as LoDInfo, HasCaption);
        }

        private static void OnHasCaptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as LoDViewerControl)?.OnHasCaptionChanged((bool)e.NewValue);
        }

        private void OnHasCaptionChanged(bool value)
        {
            DisplayInfo(DataContext as LoDInfo, value);
        }

        private void DisplayInfo(LoDInfo info, bool hasCaption)
        {
            grid.Children.Clear();

            int n = hasCaption ? 2 : 1;
            while (grid.RowDefinitions.Count > n)
            {
                grid.RowDefinitions.RemoveAt(grid.RowDefinitions.Count - 1);
            }

            int row = 0;
            if (hasCaption)
            {
                AddCaptionRow();
                row++;
            }

            if (info != null)
            {
                AddInfoRow(info, row++);
                foreach (MeshInfo mi in info.MeshInfos)
                {
                    AddMeshInfoRow(mi, row++);
                }
            }
        }

        private void AddCaptionRow()
        {
            TextBlock tb = new TextBlock
            {
                Text = " ",
                Style = _nameFieldStyle
            };
            Grid.SetColumn(tb, 0);
            grid.Children.Add(tb);

            tb = new TextBlock
            {
                Text = "Dreiecke",
                Style = _captionFieldStyle,
                Margin = new Thickness(0, 0, 1, 1)
            };
            Grid.SetColumn(tb, 1);
            grid.Children.Add(tb);

            tb = new TextBlock
            {
                Text = "Sichtbarkeit/Texturgröße",
                Style = _captionFieldStyle
            };
            Grid.SetColumn(tb, 2);
            grid.Children.Add(tb);
        }

        private void AddInfoRow(LoDInfo info, int row)
        {
            TextBlock tb = new TextBlock
            {
                Text = string.Format("LoD {0}", info.LoD),
                Style = _nameFieldStyle
            };
            Grid.SetColumn(tb, 0);
            Grid.SetRow(tb, row);
            grid.Children.Add(tb);

            tb = new TextBlock
            {
                Text = info.CountTriangles.ToString(),
                Style = _captionFieldStyle,
                TextAlignment= TextAlignment.Right,
                Margin = new Thickness(0, 0, 1, 1)
            };
            Grid.SetColumn(tb, 1);
            Grid.SetRow(tb, row);
            grid.Children.Add(tb);

            tb = new TextBlock
            {
                Text = string.Format("{0} -> {1}", info.Range.From, info.Range.To),
                Style = _captionFieldStyle
            };
            Grid.SetColumn(tb, 2);
            Grid.SetRow(tb, row);
            grid.Children.Add(tb);
        }

        private void AddMeshInfoRow(MeshInfo info, int row)
        {
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });

            TextBlock tb = new TextBlock
            {
                Text = " ",
                Style = _nameFieldStyle
            };
            Grid.SetColumn(tb, 0);
            Grid.SetRow(tb, row);
            grid.Children.Add(tb);

            tb = new TextBlock
            {
                Text = info.CountTriangles.ToString(),
                Style = _rightTextFieldStyle,
                Margin = new Thickness(0, 0, 1, 1)
            };
            Grid.SetColumn(tb, 1);
            Grid.SetRow(tb, row);
            grid.Children.Add(tb);

            tb = new TextBlock
            {
                Text = info.TextureSize.Width > 0 && info.TextureSize.Height > 0 ? string.Format("{0} x {1}", info.TextureSize.Width, info.TextureSize.Height) : " ",
                Style = _textFieldStyle
            };
            Grid.SetColumn(tb, 2);
            Grid.SetRow(tb, row);
            grid.Children.Add(tb);
        }
    }
}
