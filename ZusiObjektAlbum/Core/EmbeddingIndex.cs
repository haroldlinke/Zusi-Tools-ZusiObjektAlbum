using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ZusiObjektAlbum.Core;

/// <summary>
/// Ein einzelnes Suchergebnis: bestes Ansicht-Match für ein Objekt,
/// inklusive des echten Pfads zur ursprünglichen .ls3-Datei.
/// </summary>
public readonly record struct SearchHit(string ObjectId, float Score, string BestView, string SourcePath);

/// <summary>
/// Hält alle Embeddings im Speicher und bietet eine einfache Brute-Force-
/// Ähnlichkeitssuche per Cosine-Similarity. Da alle Vektoren bereits L2-normiert
/// sind, entspricht Cosine-Similarity einem einfachen Skalarprodukt.
///
/// Unterstützt inkrementelle Updates: ContainsObject/RemoveObject erlauben es,
/// eine bestehende index.bin zu laden und nur neue oder geänderte Objekte
/// (neu) einzubetten, statt immer den kompletten Index neu aufzubauen.
/// </summary>
public sealed class EmbeddingIndex
{
  // Bei Änderungen am Binärformat (z.B. neues Feld) IMMER erhöhen - alte
  // Indexdateien mit anderer Version werden dann bewusst abgelehnt, statt
  // im besten Fall falsch oder im schlimmsten Fall gar nicht zu laden.
  private const int FormatVersion = 2;

  private readonly List<ViewEmbedding> _entries = new();
  private readonly HashSet<string> _objectIds = new();

  public int Count => _entries.Count;
  public int ObjectCount => _objectIds.Count;

  public void Add(ViewEmbedding entry)
  {
    _entries.Add(entry);
    _objectIds.Add(entry.ObjectId);
  }

  public bool ContainsObject(string objectId) => _objectIds.Contains(objectId);

  /// <summary>
  /// Entfernt alle Ansichten eines Objekts (z.B. vor dem Neu-Einbetten nach
  /// einem erneuten Rendering), falls vorhanden.
  /// </summary>
  public void RemoveObject(string objectId)
  {
    if (!_objectIds.Remove(objectId))
    {
      return;
    }

    _entries.RemoveAll(e => e.ObjectId == objectId);
  }

  /// <summary>
  /// Liefert die Top-N Objekte, sortiert nach dem besten Score über alle
  /// vier Ansichten (front/back/left/right) hinweg.
  /// </summary>
  public IReadOnlyList<SearchHit> Search(float[] query, int topN)
  {
    var bestPerObject = new Dictionary<string, (float Score, string View, string SourcePath)>();

    foreach (var entry in _entries)
    {
      float score = CosineSimilarity(query, entry.Vector);

      if (!bestPerObject.TryGetValue(entry.ObjectId, out var current) || score > current.Score)
      {
        bestPerObject[entry.ObjectId] = (score, entry.ViewName, entry.SourcePath);
      }
    }

    return bestPerObject
        .Select(kv => new SearchHit(kv.Key, kv.Value.Score, kv.Value.View, kv.Value.SourcePath))
        .OrderByDescending(x => x.Score)
        .Take(topN)
        .ToList();
  }

  private static float CosineSimilarity(float[] a, float[] b)
  {
    int length = a.Length;
    int simdWidth = System.Numerics.Vector<float>.Count;
    int i = 0;
    float sum = 0f;

    for (; i <= length - simdWidth; i += simdWidth)
    {
      var va = new System.Numerics.Vector<float>(a, i);
      var vb = new System.Numerics.Vector<float>(b, i);
      sum += System.Numerics.Vector.Dot(va, vb);
    }

    for (; i < length; i++)
    {
      sum += a[i] * b[i];
    }

    return sum;
  }

  public void SaveToFile(string path)
  {
    // In eine temporäre Datei schreiben und dann atomar umbenennen, damit
    // bei einem Absturz/Abbruch mitten im Speichern (z.B. während des
    // periodischen Zwischenspeicherns über 12.000 Objekte) nicht die
    // vorhandene, funktionierende index.bin beschädigt zurückbleibt.
    string tempPath = path + ".tmp";
    string dir = System.IO.Path.GetDirectoryName(path);
    if (!string.IsNullOrEmpty(dir))
    {
      System.IO.Directory.CreateDirectory(dir);
    }

    using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
    using (var bw = new BinaryWriter(fs))
    {
      bw.Write(FormatVersion);
      bw.Write(_entries.Count);
      foreach (var entry in _entries)
      {
        bw.Write(entry.ObjectId);
        bw.Write(entry.ViewName);
        bw.Write(entry.SourcePath);
        bw.Write(entry.Vector.Length);
        foreach (float f in entry.Vector)
        {
          bw.Write(f);
        }
      }
    }

    File.Copy(tempPath, path, overwrite: true);
    File.Delete(tempPath);
  }

  public static EmbeddingIndex LoadFromFile(string path)
  {
    var index = new EmbeddingIndex();

    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
    using var br = new BinaryReader(fs);

    int version = br.ReadInt32();
    if (version != FormatVersion)
    {
      throw new InvalidDataException(
          $"Index-Datei '{path}' hat Format-Version {version}, erwartet wird {FormatVersion}. " +
          "Bitte den Index neu aufbauen (alte index.bin löschen bzw. umbenennen).");
    }

    int count = br.ReadInt32();
    for (int n = 0; n < count; n++)
    {
      string objectId = br.ReadString();
      string viewName = br.ReadString();
      string sourcePath = br.ReadString();
      int vecLen = br.ReadInt32();

      var vector = new float[vecLen];
      for (int i = 0; i < vecLen; i++)
      {
        vector[i] = br.ReadSingle();
      }

      index.Add(new ViewEmbedding(objectId, viewName, vector, sourcePath));
    }

    return index;
  }
}
