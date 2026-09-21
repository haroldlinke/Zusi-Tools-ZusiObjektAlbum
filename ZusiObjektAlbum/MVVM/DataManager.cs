using log4net;
using SovomaLib;
using SovomaLib.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ZusiKlassenLib.Landscape;
using ZusiObjektAlbum.Miscellaneous;
using ZusiSimilaritySearch;

namespace ZusiObjektAlbum.MVVM
{
  public sealed class BeginImportFolderEventArgs : EventArgs
  {
    public int NumberOfFiles { get; private set; }

    public BeginImportFolderEventArgs(int numOfFiles)
    {
      NumberOfFiles = numOfFiles;
    }
  }

  public delegate void BeginImportFolderEventHandler(object sender, BeginImportFolderEventArgs e);

  public sealed class DataManager : DependencyObject
  {
    private static readonly ILog _log = LogManager.GetLogger(typeof(DataManager));
    private static DataManager __instance = null;

    private readonly ObservableCollection<ObjectModel> _objects = new();

    private readonly ExportFile _exportFile = new();

    //---------------------------------------------------------------------
    public static DataManager Instance
    {
      get
      {
        if (__instance == null)
        {
          __instance = new();
        }
        return __instance;
      }
    }

    //---------------------------------------------------------------------
    public static readonly DependencyProperty CreateSubFolderProperty = DependencyProperty.Register(
        "CreateSubFolder",
        typeof(bool),
        typeof(DataManager),
        new PropertyMetadata(true));
    public string CreateSubFolder
    {
      get => (string)GetValue(CreateSubFolderProperty);
      set => SetValue(CreateSubFolderProperty, value);
    }

#if false
        //---------------------------------------------------------------------
        public static readonly DependencyProperty ExportFolderProperty = DependencyProperty.Register(
            "ExportFolder",
            typeof(string),
            typeof(DataManager),
            new PropertyMetadata(null));
        public string ExportFolder
        {
            get => (string)GetValue(ExportFolderProperty);
            set => SetValue(ExportFolderProperty, value);
        }
#endif

    //---------------------------------------------------------------------
    public static readonly DependencyProperty HasGroundplateProperty = DependencyProperty.Register(
        "HasGroundplate",
        typeof(bool),
        typeof(DataManager),
        new PropertyMetadata(false, OnHasGroundplateChanged));
    public bool HasGroundplate
    {
      get => (bool)GetValue(HasGroundplateProperty);
      set => SetValue(HasGroundplateProperty, value);
    }

    //---------------------------------------------------------------------
    public static readonly DependencyProperty IsDarkObjectViewBackgroundProperty = DependencyProperty.Register(
        "IsDarkObjectViewBackground",
        typeof(bool),
        typeof(DataManager),
        new PropertyMetadata(false, OnIsDarkObjectViewBackgroundChanged));
    public bool IsDarkObjectViewBackground
    {
      get => (bool)GetValue(IsDarkObjectViewBackgroundProperty);
      set => SetValue(IsDarkObjectViewBackgroundProperty, value);
    }

    //---------------------------------------------------------------------
    private static readonly DependencyPropertyKey _isLoadDataKey = DependencyProperty.RegisterReadOnly(
        "IsLoadData",
        typeof(bool),
        typeof(DataManager),
        new PropertyMetadata(true));
    public static readonly DependencyProperty IsLoadDataProperty = _isLoadDataKey.DependencyProperty;
    public bool IsLoadData
    {
      get => (bool)GetValue(IsLoadDataProperty);
      private set => SetValue(_isLoadDataKey, value);
    }

    //---------------------------------------------------------------------
    public static readonly DependencyProperty KeepInMindFileProperty = DependencyProperty.Register(
        "KeepInMindFile",
        typeof(bool),
        typeof(DataManager),
        new PropertyMetadata(true));
    public bool KeepInMindFile
    {
      get => (bool)GetValue(KeepInMindFileProperty);
      set => SetValue(KeepInMindFileProperty, value);
    }

    //---------------------------------------------------------------------
    public static readonly DependencyProperty ObjectFileProperty = DependencyProperty.Register(
        "ObjectFile",
        typeof(string),
        typeof(DataManager),
        new PropertyMetadata(null));
    public string ObjectFile
    {
      get => (string)GetValue(ObjectFileProperty);
      set => SetValue(ObjectFileProperty, value);
    }

    //---------------------------------------------------------------------
    public static readonly DependencyProperty ObjectFolderProperty = DependencyProperty.Register(
        "ObjectFolder",
        typeof(string),
        typeof(DataManager),
        new PropertyMetadata(null));
    public string ObjectFolder
    {
      get => (string)GetValue(ObjectFolderProperty);
      set => SetValue(ObjectFolderProperty, value);
    }

    //---------------------------------------------------------------------
    public static readonly DependencyProperty ObjectViewBackgroundProperty = DependencyProperty.Register(
        "ObjectViewBackground",
        typeof(Brush),
        typeof(DataManager),
        new PropertyMetadata(Brushes.AliceBlue));
    public Brush ObjectViewBackground_
    {
      get => (Brush)GetValue(ObjectViewBackgroundProperty);
      set => SetValue(ObjectViewBackgroundProperty, value);
    }

    //---------------------------------------------------------------------
    public static readonly DependencyProperty NewSection1Property = DependencyProperty.Register(
        "NewSection1",
        typeof(string),
        typeof(DataManager),
        new PropertyMetadata(null));
    public string NewSection1
    {
      get => (string)GetValue(NewSection1Property);
      set => SetValue(NewSection1Property, value);
    }

    //---------------------------------------------------------------------
    public static readonly DependencyProperty NewSection2Property = DependencyProperty.Register(
        "NewSection2",
        typeof(string),
        typeof(DataManager),
        new PropertyMetadata(null));
    public string NewSection2
    {
      get => (string)GetValue(NewSection2Property);
      set => SetValue(NewSection2Property, value);
    }

    //---------------------------------------------------------------------
    private static readonly DependencyPropertyKey _numberOfObjectsKey = DependencyProperty.RegisterReadOnly(
        "NumberOfObjects",
        typeof(int),
        typeof(DataManager),
        new PropertyMetadata(0));
    public static readonly DependencyProperty NumberOfObjectsProperty = _numberOfObjectsKey.DependencyProperty;
    public int NumberOfObjects
    {
      get => (int)GetValue(NumberOfObjectsProperty);
      private set => SetValue(_numberOfObjectsKey, value);
    }


    //---------------------------------------------------------------------
    private static readonly DependencyPropertyKey _statusMessageKey = DependencyProperty.RegisterReadOnly(
        "StatusMessage",
        typeof(string),
        typeof(DataManager),
        new PropertyMetadata(null));
    public static readonly DependencyProperty StatusMessageProperty = _statusMessageKey.DependencyProperty;
    public string StatusMessage
    {
      get => (string)GetValue(StatusMessageProperty);
      private set => SetValue(_statusMessageKey, value);
    }

    //---------------------------------------------------------------------
    public static readonly DependencyProperty SelectedObjectProperty = DependencyProperty.Register(
            "SelectedObject",
            typeof(Zusi3DModel),
            typeof(DataManager),
            new PropertyMetadata(null));
    public Zusi3DModel SelectedObject
    {
      get => (Zusi3DModel)GetValue(SelectedObjectProperty);
      set => SetValue(SelectedObjectProperty, value);
    }

    //---------------------------------------------------------------------
    public static readonly DependencyProperty SelectedObjectModelProperty = DependencyProperty.Register(
        "SelectedObjectModel",
        typeof(ObjectModel),
        typeof(DataManager),
        new PropertyMetadata(null, OnSelectedObjectModelChanged));
    public ObjectModel SelectedObjectModel
    {
      get => (ObjectModel)GetValue(SelectedObjectModelProperty);
      set => SetValue(SelectedObjectModelProperty, value);
    }

    //---------------------------------------------------------------------
    public ObservableCollection<ObjectModel> Objects { get => _objects; }

    public ExportFile ExportFile { get => _exportFile; }

    //---------------------------------------------------------------------
    public event BeginImportFolderEventHandler BeginImportFolder;
    public event EventHandler DataLoadCompleted;
    public event EventHandler FinishedImportFolder;

    public string modelPath = "";
    public string indexPath = "";
    public string objectsFolder = "C:\\Program Files\\Zusi3\\_ZusiData";
    public string u2netModelPath = ""; //"C:\\Users\\Public\\Documents\\Zusi3\\_Tools\\ZusiObjektAlbum\\u2net.onnx";

    //---------------------------------------------------------------------
    public DataManager()
    {
      EnumObjectsAsync();

      //KeywordView = new ListCollectionView(_keywords.AllKeywords.ToList());
      //KeywordView.Filter = o =>
      //{
      //  var text = FilterText?.Trim();
      //  return string.IsNullOrEmpty(text) ||
      //         ((string)o).StartsWith(text, StringComparison.OrdinalIgnoreCase);
      //};
      KeywordView = new ListCollectionView(_keywords.AllKeywords.ToList());
      KeywordView.Filter = o =>
      {
        var kw = (string)o;
        if (SelectedKeywords.Contains(kw, StringComparer.OrdinalIgnoreCase)) return false; // schon gewählt
        var input = KeywordInput?.Trim();
        return string.IsNullOrEmpty(input) || kw.StartsWith(input, StringComparison.OrdinalIgnoreCase);
      };
      SelectedKeywords.CollectionChanged += (_, _) => { ApplyFilter(); KeywordView.Refresh(); };
    }


    public static readonly DependencyProperty FilterTextProperty =
    DependencyProperty.Register(nameof(FilterText), typeof(string), typeof(DataManager),
        new PropertyMetadata("", (d, e) => ((DataManager)d).ApplyFilter()));

    public string FilterText
    {
      get => (string)GetValue(FilterTextProperty);
      set => SetValue(FilterTextProperty, value);
    }

    public ObservableCollection<string> SelectedKeywords { get; } = new();
    public ICollectionView KeywordView { get; }


    private bool _isFilterVisible = true;
    public bool IsFilterVisible
    {
      get => _isFilterVisible;
      set { if (_isFilterVisible == value) return; _isFilterVisible = value; RaisePropertyChanged(nameof(IsFilterVisible)); }
    }

    public bool? ExpandedBeforeFilter { get; set; }   // normale Property, braucht keine Benachrichtigung


    private readonly KeywordIndex _keywords = KeywordIndex.Load("D:\\Zusi\\displayname_keywords.csv");

    public event PropertyChangedEventHandler PropertyChanged;
    private void RaisePropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private string _keywordInput = "";
    public string KeywordInput
    {
      get => _keywordInput;
      set { if (_keywordInput == value) return; _keywordInput = value; /* Benachrichtigung */ KeywordView.Refresh(); }
    }

    public void AddKeyword(string kw)
    {
      if (!string.IsNullOrWhiteSpace(kw) && !SelectedKeywords.Contains(kw, StringComparer.OrdinalIgnoreCase))
        SelectedKeywords.Add(kw);
    }
    public void RemoveKeyword(string kw) => SelectedKeywords.Remove(kw);

    public void RemoveLastKeyword() { if (SelectedKeywords.Count > 0) SelectedKeywords.RemoveAt(SelectedKeywords.Count - 1); }

    public void ClearKeywords() => SelectedKeywords.Clear();


    private void ApplyFilter()
    {
      //var terms = FilterText.Split(new[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

      //  var terms = string.IsNullOrWhiteSpace(FilterText)
      //? Array.Empty<string>()
      //: new[] { FilterText.Trim() };

      string[] terms = SelectedKeywords.ToArray();

      foreach (var root in Objects)
        ApplyFilter(root, terms, false);

      KeywordView.Refresh();
    }

    private bool ApplyFilter(ObjectModel node, string[] terms,bool parentMatch)
    {
      bool anyChildVisible = false;

      bool selfMatch = terms.Length == 0 || parentMatch || _keywords.Matches(node.DisplayName, terms);
      foreach (var child in node.Children)              // kein Short-Circuit, alle Kinder müssen aktualisiert werden
        anyChildVisible |= ApplyFilter(child, terms, selfMatch);

      //bool selfMatch = terms.Length == 0 || _keywords.Matches(node.DisplayName, terms);
      node.IsFilterVisible = selfMatch || anyChildVisible;

      if (node.Children.Count == 0 & terms.Count() != 0)
        node.IsErroneous = node.IsFilterVisible;

      if (selfMatch)
      {
        _log.Debug("SelfMatch = True");
      }

      if (terms.Length > 0)
      {
        node.ExpandedBeforeFilter ??= node.IsExpanded;
        node.IsExpanded = anyChildVisible;            // Eltern von Treffern aufklappen
      }
      else if (node.ExpandedBeforeFilter is bool was)
      {
        node.IsExpanded = was;                        // Filter gelöscht: alten Zustand wiederherstellen
        node.ExpandedBeforeFilter = null;
      }
      return node.IsFilterVisible;
    }


    //---------------------------------------------------------------------
    public void OnImportObject(object sender, ExecutedRoutedEventArgs e)
    {
      ImportObject(ObjectFile, NewSection1, true);
      CountObjects();
      if (KeepInMindFile)
      {
        KeepInMind.AddFile(ObjectFile, NewSection1);
        KeepInMind.Save();
      }
    }

    //---------------------------------------------------------------------
    public void OnImportFolder(object sender, ExecutedRoutedEventArgs e)
    {
      int n = ImportFolder(ObjectFolder, NewSection2);
      if (n > 0)
      {
        CountObjects();

        string m = string.Format("Aus dem Ordner '{0}' wurde", ObjectFolder);
        string msg = n == 1 ? string.Format("{0} 1 Objekt importiert.", m) : string.Format("{0}n {1} Objekte importiert.", m, n);
        MessageBox.Show(msg, "Objectimport", MessageBoxButton.OK, MessageBoxImage.Information);
      }
      else
      {
        MessageBox.Show(string.Format("Im Ordner '{0}' wurden keine Objekte gefunden", ObjectFolder), "Objectimport", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
    }

    //---------------------------------------------------------------------
    public void UpdateMyObjects()
    {
      ObjectModel myObjectModel = _objects.FirstOrDefault(om => om.Object == null && om.DisplayName == "Eigene Objekte");
      if (myObjectModel != null)
      {
        myObjectModel.Children.Clear();
        ImportMyObjects();
        CountObjects();
        if (myObjectModel.Children.Count == 0)
        {
          _objects.Remove(myObjectModel);
        }
      }
    }

    //---------------------------------------------------------------------
    private bool ImportObject(string file, string section, bool throwExceptionIfExists)
    {
      bool result = false;

      if (!File.Exists(file))
      {
        return false;
      }

      try
      {
        LandschaftsDatei ld = new(file);
        ld.Parse(true);
        ObjectModel newModel = new(ld.Root);

        ObjectModel myObjectModel = _objects.FirstOrDefault(om => om.Object == null && om.DisplayName == "Eigene Objekte");
        if (myObjectModel == null)
        {
          myObjectModel = new ObjectModel("Eigene Objekte", 1);
          _objects.Add(myObjectModel);
        }
        else
        {
          if (myObjectModel.SearchByDisplayName(newModel.DisplayName) != null)
          {
            if (throwExceptionIfExists)
            {
              throw new Exception(string.Format("Ein Objekt mit dem Namen '{0}' ist bereits vorhanden", newModel.DisplayName));
            }
            else
            {
              return false;
            }
          }
        }

        ObjectModel my = myObjectModel;
        int lvl = 1;
        if (!string.IsNullOrEmpty(section))
        {
          string[] ss = section.Split('/');
          foreach (string s in ss)
          {
            string s2 = s.Replace("\"", null).Replace("'", null).Trim();
            if (string.IsNullOrEmpty(s2))
              continue;

            ObjectModel om = my.Children.FirstOrDefault(o => string.Compare(o.DisplayName, s2, true) == 0);
            if (om == null)
            {
              om = new ObjectModel(s2, lvl++) { IsReadOnly = false };
              my.Children.Add(om);
            }
            my = om;
          }
        }

        my.Children.Add(newModel);
        myObjectModel.Initialize();
        result = true;
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message);
      }

      return result;
    }

    //---------------------------------------------------------------------
    private int ImportFolder(string folder, string section)
    {
      int imported = 0;
      if (Directory.Exists(folder))
      {
        string[] files = Directory.GetFiles(folder, "*.lod.ls3", SearchOption.AllDirectories);
        if (files.Length > 0)
        {
          folder = folder.EnsureTrailingBackslash();
          foreach (string file in files)
          {
            string path = Path.GetDirectoryName(file).StripPrefix(folder);
            if (!string.IsNullOrEmpty(section))
            {
              path = string.Format("{0}\\{1}", section, path);
            }

            if (ImportObject(file, path.Replace('\\', '/'), false))
            {
              imported++;
            }
          }
        }
      }

      return imported;
    }

    //---------------------------------------------------------------------
    public void OnCanClearExportFile(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = !string.IsNullOrEmpty(ExportFile.Filename);
    }

    //---------------------------------------------------------------------
    public void OnClearExportFile(object sender, ExecutedRoutedEventArgs e)
    {
      ExportFile.Clear();
    }

    //---------------------------------------------------------------------
    private static void OnHasGroundplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      (d as DataManager)?.OnHasGroundplateChanged((bool)e.NewValue);
    }

    //---------------------------------------------------------------------
    private void OnHasGroundplateChanged(bool value)
    {
      if (SelectedObject != null)
      {
        if (value)
        {
          SelectedObject.AddGroundplate();
        }
        else
        {
          SelectedObject.RemoveGroundplate();
        }
      }
    }

    //---------------------------------------------------------------------
    private static void OnIsDarkObjectViewBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      (d as DataManager)?.OnIsDarkObjectViewBackgroundChanged((bool)e.NewValue);
    }

    //---------------------------------------------------------------------
    private void OnIsDarkObjectViewBackgroundChanged(bool value)
    {
      ObjectViewBackground_ = value ? Brushes.MidnightBlue : Brushes.AliceBlue;
    }

    //---------------------------------------------------------------------
    private static void OnSelectedObjectModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      (d as DataManager)?.OnSelectedObjectModelChanged((ObjectModel)e.NewValue);
    }

    //---------------------------------------------------------------------
    private void OnSelectedObjectModelChanged(ObjectModel value)
    {
      Zusi3DModel zusi3DModel = null;

      if (value != null && value.Object != null)
      {
        if (value.Object is LandscapeObject lo)
        {
          try
          {
            LandschaftsDatei ld = new(lo.Filename);
            ld.Parse(true);
            value.Object = ld.Root;
          }
          catch (Exception ex)
          {
            _log.Error(ex.ToString());
          }
        }

        if (value.Object is Landschaft)
        {
          zusi3DModel = new Zusi3DModel(value);
        }
      }

      SelectedObject = zusi3DModel;
    }

    //---------------------------------------------------------------------
    private void OnBeginImportFolder(int numOfFiles)
    {
      BeginImportFolder?.Invoke(this, new BeginImportFolderEventArgs(numOfFiles));
    }

    //---------------------------------------------------------------------
    private void OnDataLoadCompleted()
    {
      DataLoadCompleted?.Invoke(this, EventArgs.Empty);
    }

    //---------------------------------------------------------------------
    private void OnFinishedImportFolder()
    {
      FinishedImportFolder?.Invoke(this, EventArgs.Empty);
    }

    //---------------------------------------------------------------------
    private async void EnumObjectsAsync()
    {
      
      _log.Debug("Starting to enumerate objects.");
      ObservableCollection<ObjectModel> temp = await Task.Run(() => { return ObjectModel.Create(); });
      foreach (ObjectModel o in temp)
      {
        _objects.Add(o);
      }

      ImportMyObjects();

      CountObjects();

      OnDataLoadCompleted();
      IsLoadData = false;
    }

    //---------------------------------------------------------------------
    private void ImportMyObjects()
    {
      List<string> temp = new();
      KeepInMind.Files.ForEach(f =>
      {
        if (!ImportObject(f.Item, f.Section, false))
        {
          temp.Add(f.Item);
        }
      });
      temp.ForEach(s => KeepInMind.RemoveFile(s));
      KeepInMind.Save();
      KeepInMind.Folders.ForEach(f => ImportFolder(f.Item, f.Section));
    }

    //---------------------------------------------------------------------
    private void CountObjects()
    {
      int n = 0;
      foreach (ObjectModel om in _objects)
      {
        n += DoCountObjects(om);
      }
      NumberOfObjects = n;
    }

    //---------------------------------------------------------------------
    private int DoCountObjects(ObjectModel objectModel)
    {
      if (objectModel.Object != null)
      {
        return 1;
      }
      else
      {
        int n = 0;
        foreach (ObjectModel om in objectModel.Children)
        {
          n += DoCountObjects(om);
        }
        return n;
      }
    }

    public async Task SetStatusMessage(string message)
    {
      StatusMessage = message;
      await Task.Yield();
    }
  }
}
