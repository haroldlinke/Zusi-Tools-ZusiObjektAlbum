using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZusiKlassenLib;
using ZusiKlassenLib.Landscape;
using ZusiObjektAlbum.Core;
using ZusiObjektAlbum.Dialogs;
using ZusiObjektAlbum.MVVM;
using ZusiSimilaritySearch;

namespace ZusiObjektAlbum.Controls;

public sealed class QueryPhoto
{
  public required BitmapSource Original { get; set; }
  public BitmapSource Preview { get; set; } = null!; // ggf. ohne Hintergrund
  public string Label { get; init; } = "";
}

public partial class MainView : UserControl
{
  private static readonly string[] SupportedPhotoExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };
  private static readonly string[] SupportedThumbnailExtensions = { ".png", ".jpg", ".jpeg" };

  private readonly ObservableCollection<ResultItem> _results = new();

  private readonly ObservableCollection<QueryPhoto> _queryPhotos = new();


  // Werden beim ersten Suchlauf einmalig geladen und danach wiederverwendet -
  // Modell-Laden und Index-Laden sind die teuren Schritte, die man nicht pro
  // Anfrage wiederholen möchte.
  private IImageEmbedder? _embedder;
  private EmbeddingIndex? _index;

  private string? _currentPhotoPath;

  private BackgroundRemover? _backgroundRemover;

  private Window? _parentWindow;



  public MainView()
  {
    InitializeComponent();
    ResultsList.ItemsSource = _results;
    PhotosList.ItemsSource = _queryPhotos;
    this.Unloaded += MainView_Unloaded;
  }

  // ----------------------------------------------------------------
  // Foto-Auswahl: Drag & Drop
  // ----------------------------------------------------------------

  private void DropArea_DragOver(object sender, DragEventArgs e)
  {
    bool hasImage = e.Data.GetDataPresent(DataFormats.FileDrop)
                    && GetFirstImagePath(e.Data) is not null;

    e.Effects = hasImage ? DragDropEffects.Copy : DragDropEffects.None;
    e.Handled = true;
  }

  //private void DropArea_Drop(object sender, DragEventArgs e)
  //{
  //  string? path = GetFirstImagePath(e.Data);
  //  if (path is not null)
  //  {
  //    AddPhoto(LoadBitmap(path), Path.GetFileName(path));
  //  }
  //}

  private void DropArea_Drop(object sender, DragEventArgs e)
  {
    foreach (string path in GetImagePaths(e.Data))
    {
      AddPhoto(LoadBitmap(path), Path.GetFileName(path));
    }
  }

  private static IEnumerable<string> GetImagePaths(IDataObject data)
  {
    if (!data.GetDataPresent(DataFormats.FileDrop))
    {
      return Enumerable.Empty<string>();
    }

    var files = (string[])data.GetData(DataFormats.FileDrop)!;
    return files.Where(f => SupportedPhotoExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));
  }

  private static string? GetFirstImagePath(IDataObject data)
  {
    if (!data.GetDataPresent(DataFormats.FileDrop))
    {
      return null;
    }

    var files = (string[])data.GetData(DataFormats.FileDrop)!;

    return files.FirstOrDefault(f =>
        SupportedPhotoExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));
  }

  // ----------------------------------------------------------------
  // Foto-Auswahl: Klick -> Dateidialog
  // ----------------------------------------------------------------

  private void DropArea_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
  {
    DropArea.Focus(); // damit Ctrl+V danach greift, ohne extra hinklicken zu müssen
    var dialog = new OpenFileDialog
    {
      Multiselect = true,
      Title = "Foto(s) des realen Objekts auswählen",
      Filter = "Bilder (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Alle Dateien (*.*)|*.*"
    };

    var owner = Application.Current?.MainWindow;
    if (dialog.ShowDialog(owner) == true)
    {
      //AddPhoto(LoadBitmap(dialog.FileName), Path.GetFileName(dialog.FileName));
      foreach (string path in dialog.FileNames)
      {
        AddPhoto(LoadBitmap(path), Path.GetFileName(path));
      }
    }
  }

  //private void SetPhoto(string path)
  //{
  //  _currentPhotoPath = path;

  //  PhotoPreviewImage.Source = LoadBitmap(path);
  //  PhotoPreviewImage.Visibility = Visibility.Visible;
  //  DropHintText.Visibility = Visibility.Collapsed;

  //  SearchButton.IsEnabled = true;
  //  StatusText.Text = $"Foto geladen: {Path.GetFileName(path)}";
  //}

  //private static BitmapImage LoadBitmap(string path)
  //{
  //  var bitmap = new BitmapImage();
  //  bitmap.BeginInit();
  //  bitmap.CacheOption = BitmapCacheOption.OnLoad;
  //  bitmap.UriSource = new Uri(path, UriKind.Absolute);
  //  bitmap.EndInit();
  //  bitmap.Freeze();
  //  return bitmap;
  //}

private static BitmapSource LoadBitmap(string path)
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

// ----------------------------------------------------------------
// Suche
// ----------------------------------------------------------------

private DateTime _indexLoadedAt = DateTime.MinValue;


  //private void SetPhoto(string path)
  //{
  //  _currentPhotoPath = path;
  //  _originalPhotoBitmap = LoadBitmap(path);

  //  DropHintText.Visibility = Visibility.Collapsed;
  //  PhotoPreviewImage.Visibility = Visibility.Visible;
  //  SearchButton.IsEnabled = true;

  //  _ = UpdatePhotoPreviewAsync();
  //}

  private QueryPhoto? _selectedPhoto;

  private void PhotosList_SelectionChanged(object sender, SelectionChangedEventArgs e)
  {
    if (PhotosList.SelectedItem is QueryPhoto photo)
    {
      ShowInDropArea(photo);
    }
  }

  private void ShowInDropArea(QueryPhoto photo)
  {
    _selectedPhoto = photo;
    PhotoPreviewImage.Source = photo.Preview;
    PhotoPreviewImage.Visibility = Visibility.Visible;
    DropHintText.Visibility = Visibility.Collapsed;
  }

  private void AddPhoto(BitmapSource bitmap, string label)
  {
    if (bitmap.CanFreeze && !bitmap.IsFrozen)
    {
      bitmap.Freeze();
    }

    bool isFirstPhoto = _queryPhotos.Count == 0;

    var photo = new QueryPhoto { Original = bitmap, Preview = bitmap, Label = label };
    _queryPhotos.Add(photo);

    SearchButton.IsEnabled = true;
    StatusText.Text = $"{_queryPhotos.Count} Foto(s) ausgewählt.";

    if (isFirstPhoto)
    {
      // Erstes Foto: automatisch in der Drop-Area zeigen, ohne dass man
      // extra draufklicken muss. Löst PhotosList_SelectionChanged aus.
      PhotosList.SelectedItem = photo;
    }

    _ = UpdatePhotoPreviewAsync(photo);
  }

  private void RemovePhoto(QueryPhoto photo)
  {
    bool wasSelected = _selectedPhoto == photo;
    int index = _queryPhotos.IndexOf(photo);
    _queryPhotos.Remove(photo);

    if (_queryPhotos.Count == 0)
    {
      _selectedPhoto = null;
      PhotoPreviewImage.Visibility = Visibility.Collapsed;
      DropHintText.Visibility = Visibility.Visible;
      SearchButton.IsEnabled = false;
    }
    else if (wasSelected)
    {
      int newIndex = Math.Min(index, _queryPhotos.Count - 1);
      PhotosList.SelectedItem = _queryPhotos[newIndex]; // zeigt automatisch nach
      SearchButton.IsEnabled = true;
    }

    StatusText.Text = $"{_queryPhotos.Count} Foto(s) ausgewählt.";
  }

  private async Task UpdatePhotoPreviewAsync(QueryPhoto photo)
  {
    if (RemoveBackgroundCheckBox.IsChecked != true)
    {
      photo.Preview = photo.Original;
    }
    else
    {
      try
      {
        if (_backgroundRemover is null)
        {
          string u2netPath = DataManager.Instance.u2netModelPath;
          _backgroundRemover = await Task.Run(() => new BackgroundRemover(u2netPath));
        }

        photo.Preview = await Task.Run(() => _backgroundRemover.RemoveBackground(photo.Original));
      }
      catch (Exception ex)
      {
        photo.Preview = photo.Original;
        StatusText.Text = $"Hintergrundentfernung fehlgeschlagen: {ex.Message}";
      }
    }

    PhotosList.Items.Refresh(); // Thumbnail aktualisieren

    if (_selectedPhoto == photo)
    {
      PhotoPreviewImage.Source = photo.Preview; // große Vorschau mit aktualisieren
    }
    SearchButton.IsEnabled = true;
  }

  private void CorrectPerspectiveButton_Click(object sender, RoutedEventArgs e)
  {
    if (_selectedPhoto is null)
    {
      return;
    }

    var window = new PerspectiveCorrectionWindow(_selectedPhoto.Original) { Owner = Window.GetWindow(this) };
    if (window.ShowDialog() == true && window.Result != null)
    {
      _selectedPhoto.Original = window.Result;
      _ = UpdatePhotoPreviewAsync(_selectedPhoto); // wendet ggf. Hintergrundentfernung erneut an
    }
  }

  private async void RemoveBackgroundCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
  {
    foreach (var photo in _queryPhotos.ToList())
    {
      await UpdatePhotoPreviewAsync(photo);
    }
  }

  private void RemovePhotoButton_Click(object sender, RoutedEventArgs e)
  {
    if (sender is FrameworkElement fe && fe.Tag is QueryPhoto photo)
    {
      RemovePhoto(photo);
    }
  }


  private BitmapSource? _originalPhotoBitmap;

  private void SetPhoto(string path)
  {
    _currentPhotoPath = path;
    BitmapSource bitmap = LoadBitmap(path);
    SetPhotoFromBitmap(bitmap, Path.GetFileName(path));
  }

  private void SetPhotoFromClipboard(BitmapSource bitmap)
  {
    _currentPhotoPath = null; // kein Dateipfad vorhanden - ist ok, wird nirgends mehr vorausgesetzt
    AddPhoto(bitmap, "eingefügtes Bild (Zwischenablage)");
    //SetPhotoFromBitmap(bitmap, "eingefügtes Bild (Zwischenablage)");
  }

  private void SetPhotoFromBitmap(BitmapSource bitmap, string sourceLabel)
  {
    // Absicherung an zentraler Stelle: JEDES hereinkommende Bild einfrieren,
    // egal aus welcher Quelle. Ohne Freeze() ist das Bild an den Thread
    // gebunden, der es erzeugt hat - greift z.B. der Embedding-Task
    // (Task.Run) später darauf zu, kommt genau die gemeldete
    // InvalidOperationException ("Der aufrufende Thread kann nicht
    // zugreifen..."). Dateien waren schon ok, weil LoadBitmap() das schon
    // macht - Clipboard.GetImage() liefert dagegen ein nicht eingefrorenes
    // Bild.
    if (bitmap.CanFreeze && !bitmap.IsFrozen)
    {
      bitmap.Freeze();
    }


    _originalPhotoBitmap = bitmap;

    DropHintText.Visibility = Visibility.Collapsed;
    PhotoPreviewImage.Visibility = Visibility.Visible;
    SearchButton.IsEnabled = true;
    StatusText.Text = $"Foto geladen: {sourceLabel}";

    _ = UpdatePhotoPreviewAsync(); // berücksichtigt automatisch den aktuellen Stand der "Hintergrund entfernen"-Checkbox
  }

  //private async void RemoveBackgroundCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
  //{
  //  await UpdatePhotoPreviewAsync();
  //}

  private async Task UpdatePhotoPreviewAsync()
  {
    if (_originalPhotoBitmap is null)
    {
      return;
    }
    string u2netPath = DataManager.Instance.u2netModelPath;
    if (!File.Exists(u2netPath))
    {
      MessageBox.Show(
          Window.GetWindow(this),
          $"Es wurde noch kein REMbg-Modell gefunden unter:\n{u2netPath}\n\n" +
          "Bitte zuerst über \"Tools \u2192 Hintergrunderkennung-Modell von GitHub herunterladen...\" das Modell herunterladen.",
          "Kein Hintergrunderkennung-Modell vorhanden",
          MessageBoxButton.OK,
          MessageBoxImage.Warning);
      return;
    }

    if (RemoveBackgroundCheckBox.IsChecked != true)
    {
      PhotoPreviewImage.Source = _originalPhotoBitmap;
      //StatusText.Text = "Foto geladen.";
      return;
    }

    try
    {
      StatusText.Text = "Entferne Hintergrund für Vorschau...";

      if (_backgroundRemover is null)
      {
        
        _backgroundRemover = await Task.Run(() => new BackgroundRemover(u2netPath));
      }
      
      BitmapSource cleaned = await Task.Run(() => _backgroundRemover.RemoveBackground(_originalPhotoBitmap));
      PhotoPreviewImage.Source = cleaned;
      StatusText.Text = "Vorschau ohne Hintergrund - so wird auch gesucht.";
    }
    catch (Exception ex)
    {
      // Vorschau ist nur ein Komfortfeature - bei Fehlern lieber das
      // Originalfoto weiter zeigen, als die ganze Suche zu blockieren.
      PhotoPreviewImage.Source = _originalPhotoBitmap;
      StatusText.Text = $"Hintergrundentfernung fehlgeschlagen, zeige Original: {ex.Message}";
    }
  }

  private void PasteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
  {
    e.CanExecute = true;
  }

  private void PasteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
  {
    PasteFromClipboard();
  }

  // Sorgt dafür, dass Ctrl+V auch direkt nach dem Öffnen funktioniert, BEVOR
  // irgendwo hingeklickt wurde - Befehlssuche braucht ein fokussiertes
  // Element als Startpunkt, ohne das würde Ctrl+V ins Leere laufen.
  private void SimilaritySearchControl_Loaded(object sender, RoutedEventArgs e)
  {
    DropArea.Focus();
    _parentWindow = Window.GetWindow(this);
    if (_parentWindow != null)
    {
      _parentWindow.Activated += ParentWindow_Activated;
    }
  }

  private void ParentWindow_Activated(object? sender, EventArgs e)
  {
    // Nach Reaktivierung (z.B. nach Screenshot in anderer App, dann Klick
    // auf die Titelleiste) kann der Tastatur-Fokus komplett verloren
    // gegangen sein - Routed Commands wie Ctrl+V brauchen aber ein
    // fokussiertes Element als Ausgangspunkt für die Befehlssuche, sonst
    // läuft die Taste ins Leere. Zur Sicherheit Fokus zurücksetzen, falls
    // aktuell nichts fokussiert ist.
    if (Keyboard.FocusedElement == null)
    {
      DropArea.Focus();
    }
  }

  private void SimilaritySearchControl_Unloaded(object sender, RoutedEventArgs e)
  {
    if (_parentWindow != null)
    {
      _parentWindow.Activated -= ParentWindow_Activated;
    }
  }

  //private void DropArea_PreviewKeyDown(object sender, KeyEventArgs e)
  //{
  //  if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
  //  {
  //    PasteFromClipboard();
  //    e.Handled = true;
  //  }
  //}

  //private void PasteFromClipboard()
  //{
  //  if (!Clipboard.ContainsImage())
  //  {
  //    StatusText.Text = "Zwischenablage enthält kein Bild.";
  //    return;
  //  }

  //  try
  //  {
  //    BitmapSource clipboardImage = Clipboard.GetImage();
  //    SetPhotoFromClipboard(clipboardImage);
  //  }
  //  catch (Exception ex)
  //  {
  //    StatusText.Text = $"Einfügen aus Zwischenablage fehlgeschlagen: {ex.Message}";
  //  }
  //}

  private void PasteFromClipboard()
  {
    if (Clipboard.ContainsImage())
    {
      try
      {
        BitmapSource clipboardImage = Clipboard.GetImage();
        SetPhotoFromClipboard(clipboardImage);
      }
      catch (Exception ex)
      {
        StatusText.Text = $"Einfügen aus Zwischenablage fehlgeschlagen: {ex.Message}";
      }
      return;
    }

    if (Clipboard.ContainsFileDropList())
    {
      // Explorer legt beim Kopieren einer Datei (Ctrl+C) keine Bitmap in
      // die Zwischenablage, sondern eine Dateiliste (CF_HDROP) - genau
      // dasselbe Format wie beim Drag&Drop aus dem Explorer.
      StringCollection files = Clipboard.GetFileDropList();
      string? imagePath = files
          .Cast<string>()
          .FirstOrDefault(f => SupportedPhotoExtensions.Contains(
              Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));

      if (imagePath != null)
      {
        AddPhoto(LoadBitmap(imagePath), Path.GetFileName(imagePath));
        return;
      }

      StatusText.Text = "Die kopierte(n) Datei(en) sind kein unterstütztes Bildformat.";
      return;
    }

    StatusText.Text = "Zwischenablage enthält weder ein Bild noch eine Bilddatei.";
  }

  private async void SearchButton_Click(object sender, RoutedEventArgs e)
  {
    if (_queryPhotos.Count == 0)
    {
      return;
    }

    string modelPath = DataManager.Instance.modelPath; //ModelPathBox.Text.Trim();
    string indexPath = DataManager.Instance.indexPath;//IndexPathBox.Text.Trim();
    string objectsFolder = DataManager.Instance.objectsFolder; //ObjectsFolderBox.Text.Trim();

    if (!File.Exists(indexPath))
    {
      MessageBox.Show(
          Window.GetWindow(this),
          $"Es wurde noch keine index.bin gefunden unter:\n{indexPath}\n\n" +
          "Bitte zuerst über \"Tools \u2192 Index objects\" die Objektdatenbank indizieren.\n"+
          "oder über \"Tools \u2192 Index von Github herunerladen\" den Objektindex herunterladen.",
          "Kein Index vorhanden",
          MessageBoxButton.OK,
          MessageBoxImage.Warning);
      return;
    }

    SetBusy(true);
    _results.Clear();

    try
    {
      // Modell + Index nur beim ersten Mal (oder nach Pfadänderung) laden.
      if (_embedder is null)
      {
        StatusText.Text = "Lade ONNX-Modell...";
        //_embedder = await Task.Run(() => new ClipEmbedder(modelPath));
        _embedder = await Task.Run(() => new Dinov2Embedder(modelPath));
      }

      DateTime indexFileTime = File.GetLastWriteTimeUtc(indexPath);
      if (_index is null || indexFileTime > _indexLoadedAt)
      {
        StatusText.Text = "Lade Embedding-Index...";
        _index = await Task.Run(() => EmbeddingIndex.LoadFromFile(indexPath));
        _indexLoadedAt = indexFileTime;
      }
      var embeddings = new List<float[]>();
      foreach (var photo in _queryPhotos)
      {
        StatusText.Text = $"Berechne Embedding ({embeddings.Count + 1}/{_queryPhotos.Count})...";
        embeddings.Add(await Task.Run(() => _embedder.ComputeEmbedding(photo.Preview)));
      }

      StatusText.Text = "Suche ähnliche Objekte...";
      var matches = await Task.Run(() => _index.SearchMulti(embeddings, topN: 100));

      foreach (var match in matches)
      {
        //check if keyword filter is set and if so, check if the object has the keyword - moved this to the searchMulti method in EmbeddingIndex.cs
        //if (DataManager.Instance.MatchesKeywords(match.ObjectId) == false)
        //{
        //  continue;
        //}
        // replace zusi path saved in index.bin with local zusi path
        string local_sourcePath = "";
        if (match.SourcePath.StartsWith(DataManager.Instance.objectsFolder)) // correct sourcepath to real source path of current ZUSI installation
        {
          local_sourcePath = match.SourcePath.Substring(DataManager.Instance.objectsFolder.Length);
          DataPathType dtp = DataPathType.Official;
          local_sourcePath = Zusi.GetAbsolutePathOf(local_sourcePath,ref dtp); // normalize path
        }
        else
        {
          local_sourcePath = match.SourcePath;
        }
        string? thumbnail = FindThumbnail(objectsFolder, match.ObjectId, match.BestView);

        _results.Add(new ResultItem
        {
          ObjectId = match.ObjectId.Substring(0,match.ObjectId.Length-9), // remove hash
          Score = match.Score,
          BestView = match.BestView,
          SourcePath = local_sourcePath,
          ThumbnailPath = thumbnail
        });
      }

      StatusText.Text = $"Fertig - {matches.Count} Treffer gefunden.";
    }
    catch (Exception ex)
    {
      // Bei Pfad-/Modellfehlern: Embedder/Index verwerfen, damit beim
      // nächsten Versuch (nach Korrektur der Pfade) neu geladen wird.
      _embedder?.Dispose();
      _embedder = null;
      _index = null;

      StatusText.Text = "Fehler - siehe Meldung.";
      MessageBox.Show(Application.Current?.MainWindow, ex.Message, "Fehler bei der Suche",
          MessageBoxButton.OK, MessageBoxImage.Error);
    }
    finally
    {
      SetBusy(false);
    }
  }

  private void SetBusy(bool busy)
  {
    SearchButton.IsEnabled = !busy; // && _currentPhotoPath is not null;
    ProgressIndicator.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
  }

  private static string? FindThumbnail(string objectsFolder, string objectId, string viewName)
  {
    string dir = Path.Combine(objectsFolder, objectId);
    if (!Directory.Exists(dir))
    {
      return null;
    }

    foreach (string ext in SupportedThumbnailExtensions)
    {
      string candidate = Path.Combine(dir, viewName + ext);
      if (File.Exists(candidate))
      {
        return candidate;
      }
    }

    return null;
  }

  private void MainView_Unloaded(object? sender, RoutedEventArgs e)
  {
    _embedder?.Dispose();
  }

  private void On_ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
  {
    Zusi3DModel zusi3DModel = null;
    try
    {
      ResultItem resultitem = e.AddedItems[0] as ResultItem;

      LandschaftsDatei ld = new(resultitem.SourcePath);
      ld.Parse(true);
      ObjectModel value = new ObjectModel(ld.Root);
      if (value.Object is Landschaft)
      {
        zusi3DModel = new Zusi3DModel(value);
      }
    }
    catch (Exception ex)
    {
      //_log.Error(ex.ToString());
    }

    DataManager.Instance.SelectedObject = zusi3DModel;
  }
  private Point _dragStartPoint;

  private void ResultItemBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
  {
    _dragStartPoint = e.GetPosition(null);
  }

  private void ResultItemBorder_MouseMove(object sender, MouseEventArgs e)
  {
    if (e.LeftButton != MouseButtonState.Pressed)
    {
      return;
    }

    Point currentPosition = e.GetPosition(null);
    Vector diff = _dragStartPoint - currentPosition;

    bool draggedFarEnough =
        Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
        Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;

    if (!draggedFarEnough)
    {
      return;
    }

    if (sender is not FrameworkElement element || element.DataContext is not ResultItem item)
    {
      return;
    }

    if (string.IsNullOrEmpty(item.SourcePath) || !File.Exists(item.SourcePath))
    {
      return;
    }

    // Exakt das Format, das Explorer beim Datei-Ziehen erzeugt (CF_HDROP) -
    // jede Anwendung, die Drag&Drop vom Explorer akzeptiert, akzeptiert das auch.
    var dataObject = new DataObject(DataFormats.FileDrop, new[] { item.SourcePath });
    DragDrop.DoDragDrop(element, dataObject, DragDropEffects.Copy);
  }
}
