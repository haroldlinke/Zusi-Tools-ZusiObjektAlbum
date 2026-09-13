using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using ZusiObjektAlbum.Similaritysearch;

namespace ZusiObjektAlbum.Converter
{
    /// <summary>
    /// Rendert das Vorschaubild für ein Suchergebnis live aus dem echten
    /// .ls3-Quellpfad (statt eine vorab gespeicherte PNG-Datei zu laden - die
    /// gibt es seit der Umstellung auf reine In-Memory-Embeddings nicht mehr).
    ///
    /// Erwartet eine MultiBinding mit zwei Werten: [0] SourcePath, [1] BestView.
    ///
    /// Cached Ergebnisse (auch fehlgeschlagene, als null) in einem statischen
    /// Dictionary für die Lebensdauer der Anwendung, damit ein einmal
    /// angezeigtes Ergebnis nicht bei jedem Scrollen/erneuten Öffnen der
    /// Ergebnisliste neu gerendert wird. Bei sehr vielen unterschiedlichen
    /// Suchen über eine lange Session hinweg wächst dieser Cache unbegrenzt -
    /// für den normalen Gebrauch (einzelne Suchsitzungen mit überschaubaren
    /// Trefferzahlen) ist das unkritisch.
    ///
    /// WICHTIG: Das Rendern läuft synchron auf dem UI-Thread (wie beim Batch-
    /// Export auch), da WPFs Viewport3D/RenderTargetBitmap Thread-Affinität
    /// zum UI-Thread haben. Bei sehr vielen gleichzeitig neu anzuzeigenden
    /// Treffern (z.B. großes topN) kann das erstmalige Füllen der Ergebnisliste
    /// dadurch spürbar dauern - einmal gecachte Objekte sind danach sofort da.
    /// </summary>
    public sealed class ObjectPreviewImageConverter : IMultiValueConverter
    {
        private static readonly Dictionary<string, BitmapSource?> Cache = new();

        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not string sourcePath || values[1] is not string bestView)
            {
                return null;
            }

            if (string.IsNullOrEmpty(sourcePath))
            {
                return null;
            }

            string cacheKey = sourcePath + "|" + bestView;
            if (Cache.TryGetValue(cacheKey, out BitmapSource? cached))
            {
                return cached;
            }

            BitmapSource? bitmap = BatchImageExporter.RenderPreview(sourcePath, bestView);
            Cache[cacheKey] = bitmap;
            return bitmap;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
