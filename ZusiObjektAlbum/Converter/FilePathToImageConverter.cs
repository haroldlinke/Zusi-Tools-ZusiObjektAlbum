using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System;

namespace ZusiObjektAlbum.Converter;

/// <summary>
/// Wandelt einen Dateipfad (string) in eine BitmapImage um, damit Image.Source
/// direkt an ein string-Property (z. B. ResultItem.ThumbnailPath) gebunden
/// werden kann. Gibt bei fehlendem/ungültigem Pfad null zurück - das Image
/// bleibt dann einfach leer, statt die App abstürzen zu lassen.
/// </summary>
public sealed class FilePathToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.DecodePixelWidth = 160; // kleines Thumbnail, spart Speicher
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
