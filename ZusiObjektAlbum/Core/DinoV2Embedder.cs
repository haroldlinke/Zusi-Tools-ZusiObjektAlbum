using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ZusiObjektAlbum.Core;

public sealed class Dinov2Embedder : IImageEmbedder
{
  private const string InputName = "input"; // mit netron.app prüfen
  private const int ImageSize = 224; // manche DINOv2-Exporte nutzen 518 - prüfen!

  // WICHTIG: DINOv2 verwendet Standard-ImageNet-Normalisierung -
  // ANDERS als CLIP (dort waren es andere Mean/Std-Werte)!
  private static readonly float[] Mean = { 0.485f, 0.456f, 0.406f };
  private static readonly float[] Std = { 0.229f, 0.224f, 0.225f };

  private readonly InferenceSession _session;

  public Dinov2Embedder(string onnxModelPath)
  {
    var options = new SessionOptions { GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL };
    _session = new InferenceSession(onnxModelPath, options);
  }

  public float[] ComputeEmbedding(string imagePath)
  {
    BitmapSource bitmap = ImageFileLoader.Load(imagePath);
    return ComputeEmbedding(bitmap);
  }

  public float[] ComputeEmbedding(BitmapSource source)
  {
    return RunAndNormalize(PreprocessBitmapSource(source));
  }

  private float[] RunAndNormalize(DenseTensor<float> tensor)
  {
    var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(InputName, tensor) };
    using var results = _session.Run(inputs);

    // WICHTIG: Viele DINOv2-ONNX-Exporte liefern "last_hidden_state"
    // [1, num_patches+1, hidden_dim] statt eines fertig gepoolten
    // Vektors - das gesuchte Embedding ist dann Token 0 (CLS-Token).
    // Mit netron.app prüfen, ob euer Export stattdessen direkt einen
    // "pooler_output" [1, hidden_dim] liefert - dann reicht result.First()
    // wie bei ClipEmbedder. Falls nicht: hier die ersten hiddenDim
    // Werte herausschneiden (entspricht bei row-major Layout exakt
    // dem CLS-Token).
    var output = results.First();
    float[] raw = output.AsEnumerable<float>().ToArray();

    return Normalize(raw);
  }

  //private static DenseTensor<float> PreprocessImageSharp(Image<Rgb24> image)
  //{
  //  image.Mutate(ctx => ctx.Resize(new ResizeOptions { Size = new Size(ImageSize, ImageSize), Mode = ResizeMode.Stretch }));
  //  var tensor = new DenseTensor<float>(new[] { 1, 3, ImageSize, ImageSize });
  //  for (int y = 0; y < ImageSize; y++)
  //    for (int x = 0; x < ImageSize; x++)
  //    {
  //      Rgb24 pixel = image[x, y];
  //      tensor[0, 0, y, x] = (pixel.R / 255f - Mean[0]) / Std[0];
  //      tensor[0, 1, y, x] = (pixel.G / 255f - Mean[1]) / Std[1];
  //      tensor[0, 2, y, x] = (pixel.B / 255f - Mean[2]) / Std[2];
  //    }
  //  return tensor;
  //}

  private static DenseTensor<float> PreprocessBitmapSource(BitmapSource source)
  {
    double scaleX = (double)ImageSize / source.PixelWidth;
    double scaleY = (double)ImageSize / source.PixelHeight;
    var resized = new TransformedBitmap(source, new ScaleTransform(scaleX, scaleY));
    var converted = new FormatConvertedBitmap(resized, PixelFormats.Rgb24, null, 0);
    int stride = ImageSize * 3;
    var pixels = new byte[stride * ImageSize];
    converted.CopyPixels(pixels, stride, 0);

    var tensor = new DenseTensor<float>(new[] { 1, 3, ImageSize, ImageSize });
    for (int y = 0; y < ImageSize; y++)
    {
      int rowOffset = y * stride;
      for (int x = 0; x < ImageSize; x++)
      {
        int i = rowOffset + x * 3;
        tensor[0, 0, y, x] = (pixels[i] / 255f - Mean[0]) / Std[0];
        tensor[0, 1, y, x] = (pixels[i + 1] / 255f - Mean[1]) / Std[1];
        tensor[0, 2, y, x] = (pixels[i + 2] / 255f - Mean[2]) / Std[2];
      }
    }
    return tensor;
  }

  private static float[] Normalize(float[] vector)
  {
    double sumSquares = 0.0;
    foreach (float v in vector) sumSquares += v * v;
    double norm = Math.Sqrt(sumSquares);
    if (norm < 1e-12) return vector;
    var result = new float[vector.Length];
    for (int i = 0; i < vector.Length; i++) result[i] = (float)(vector[i] / norm);
    return result;
  }

  public void Dispose() => _session.Dispose();
}