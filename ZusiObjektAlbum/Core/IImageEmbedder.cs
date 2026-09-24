using System;
using System.Windows.Media.Imaging;

namespace ZusiObjektAlbum.Core
{ 

/// <summary>
/// Gemeinsame Schnittstelle für Bild-Encoder (CLIP, DINOv2, ...) - erlaubt,
/// das für die Ähnlichkeitssuche verwendete Modell auszutauschen, ohne
/// BatchImageExporter oder die Suche selbst anfassen zu müssen.
/// </summary>
public interface IImageEmbedder : IDisposable
{
  float[] ComputeEmbedding(string imagePath);
  float[] ComputeEmbedding(BitmapSource source);
}
}
