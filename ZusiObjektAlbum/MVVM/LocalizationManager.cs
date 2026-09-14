using log4net;
using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ZusiObjektAlbum;


namespace ZusiObjektAlbum.MVVM
{

  public static class LocalizationManager
  {
    private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    private static readonly Dictionary<string, Dictionary<string, string>> _translations = new();
    //public static string CurrentLanguage { get; set; } = "de";

    public static void Load()
    {
      string zusistart_language_relativePath = "Assets/Language_table.txt";
      string zusistart_language_absolutePath = Path.GetFullPath(zusistart_language_relativePath);

      foreach (var line in File.ReadLines(zusistart_language_absolutePath))
      {
        if (string.IsNullOrWhiteSpace(line)) continue;
        var parts = line.Split('\"');

        int idx_de = 1;
        int idx_en = 3;
        int idx_fr = 5;

        if (parts.Length < 7)
        {
          parts = line.Split(',');
          if (parts.Length < 3) continue;
          idx_de = 0;
          idx_en = 1;
          idx_fr = 2;
        }

        var de = parts[idx_de].Trim();
        var en = parts[idx_en].Trim();
        var fr = parts[idx_fr].Trim();


        _translations[de] = new Dictionary<string, string>
            {
                { "de", de },
                { "en", en },
                { "fr", fr }
            };
      }
    }

    public static string Translate(string germanText)
    {
      //_log.Debug($"Translating: {germanText} to {DataManager.CurrentLanguage.ToString()}");
      try
      {
        germanText = germanText.Trim();
        //if (_translations.TryGetValue(germanText, out var dict))
        //{
        //  if (dict.TryGetValue(DataManager.CurrentLanguage, out var result))
        //    return result;
        //}
        //else
        //{
        //  // If the text is not found in the dictionary, you might want to log this or handle it accordingly.
        //  // For now, we'll just return the original German text.
        //  _log.Debug($"Translation not found for: {germanText}");
        //}
        return germanText;
      }
      catch (Exception ex)
      {
        _log.Error($"Error during translation of '{germanText}': {ex.Message}");
        return germanText; // Return the original text in case of an error
      }
    }
  }
}