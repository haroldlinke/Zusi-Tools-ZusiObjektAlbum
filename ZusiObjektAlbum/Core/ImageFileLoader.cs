using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ZusiObjektAlbum.Core;

/// <summary>
/// Lädt ein Bild von der Platte als eingefrorenes (thread-sicheres)
/// BitmapSource - reiner WPF-Code, keine externe Bildbibliothek nötig.
///
/// Wertet außerdem die EXIF-Orientierung aus (wichtig bei Handy-/
/// Kamerafotos, die "liegend" gespeichert sind und nur ein Metadaten-Flag
/// für die richtige Anzeige tragen) und brennt sie direkt in die
/// Pixeldaten ein, damit PixelWidth/PixelHeight/CopyPixels überall im
/// Code (Vorschau, BackgroundRemover, PerspectiveCorrector, Embedding)
/// danach konsistent das tatsächlich korrekt orientierte Bild sehen.
/// </summary>
public static class ImageFileLoader
{
    public static BitmapSource Load(string path)
    {
        BitmapDecoder decoder = BitmapDecoder.Create(
            new Uri(path, UriKind.Absolute), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        BitmapFrame frame = decoder.Frames[0];

        BitmapSource oriented = ApplyExifOrientation(frame);
        if (oriented.CanFreeze && !oriented.IsFrozen)
        {
            oriented.Freeze();
        }

        return oriented;
    }

    private static BitmapSource ApplyExifOrientation(BitmapFrame frame)
    {
        if (frame.Metadata is not BitmapMetadata metadata)
        {
            return frame;
        }

        int orientation;
        try
        {
            // Standard-EXIF-Orientation-Tag (0x0112 = 274).
            object? value = metadata.GetQuery("/app1/ifd/{ushort=274}");
            if (value is null)
            {
                return frame; // kein EXIF vorhanden - unverändert
            }
            orientation = Convert.ToInt32(value);
        }
        catch
        {
            return frame; // kein lesbares EXIF - unverändert
        }

        if (orientation == 1)
        {
            return frame; // bereits korrekt orientiert
        }

        // Die 8 EXIF-Orientierungswerte, siehe exif.org / CIPA DC-008.
        Transform transform = orientation switch
        {
            2 => new ScaleTransform(-1, 1),
            3 => new RotateTransform(180),
            4 => new ScaleTransform(1, -1),
            5 => new TransformGroup { Children = { new RotateTransform(90), new ScaleTransform(-1, 1) } },
            6 => new RotateTransform(90),
            7 => new TransformGroup { Children = { new RotateTransform(270), new ScaleTransform(-1, 1) } },
            8 => new RotateTransform(270),
            _ => Transform.Identity
        };

        return new TransformedBitmap(frame, transform);
    }
}
