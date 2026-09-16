using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ZusiKlassenLib.Landscape;
using ZusiObjektAlbum.Core;
using ZusiObjektAlbum.MVVM;
using ZusiSimilaritySearch;
using ZusiKlassenLib;
using System.Collections.Specialized;

namespace ZusiObjektAlbum.Controls;

public partial class MainView : UserControl
{
  private static readonly string[] SupportedPhotoExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };
  private static readonly string[] SupportedThumbnailExtensions = { ".png", ".jpg", ".jpeg" };

  private readonly ObservableCollection<ResultItem> _results = new();


  // Werden beim ersten Suchlauf einmalig geladen und danach wiederverwendet -
  // Modell-Laden und Index-Laden sind die teuren Schritte, die man nicht pro
  // Anfrage wiederholen möchte.
  private ClipEmbedder? _embedder;
  private EmbeddingIndex? _index;

  private string? _currentPhotoPath;

  private BackgroundRemover? _backgroundRemover;

  private Window? _parentWindow;

  public MainView()
  {
    InitializeComponent();
    ResultsList.ItemsSource = _results;
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

  private void DropArea_Drop(object sender, DragEventArgs e)
  {
    string? path = GetFirstImagePath(e.Data);
    if (path is not null)
    {
      SetPhoto(path);
    }
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
      Title = "Foto des realen Objekts auswählen",
      Filter = "Bilder (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Alle Dateien (*.*)|*.*"
    };

    var owner = Application.Current?.MainWindow;
    if (dialog.ShowDialog(owner) == true)
    {
      SetPhoto(dialog.FileName);
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

  private static BitmapImage LoadBitmap(string path)
  {
    var bitmap = new BitmapImage();
    bitmap.BeginInit();
    bitmap.CacheOption = BitmapCacheOption.OnLoad;
    bitmap.UriSource = new Uri(path, UriKind.Absolute);
    bitmap.EndInit();
    bitmap.Freeze();
    return bitmap;
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
    SetPhotoFromBitmap(bitmap, "eingefügtes Bild (Zwischenablage)");
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

  private async void RemoveBackgroundCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
  {
    await UpdatePhotoPreviewAsync();
  }

  private async Task UpdatePhotoPreviewAsync()
  {
    if (_originalPhotoBitmap is null)
    {
      return;
    }
    string u2netPath = DataManager.Instance.rembgModelPath;
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
        SetPhoto(imagePath); // vorhandener Datei-Ladeweg, inkl. Freeze() in LoadBitmap()
        return;
      }

      StatusText.Text = "Die kopierte(n) Datei(en) sind kein unterstütztes Bildformat.";
      return;
    }

    StatusText.Text = "Zwischenablage enthält weder ein Bild noch eine Bilddatei.";
  }

  private async void SearchButton_Click(object sender, RoutedEventArgs e)
  {
    if (_originalPhotoBitmap is null)
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
          "Bitte zuerst über \"Tools \u2192 Index objects\" die Objektdatenbank indizieren.",
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
        _embedder = await Task.Run(() => new ClipEmbedder(modelPath));
      }

      //if (_index is null)
      //{
      //  StatusText.Text = "Lade Embedding-Index...";
      //  _index = await Task.Run(() => EmbeddingIndex.LoadFromFile(indexPath));
      //}

      DateTime indexFileTime = File.GetLastWriteTimeUtc(indexPath);
      if (_index is null || indexFileTime > _indexLoadedAt)
      {
        StatusText.Text = "Lade Embedding-Index...";
        _index = await Task.Run(() => EmbeddingIndex.LoadFromFile(indexPath));
        _indexLoadedAt = indexFileTime;
      }

      StatusText.Text = "Berechne Embedding für das Foto...";
      //float[] queryEmbedding = await Task.Run(() => _embedder.ComputeEmbedding(_currentPhotoPath));

      var previewBitmap = (BitmapSource)PhotoPreviewImage.Source;
      float[] queryEmbedding = await Task.Run(() => _embedder.ComputeEmbedding(previewBitmap));


//      float[] queryEmbedding;

//if (RemoveBackgroundCheckBox.IsChecked == true)
//{
//    if (_backgroundRemover is null)
//    {
//        StatusText.Text = "Lade Hintergrund-Entfernungs-Modell...";
//        string u2netPath = DataManager.Instance.u2netModelPath; // Pfad analog zu modelPath/indexPath anlegen
//  _backgroundRemover = await Task.Run(() => new BackgroundRemover(u2netPath));
//    }

//StatusText.Text = "Entferne Hintergrund...";
//    BitmapSource photoBitmap = LoadBitmap(_currentPhotoPath); // vorhandene Hilfsmethode aus dem Foto-Preview
//BitmapSource cleaned = await Task.Run(() => _backgroundRemover.RemoveBackground(photoBitmap));

//StatusText.Text = "Berechne Embedding für das Foto...";
//    queryEmbedding = await Task.Run(() => _embedder.ComputeEmbedding(cleaned)); // BitmapSource-Overload, kennt ClipEmbedder schon
//}
//else
//{
//  StatusText.Text = "Berechne Embedding für das Foto...";
//  queryEmbedding = await Task.Run(() => _embedder.ComputeEmbedding(_currentPhotoPath));
//}



StatusText.Text = "Suche ähnliche Objekte...";
      var matches = await Task.Run(() => _index.Search(queryEmbedding, topN: 100));

      foreach (var match in matches)
      {
        

        // replace zusi path saved in index.bin with local zusi path


        string local_sourcePath = "";
        if (match.SourcePath.StartsWith(DataManager.Instance.objectsFolder))
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
          ObjectId = match.ObjectId,
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
    SearchButton.IsEnabled = !busy && _currentPhotoPath is not null;
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
