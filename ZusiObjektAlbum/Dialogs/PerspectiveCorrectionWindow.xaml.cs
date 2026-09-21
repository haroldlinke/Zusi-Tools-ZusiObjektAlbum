using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ZusiSimilaritySearch;

namespace ZusiObjektAlbum.Dialogs
{
  public partial class PerspectiveCorrectionWindow : Window
  {
    private BitmapSource _sourceImage;
    private readonly List<Point> _imagePoints = new(); // direkt in Bildpixel-Koordinaten

    public BitmapSource? Result { get; private set; }

    public PerspectiveCorrectionWindow(BitmapSource sourceImage)
    {
      InitializeComponent();
      _sourceImage = sourceImage;

      // ImageCanvas exakt auf Bildpixelgröße setzen - der Viewbox
      // skaliert alles (Bild UND später hinzugefügte Marker)
      // gleichmäßig auf die verfügbare Fenstergröße. Dadurch liefert
      // e.GetPosition(ImageCanvas) im Klick-Handler direkt echte
      // Bildpixel-Koordinaten - keine eigene Skalierungsrechnung
      // mehr nötig, WPF übernimmt die Rücktransformation korrekt.
      ImageCanvas.Width = sourceImage.PixelWidth;
      ImageCanvas.Height = sourceImage.PixelHeight;
      PhotoImage.Width = sourceImage.PixelWidth;
      PhotoImage.Height = sourceImage.PixelHeight;
      PhotoImage.Source = sourceImage;
    }

    private void ImageCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (_imagePoints.Count >= 4)
      {
        return;
      }

      Point imagePoint = e.GetPosition(ImageCanvas); // bereits Bildpixel-Koordinaten
      _imagePoints.Add(imagePoint);
      DrawMarker(imagePoint, _imagePoints.Count);

      if (_imagePoints.Count == 4)
      {
        DrawQuadOutline();
        ApplyButton.IsEnabled = true;
      }
    }

    private void DrawMarker(Point imagePoint, int number)
    {
      // Größe relativ zur Bildbreite, damit die Marker beim
      // Herunterskalieren durch den Viewbox nicht winzig werden.
      double size = _sourceImage.PixelWidth * 0.012;

      var ellipse = new Ellipse
      {
        Width = size,
        Height = size,
        Fill = Brushes.Red,
        Stroke = Brushes.White,
        StrokeThickness = size * 0.15
      };
      Canvas.SetLeft(ellipse, imagePoint.X - size / 2);
      Canvas.SetTop(ellipse, imagePoint.Y - size / 2);
      ImageCanvas.Children.Add(ellipse);

      var label = new TextBlock
      {
        Text = number.ToString(),
        Foreground = Brushes.Yellow,
        FontWeight = FontWeights.Bold,
        FontSize = size * 1.2
      };
      Canvas.SetLeft(label, imagePoint.X + size * 0.7);
      Canvas.SetTop(label, imagePoint.Y - size * 0.8);
      ImageCanvas.Children.Add(label);
    }

    private void DrawQuadOutline()
    {
      var polygon = new Polygon
      {
        Stroke = Brushes.Lime,
        StrokeThickness = _sourceImage.PixelWidth * 0.003,
        Fill = Brushes.Transparent
      };

      foreach (Point p in _imagePoints)
      {
        polygon.Points.Add(p);
      }

      ImageCanvas.Children.Add(polygon);
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
      _imagePoints.Clear();

      // Nur Marker/Polygon entfernen, das Foto selbst (erstes Kind) behalten.
      for (int i = ImageCanvas.Children.Count - 1; i >= 1; i--)
      {
        ImageCanvas.Children.RemoveAt(i);
      }

      ApplyButton.IsEnabled = false;
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
      Result = PerspectiveCorrector.Correct(_sourceImage, _imagePoints.ToArray());
      _sourceImage = Result;
      //ImageCanvas.Width = Result.PixelWidth;
      //ImageCanvas.Height = Result.PixelHeight;
      //PhotoImage.Width = Result.PixelWidth;
      //PhotoImage.Height = Result.PixelHeight;
      //PhotoImage.Source = Result;


      Transform transform = new TransformGroup
      {
        Children = { new RotateTransform(90), new ScaleTransform(-1, 1) }

      };

      Result = new TransformedBitmap(Result, transform);

      Result.Freeze();

      PhotoImage.Source = Result;

      _imagePoints.Clear();

      // Nur Marker/Polygon entfernen, das Foto selbst (erstes Kind) behalten.
      for (int i = ImageCanvas.Children.Count - 1; i >= 1; i--)
      {
        ImageCanvas.Children.RemoveAt(i);
      }
      ApplyButton.IsEnabled = false;
      DialogResult = true;
    }
  }
}