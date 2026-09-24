using log4net;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZusiObjektAlbum.MVVM;

namespace ZusiSimilaritySearch;

// Handles the keyword index for searching objects by keywords. The index is loaded from a text file where each line has the format:
// ObjectId;Keyword1,Keyword2,Keyword3

public sealed class KeywordIndex
{
  private static readonly ILog _log = LogManager.GetLogger(typeof(KeywordIndex));
  private readonly Dictionary<string, HashSet<string>> _map = new(StringComparer.OrdinalIgnoreCase);
  private readonly SortedSet<string> _all = new(StringComparer.OrdinalIgnoreCase);

  public IReadOnlyCollection<string> AllKeywords => _all;   // z.B. für Autovervollständigung

  public static KeywordIndex Load(string path)
  {
    var idx = new KeywordIndex();

    try
    {
      foreach (var line in System.IO.File.ReadLines(path, Encoding.UTF8))
      {
        if (string.IsNullOrWhiteSpace(line)) continue;
        string[] parts = line.Split(';');
        var name = parts[0].Trim().Replace(" ","_");
        if (name.Length == 0) continue;

        if (!idx._map.TryGetValue(name, out var set))
          idx._map[name] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kw in parts.Skip(1).SelectMany(p => p.Split(',')))
        {
          var k = kw.Trim();
          if (k.Length == 0) continue;
          set.Add(k);
          idx._all.Add(k);
        }
      }
    }
    catch (Exception ex)
    {
      _log.Error($"Fehler beim Laden des Keyword-Index aus '{path}': {ex.Message}", ex);
    }
    return idx;
  }

  /// Alle Suchbegriffe müssen zu mindestens einem Schlagwort passen (UND, Teilstring, ohne Groß-/Kleinschreibung).
  public bool Matches(string ObjectId, string[] terms)
  {
    if (!_map.TryGetValue(ObjectId, out var kws))
    {
      //_log.Debug("Keine Schlagworte für Objekt " + ObjectId);
      return terms.Any(t => ObjectId.Contains(t, StringComparison.OrdinalIgnoreCase)); // check if the object id itself contains any of the terms
    }
    //return terms.All(t => kws.Any(k => k.Contains(t, StringComparison.OrdinalIgnoreCase))); //UND
    return terms.Any(t => kws.Any(k => k.Contains(t, StringComparison.OrdinalIgnoreCase))); // ODER
  }
}

