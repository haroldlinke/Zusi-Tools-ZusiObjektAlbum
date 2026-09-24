using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ZusiObjektAlbum.Core;

/// 
/// <summary>
/// Lädt ein CLIP-Vision-ONNX-Modell und berechnet daraus L2-normierte Embeddings für Bilder.
///
/// Zwei Eingabewege:
///  - ComputeEmbedding(string imagePath): für Bilder von der Platte (z.B. das
///    vom Nutzer hochgeladene Suchfoto). Nutzt ImageSharp.
///  - ComputeEmbedding(BitmapSource source): für bereits im Speicher
///    gerenderte WPF-Bilder (z.B. direkt aus RenderTargetBitmap beim
///    Indizieren) - spart den Umweg über PNG-Encode/Decode und das
///    Speichern auf der Platte komplett. Nutzt reines WPF, kein ImageSharp.
///
/// Beide Wege liefern identisch vorverarbeitete (Stretch-Resize auf 224x224,
/// CLIP-Normalisierung) und L2-normierte Vektoren, sind also untereinander
/// direkt vergleichbar.
/// </summary>
public sealed class ClipEmbedder : IImageEmbedder
{
    private const string InputName = "pixel_values";
    private const int ImageSize = 224;

    private static readonly float[] Mean = { 0.48145466f, 0.4578275f, 0.40821073f };
    private static readonly float[] Std = { 0.26862954f, 0.26130258f, 0.27577711f };

    private readonly InferenceSession _session;

    public ClipEmbedder(string onnxModelPath)
    {
        var options = new SessionOptions
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
        };

        _session = new InferenceSession(onnxModelPath, options);
    }

    /// <summary>
    /// Für Bilder von der Platte (z.B. das vom Nutzer hochgeladene Suchfoto).
    /// </summary>
    public float[] ComputeEmbedding(string imagePath)
    {
    BitmapSource bitmap = ImageFileLoader.Load(imagePath);
    return ComputeEmbedding(bitmap);

  }

  /// <summary>
  /// Für bereits im Speicher vorliegende WPF-Bilder (z.B. direkt aus
  /// RenderTargetBitmap) - ohne Umweg über Datei/PNG-Codec.
  /// </summary>
  public float[] ComputeEmbedding(BitmapSource source)
    {
        var tensor = PreprocessBitmapSource(source);
        return RunAndNormalize(tensor);
    }

    //---------------------------------------------------------------------
    private float[] RunAndNormalize(DenseTensor<float> tensor)
    {
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(InputName, tensor)
        };

        using var results = _session.Run(inputs);
        float[] output = results.First().AsEnumerable<float>().ToArray();

        return Normalize(output);
    }

    //---------------------------------------------------------------------
    /// <summary>
    /// Reines WPF-Äquivalent zu PreprocessImageSharp: staucht (nicht
    /// beschneidet) das Bild auf exakt ImageSize x ImageSize - analog zum
    /// bisherigen ResizeMode.Stretch - und wendet dieselbe CLIP-Normalisierung an.
    /// </summary>
    private static DenseTensor<float> PreprocessBitmapSource(BitmapSource source)
    {
        double scaleX = (double)ImageSize / source.PixelWidth;
        double scaleY = (double)ImageSize / source.PixelHeight;

        var resized = new TransformedBitmap(source, new ScaleTransform(scaleX, scaleY));
        var converted = new FormatConvertedBitmap(resized, PixelFormats.Rgb24, null, 0);

        int stride = ImageSize * 3;
        byte[] pixels = new byte[stride * ImageSize];
        converted.CopyPixels(pixels, stride, 0);

        var tensor = new DenseTensor<float>(new[] { 1, 3, ImageSize, ImageSize });

        for (int y = 0; y < ImageSize; y++)
        {
            int rowOffset = y * stride;
            for (int x = 0; x < ImageSize; x++)
            {
                int i = rowOffset + x * 3;
                byte r = pixels[i];
                byte g = pixels[i + 1];
                byte b = pixels[i + 2];

                tensor[0, 0, y, x] = (r / 255f - Mean[0]) / Std[0];
                tensor[0, 1, y, x] = (g / 255f - Mean[1]) / Std[1];
                tensor[0, 2, y, x] = (b / 255f - Mean[2]) / Std[2];
            }
        }

        return tensor;
    }

    //---------------------------------------------------------------------
    private static float[] Normalize(float[] vector)
    {
        double sumSquares = 0.0;
        foreach (float v in vector)
        {
            sumSquares += v * v;
        }

        double norm = Math.Sqrt(sumSquares);
        if (norm < 1e-12) return vector;

        var result = new float[vector.Length];
        for (int i = 0; i < vector.Length; i++)
        {
            result[i] = (float)(vector[i] / norm);
        }

        return result;
    }

    public void Dispose() => _session.Dispose();
}
