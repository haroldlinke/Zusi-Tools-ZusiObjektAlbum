using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ZusiSimilaritySearch;

/// <summary>
/// Entzerrt ein Foto per perspektivischer Transformation (Homographie):
/// bildet 4 vom Nutzer markierte Eckpunkte eines eigentlich rechteckigen
/// Merkmals (z.B. Fensterrahmen, Gebäudekante) exakt auf ein Rechteck ab -
/// und dehnt dieselbe Korrektur auf das GESAMTE Foto aus (nicht nur auf
/// den markierten Ausschnitt), analog zu "Perspektive korrigieren" in
/// Lightroom/Photoshop. Am Rand können dadurch transparente Bereiche
/// entstehen, wo nach der Entzerrung "nichts mehr da ist" - das ist bei
/// dieser Art Korrektur normal.
///
/// Nutzt den klassischen "Square-to-Quad"-Algorithmus (Paul Heckbert,
/// "Fundamentals of Texture Mapping and Image Warping", 1989) für die
/// Homographie-Berechnung, plus eine einfache 3x3-Matrixinversion für die
/// Rückrichtung - beides ohne externe Abhängigkeiten.
/// </summary>
public static class PerspectiveCorrector
{
  // Bei sehr extremen Blickwinkeln kann die extrapolierte Fläche
  // theoretisch riesig werden - Sicherheitsbegrenzung dagegen.
  private const int MaxCanvasSize = 4000;

  /// <summary>
  /// corners: die 4 vom Nutzer markierten Punkte in Bildpixel-Koordinaten
  /// des Quellbilds, in der Reihenfolge oben-links, oben-rechts,
  /// unten-rechts, unten-links.
  ///
  /// targetRectWidth/Height: Größe, auf die die 4 Punkte selbst abgebildet
  /// werden (bestimmt den "Maßstab" der Entzerrung). Wenn nicht angegeben,
  /// aus den Kantenlängen der 4 Punkte geschätzt.
  /// </summary>
  public static BitmapSource Correct(BitmapSource source, Point[] corners, int? targetRectWidth = null, int? targetRectHeight = null)
  {
    if (corners.Length != 4)
    {
      throw new ArgumentException("Es werden genau 4 Eckpunkte erwartet (oben-links, oben-rechts, unten-rechts, unten-links).");
    }

    int rectWidth = targetRectWidth ?? (int)Math.Max(50,
        ((corners[0] - corners[1]).Length + (corners[3] - corners[2]).Length) / 2);
    int rectHeight = targetRectHeight ?? (int)Math.Max(50,
        ((corners[0] - corners[3]).Length + (corners[1] - corners[2]).Length) / 2);

    // rectToQuad: bildet ein normiertes Rechteck (u,v in [0,1]) auf die
    // 4 markierten Punkte im Quellbild ab - für das Backward-Mapping
    // (Ziel-Pixel -> Quell-Pixel) beim eigentlichen Rendern.
    double[,] rectToQuad = ComputeSquareToQuadMatrix(corners);

    // quadToRect: die Umkehrung - wird nur gebraucht, um herauszufinden,
    // wohin die 4 Ecken des GESAMTEN Quellbilds nach der Entzerrung
    // wandern würden (um die nötige Leinwandgröße zu bestimmen).
    double[,] quadToRect = Invert3x3(rectToQuad);

    var srcCorners = new[]
    {
            new Point(0, 0),
            new Point(source.PixelWidth, 0),
            new Point(source.PixelWidth, source.PixelHeight),
            new Point(0, source.PixelHeight)
        };

    double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
    foreach (Point corner in srcCorners)
    {
      (double u, double v) = ApplyMatrix(quadToRect, corner.X, corner.Y);
      double dx = u * rectWidth;
      double dy = v * rectHeight;
      minX = Math.Min(minX, dx); maxX = Math.Max(maxX, dx);
      minY = Math.Min(minY, dy); maxY = Math.Max(maxY, dy);
    }

    int canvasWidth = (int)Math.Clamp(maxX - minX, 50, MaxCanvasSize);
    int canvasHeight = (int)Math.Clamp(maxY - minY, 50, MaxCanvasSize);
    double offsetX = -minX;
    double offsetY = -minY;

    var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
    int srcStride = source.PixelWidth * 4;
    var srcPixels = new byte[srcStride * source.PixelHeight];
    converted.CopyPixels(srcPixels, srcStride, 0);

    int dstStride = canvasWidth * 4;
    var dstPixels = new byte[dstStride * canvasHeight];

    for (int dy = 0; dy < canvasHeight; dy++)
    {
      double v = (dy - offsetY) / rectHeight;
      for (int dx = 0; dx < canvasWidth; dx++)
      {
        double u = (dx - offsetX) / rectWidth;
        (double sx, double sy) = ApplyMatrix(rectToQuad, u, v);

        (byte b, byte g, byte r, byte a) = SampleBilinear(
            srcPixels, srcStride, source.PixelWidth, source.PixelHeight, sx, sy);

        int di = dy * dstStride + dx * 4;
        dstPixels[di] = b;
        dstPixels[di + 1] = g;
        dstPixels[di + 2] = r;
        dstPixels[di + 3] = a;
      }
    }

    var result = BitmapSource.Create(canvasWidth, canvasHeight, 96, 96, PixelFormats.Bgra32, null, dstPixels, dstStride);
    result.Freeze();
    return result;
  }

  //---------------------------------------------------------------------
  /// <summary>
  /// Heckberts Square-to-Quad-Algorithmus: liefert die 3x3-Matrix [a b c; d e f; g h 1],
  /// die das Einheitsquadrat (0,0),(1,0),(1,1),(0,1) auf das gegebene
  /// Viereck abbildet: (x,y) = (a*u+b*v+c, d*u+e*v+f) / (g*u+h*v+1).
  /// </summary>
  private static double[,] ComputeSquareToQuadMatrix(Point[] quad)
  {
    double x0 = quad[0].X, y0 = quad[0].Y;
    double x1 = quad[1].X, y1 = quad[1].Y;
    double x2 = quad[2].X, y2 = quad[2].Y;
    double x3 = quad[3].X, y3 = quad[3].Y;

    double dx1 = x1 - x2, dx2 = x3 - x2, dx3 = x0 - x1 + x2 - x3;
    double dy1 = y1 - y2, dy2 = y3 - y2, dy3 = y0 - y1 + y2 - y3;

    double a, b, c, d, e, f, g, h;
    const double eps = 1e-10;

    if (Math.Abs(dx3) < eps && Math.Abs(dy3) < eps)
    {
      a = x1 - x0;
      b = x2 - x1;
      c = x0;
      d = y1 - y0;
      e = y2 - y1;
      f = y0;
      g = 0;
      h = 0;
    }
    else
    {
      double denom = dx1 * dy2 - dx2 * dy1;
      g = (dx3 * dy2 - dx2 * dy3) / denom;
      h = (dx1 * dy3 - dx3 * dy1) / denom;

      a = x1 - x0 + g * x1;
      b = x3 - x0 + h * x3;
      c = x0;
      d = y1 - y0 + g * y1;
      e = y3 - y0 + h * y3;
      f = y0;
    }

    return new double[,] { { a, b, c }, { d, e, f }, { g, h, 1 } };
  }

  //---------------------------------------------------------------------
  private static double[,] Invert3x3(double[,] m)
  {
    double a = m[0, 0], b = m[0, 1], c = m[0, 2];
    double d = m[1, 0], e = m[1, 1], f = m[1, 2];
    double g = m[2, 0], h = m[2, 1], i = m[2, 2];

    double coA = e * i - f * h;
    double coB = -(d * i - f * g);
    double coC = d * h - e * g;
    double coD = -(b * i - c * h);
    double coE = a * i - c * g;
    double coF = -(a * h - b * g);
    double coG = b * f - c * e;
    double coH = -(a * f - c * d);
    double coI = a * e - b * d;

    double det = a * coA + b * coB + c * coC;
    if (Math.Abs(det) < 1e-12)
    {
      throw new InvalidOperationException("Die 4 Eckpunkte bilden kein gültiges (nicht-entartetes) Viereck.");
    }

    double invDet = 1.0 / det;
    return new double[,]
    {
            { coA * invDet, coD * invDet, coG * invDet },
            { coB * invDet, coE * invDet, coH * invDet },
            { coC * invDet, coF * invDet, coI * invDet }
    };
  }

  //---------------------------------------------------------------------
  private static (double X, double Y) ApplyMatrix(double[,] m, double u, double v)
  {
    double x = m[0, 0] * u + m[0, 1] * v + m[0, 2];
    double y = m[1, 0] * u + m[1, 1] * v + m[1, 2];
    double w = m[2, 0] * u + m[2, 1] * v + m[2, 2];
    return (x / w, y / w);
  }

  //---------------------------------------------------------------------
  private static (byte B, byte G, byte R, byte A) SampleBilinear(
      byte[] pixels, int stride, int width, int height, double x, double y)
  {
    if (x < 0 || y < 0 || x >= width - 1 || y >= height - 1)
    {
      return (255, 255, 255, 0); // außerhalb des Quellbilds -> transparent
    }

    int x0 = (int)x, y0 = (int)y;
    double fx = x - x0, fy = y - y0;

    var c00 = GetPixel(pixels, stride, x0, y0);
    var c10 = GetPixel(pixels, stride, x0 + 1, y0);
    var c01 = GetPixel(pixels, stride, x0, y0 + 1);
    var c11 = GetPixel(pixels, stride, x0 + 1, y0 + 1);

    byte Lerp(byte v00, byte v10, byte v01, byte v11) =>
        (byte)((v00 * (1 - fx) + v10 * fx) * (1 - fy) + (v01 * (1 - fx) + v11 * fx) * fy);

    return (
        Lerp(c00.B, c10.B, c01.B, c11.B),
        Lerp(c00.G, c10.G, c01.G, c11.G),
        Lerp(c00.R, c10.R, c01.R, c11.R),
        Lerp(c00.A, c10.A, c01.A, c11.A));
  }

  //---------------------------------------------------------------------
  private static (byte B, byte G, byte R, byte A) GetPixel(byte[] pixels, int stride, int x, int y)
  {
    int i = y * stride + x * 4;
    return (pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]);
  }
}
