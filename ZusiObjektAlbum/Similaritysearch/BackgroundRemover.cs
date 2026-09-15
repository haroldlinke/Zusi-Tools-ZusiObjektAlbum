using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ZusiSimilaritySearch;

/// <summary>
/// Entfernt den Hintergrund von einem Foto per U2Net - demselben Modell,
/// das "rembg" standardmäßig verwendet. Nur für das vom Nutzer hochgeladene
/// Suchfoto gedacht, nicht für die gerenderten Objektbilder (die haben
/// bereits einen sauberen, einheitlichen Hintergrund).
///
/// Modell separat besorgen: u2net.onnx (~176 MB, genauer) oder u2netp.onnx
/// (~4.5 MB, schneller, etwas ungenauer) - beide z.B. über
/// https://github.com/danielgatis/rembg verlinkt. Ein-/Ausgabenamen ggf.
/// mit netron.app prüfen und InputName anpassen.
///
/// Ergebnis hat weißen (statt transparenten) Hintergrund, damit es sich
/// genauso wie die gerenderten Objektbilder für ClipEmbedder eignet.
/// </summary>
public sealed class BackgroundRemover : IDisposable
{
    private const string InputName = "input.1"; // ggf. mit netron.app prüfen
    private const int ModelSize = 320;

    // U2Net verwendet Standard-ImageNet-Normalisierung.
    private static readonly float[] Mean = { 0.485f, 0.456f, 0.406f };
    private static readonly float[] Std = { 0.229f, 0.224f, 0.225f };

    private readonly InferenceSession _session;

    public BackgroundRemover(string onnxModelPath)
    {
        var options = new SessionOptions { GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL };
        _session = new InferenceSession(onnxModelPath, options);
    }

    /// <summary>
    /// Entfernt den Hintergrund und gibt ein neues Bild mit weißem
    /// Hintergrund zurück (gleiche Auflösung wie source).
    /// </summary>
    public BitmapSource RemoveBackground(BitmapSource source)
    {
        float[,] mask = ComputeMask(source);
        return ApplyMask(source, mask);
    }

    //---------------------------------------------------------------------
    private float[,] ComputeMask(BitmapSource source)
    {
        DenseTensor<float> tensor = Preprocess(source);

        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(InputName, tensor) };
        using var results = _session.Run(inputs);
        float[] raw = results.First().AsEnumerable<float>().ToArray(); // [1,1,320,320]

        // Min-Max-Normalisierung des rohen Saliency-Outputs auf 0..1
        // (0 = Hintergrund, 1 = Objekt) - macht auch rembg intern so.
        float min = raw.Min();
        float max = raw.Max();
        float range = Math.Max(max - min, 1e-6f);

        var mask = new float[ModelSize, ModelSize];
        for (int y = 0; y < ModelSize; y++)
        {
            for (int x = 0; x < ModelSize; x++)
            {
                mask[y, x] = (raw[y * ModelSize + x] - min) / range;
            }
        }

        return mask;
    }

    //---------------------------------------------------------------------
    private static DenseTensor<float> Preprocess(BitmapSource source)
    {
        double scaleX = (double)ModelSize / source.PixelWidth;
        double scaleY = (double)ModelSize / source.PixelHeight;

        var resized = new TransformedBitmap(source, new ScaleTransform(scaleX, scaleY));
        var converted = new FormatConvertedBitmap(resized, PixelFormats.Rgb24, null, 0);

        int stride = ModelSize * 3;
        var pixels = new byte[stride * ModelSize];
        converted.CopyPixels(pixels, stride, 0);

        var tensor = new DenseTensor<float>(new[] { 1, 3, ModelSize, ModelSize });
        for (int y = 0; y < ModelSize; y++)
        {
            int rowOffset = y * stride;
            for (int x = 0; x < ModelSize; x++)
            {
                int i = rowOffset + x * 3;
                tensor[0, 0, y, x] = (pixels[i] / 255f - Mean[0]) / Std[0];
                tensor[0, 1, y, x] = (pixels[i + 1] / 255f - Mean[1]) / Std[1];
                tensor[0, 2, y, x] = (pixels[i + 2] / 255f - Mean[2]) / Std[2];
            }
        }

        return tensor;
    }

    //---------------------------------------------------------------------
    private static BitmapSource ApplyMask(BitmapSource source, float[,] mask320)
    {
        int width = source.PixelWidth;
        int height = source.PixelHeight;

        var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        int stride = width * 4;
        var pixels = new byte[stride * height];
        converted.CopyPixels(pixels, stride, 0);

        for (int y = 0; y < height; y++)
        {
            int maskY = Math.Min(ModelSize - 1, (int)((double)y / height * ModelSize));
            for (int x = 0; x < width; x++)
            {
                int maskX = Math.Min(ModelSize - 1, (int)((double)x / width * ModelSize));
                float alpha = mask320[maskY, maskX]; // 0 = Hintergrund, 1 = Objekt

                int i = y * stride + x * 4;
                // Hintergrund weiß einfärben, Vordergrund unverändert lassen.
                pixels[i] = (byte)(pixels[i] * alpha + 255 * (1 - alpha));         // B
                pixels[i + 1] = (byte)(pixels[i + 1] * alpha + 255 * (1 - alpha)); // G
                pixels[i + 2] = (byte)(pixels[i + 2] * alpha + 255 * (1 - alpha)); // R
                pixels[i + 3] = 255;
            }
        }

        var result = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        result.Freeze();
        return result;
    }

    public void Dispose() => _session.Dispose();
}
