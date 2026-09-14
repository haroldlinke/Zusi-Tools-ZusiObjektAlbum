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

  private void SetPhoto(string path)
  {
    _currentPhotoPath = path;

    PhotoPreviewImage.Source = LoadBitmap(path);
    PhotoPreviewImage.Visibility = Visibility.Visible;
    DropHintText.Visibility = Visibility.Collapsed;

    SearchButton.IsEnabled = true;
    StatusText.Text = $"Foto geladen: {Path.GetFileName(path)}";
  }

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

  private async void SearchButton_Click(object sender, RoutedEventArgs e)
  {
    if (_currentPhotoPath is null)
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
      float[] queryEmbedding = await Task.Run(() => _embedder.ComputeEmbedding(_currentPhotoPath));

      StatusText.Text = "Suche ähnliche Objekte...";
      var matches = await Task.Run(() => _index.Search(queryEmbedding, topN: 100));

      foreach (var match in matches)
      {
        string? thumbnail = FindThumbnail(objectsFolder, match.ObjectId, match.BestView);

        _results.Add(new ResultItem
        {
          ObjectId = match.ObjectId,
          Score = match.Score,
          BestView = match.BestView,
          SourcePath = match.SourcePath,
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
