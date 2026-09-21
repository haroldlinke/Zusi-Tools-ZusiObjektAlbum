using log4net;
using SovomaLib;
using SovomaLib.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPFTreeViewLib;
using ZusiKlassenLib;
using ZusiKlassenLib.Common;
using ZusiKlassenLib.Landscape;
using ZusiObjektAlbum.Controls;
using ZusiObjektAlbum.Dialogs;
using ZusiObjektAlbum.Miscellaneous;
using ZusiObjektAlbum.ModelDownloader;
using ZusiObjektAlbum.MVVM;
using ZusiObjektAlbum.ValidationRules;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace ZusiObjektAlbum
{
  /// <summary>
  /// Interaktionslogik für MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private static readonly ILog Log = LogManager.GetLogger(typeof(MainWindow));

    private bool _dataLoadComplete;

    /// <summary>
    /// Gets the routed UI command for indexing objects.
    /// </summary>
    public static readonly RoutedUICommand CommandIndexObjects = new RoutedUICommand("Objekte indexieren", nameof(CommandIndexObjects), typeof(MainWindow));

    //---------------------------------------------------------------------
    public MainWindow()
    {
      InitializeComponent();

      Closing += MainWindow_Closing;
      Loaded += MainWindow_Loaded;

      TreeViewInPlaceEditBoxBehavior.AddEditCompletedHandler(this, new EditCompletedEventHandler(ObjectView_EditCompleted));

      CommandBindings.Add(new CommandBinding(ZOACommands.ImportObjectCommand, DataManager.Instance.OnImportObject, OnCanImportObject));
      CommandBindings.Add(new CommandBinding(ZOACommands.ImportFolderCommand, DataManager.Instance.OnImportFolder, OnCanImportFolder));
      CommandBindings.Add(new CommandBinding(ZOACommands.RenameSectionCommand, OnRenameSection, OnCanRenameSection));
      CommandBindings.Add(new CommandBinding(ZOACommands.ManageFoldersCommand, OnManageFolders));
      CommandBindings.Add(new CommandBinding(ZOACommands.RemoveObjectCommand, OnRemoveObject));
      CommandBindings.Add(new CommandBinding(ZOACommands.CopyPathToClipboardCommand, OnCopyPathToClipboard, OnCanCopyPathToClipboard));
      CommandBindings.Add(new CommandBinding(ZOACommands.CopyFilenamePathToClipboardCommand, OnCopyFilenamePathToClipboard, OnCanCopyFilenamePathToClipboard));
      CommandBindings.Add(new CommandBinding(ZOACommands.ClearExportFileCommand, DataManager.Instance.OnClearExportFile, DataManager.Instance.OnCanClearExportFile));
      CommandBindings.Add(new CommandBinding(CommandIndexObjects, OnIndexObjects, OnCanIndexObjects));
      DataManager.Instance.DataLoadCompleted += (s, e) => _dataLoadComplete = true;
    }

    //---------------------------------------------------------------------
    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      panoramaView.EndAnimation();
      DataManager.Instance.ExportFile.Kill();
    }

    //---------------------------------------------------------------------
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      if (!Properties.Settings.Default.NoFiddleScreenPos)
      {
        WpfScreen wpfScreen = WpfScreen.GetScreenFrom(this);
        System.Windows.Rect bounds = wpfScreen.DeviceBounds;
        uint dpi = wpfScreen.Dpi;
        double left = Utils.Normalize(Left, 0, bounds.Width);
        double top = Utils.Normalize(Top, 0, bounds.Height);
        double right = left + Width;
        double bottom = top + Height;

        //Log.Debug($"device bounds [{bounds.Left}, {bounds.Top}, {bounds.Width}, {bounds.Height}]");
        //Log.Debug($"window bounds [{Left}, {Top}, {ActualWidth}, {ActualHeight}] dpi {dpi}");

        if (dpi != 96 || bounds.Width < 1920 || bounds.Height < 1080)
        {
          double scale = Math.Min(bounds.Width / 1920, bounds.Height / 1080) * 96.0 / dpi;
          if (scale != 1)
          {
            WindowState = WindowState.Maximized;
          }
        }
        else
        {
          // get monitor
          int mx = (int)((Left + bounds.Width - 1) / bounds.Width) - 1;
          int my = (int)((Top + bounds.Height - 1) / bounds.Height) - 1;

          bool moveIt = false;
          bool maximizeIt = false;
          left = 0;
          top = 0;
          if (right >= bounds.Width)
          {
            left = bounds.Width - 1920;
            moveIt = true;
            maximizeIt = left < 0;
            left += mx * bounds.Width;
          }
          if (bottom >= bounds.Height)
          {
            top = bounds.Height - 1080 + my * bounds.Height;
            moveIt = true;
            maximizeIt = maximizeIt || top < 0;
            top += my * bounds.Height;
          }
          if (moveIt)
          {
            if (maximizeIt)
            {
              WindowState = WindowState.Maximized;
            }
            else
            {
              if (left != Left)
              {
                Left = left;
              }
              if (top != Top)
              {
                Top = top;
              }
            }
          }
        }
      }

      string ObjektAlbumBaseFolder = System.IO.Path.Combine(Zusi.DataPath[2], @"_Tools\ZusiObjektAlbum\");
      if (!Directory.Exists(ObjektAlbumBaseFolder))
      {
        Directory.CreateDirectory(ObjektAlbumBaseFolder);
      }

      DataManager.Instance.modelPath = System.IO.Path.Combine(ObjektAlbumBaseFolder, "vision_model.onnx");
      DataManager.Instance.u2netModelPath = System.IO.Path.Combine(ObjektAlbumBaseFolder, "u2net.onnx");
      DataManager.Instance.indexPath = System.IO.Path.Combine(ObjektAlbumBaseFolder, "index.bin");

      if (!_dataLoadComplete)
      {
        JustAMomentWindow jamw = new()
        {
          Owner = this
        };
        jamw.ShowDialog();
      }
    }

    //---------------------------------------------------------------------
    private void ObjectView_EditCompleted(object sender, EditCompletedEventArgs e)
    {
      string newValue = e.NewValue?.Trim();
      if (string.IsNullOrEmpty(newValue) || newValue.IndexOfAny(ValidateSectionName.ForbiddenChars) > -1 || newValue.IndexOf('/') > -1)
      {
        e.DiscardChanges = true;
      }
      else if (string.Compare(e.OldValue, e.NewValue, true) != 0)
      {
        KeepInMind.AddMapping(e.OldValue, e.NewValue);
        KeepInMind.Save();
      }
      e.Handled = true;
    }

    //---------------------------------------------------------------------
    private void ObjectView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
      DependencyObject obj = e.OriginalSource as DependencyObject;
      TreeViewItem tvi = obj.GetVisualAncestor<TreeViewItem>();
      if (tvi.Header is ObjectModel om)
      {
        MenuItem mi;
        bool needsSeperator = false;

        ContextMenu cm = new();

        if (om.Object != null)
        {
          mi = new MenuItem
          {
            Command = ZOACommands.CopyPathToClipboardCommand,
            CommandParameter = om,
            CommandTarget = this
          };
          cm.Items.Add(mi);
          needsSeperator = true;
        }

        if (ObjectModel.FindMyObjects(om) != null)
        {
          if (om.Object == null && !ObjectModel.IsMyObjects(om))
          {
            if (needsSeperator)
            {
              cm.Items.Add(new Separator());
              needsSeperator = false;
            }

            mi = new MenuItem
            {
              Command = ZOACommands.RenameSectionCommand,
              CommandParameter = om,
              CommandTarget = this
            };
            cm.Items.Add(mi);
          }

          if (om.Object != null)
          {
            cm.Items.Insert(0, new Label() { FontWeight = FontWeights.Bold, Content = om.DisplayName });
            cm.Items.Insert(1, new Separator());

            if (needsSeperator)
            {
              cm.Items.Add(new Separator());
              //needsSeperator = false;
            }

            mi = new MenuItem
            {
              Command = ZOACommands.RemoveObjectCommand,
              CommandParameter = om,
              CommandTarget = this
            };
            cm.Items.Add(mi);
          }
        }

        if (cm.Items.Count > 0)
        {
          tvi.ContextMenu = cm;
          tvi.IsSelected = true;
        }
      }
    }

    //---------------------------------------------------------------------
    private void ObjectView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
      panoramaView.EndAnimation();
      DataManager.Instance.SelectedObjectModel = e.NewValue as ObjectModel;
    }

    //---------------------------------------------------------------------
    public void OnCanImportFolder(object sender, CanExecuteRoutedEventArgs e)
    {
      DataManager dm = DataManager.Instance;
      e.CanExecute = !Validation.GetHasError(tbxNewSection2) && !string.IsNullOrEmpty(dm.ObjectFolder) && Directory.Exists(dm.ObjectFolder);
    }

    //---------------------------------------------------------------------
    public void OnCanImportObject(object sender, CanExecuteRoutedEventArgs e)
    {
      DataManager dm = DataManager.Instance;
      e.CanExecute = !Validation.GetHasError(tbxNewSection1) && !string.IsNullOrEmpty(dm.ObjectFile) && System.IO.File.Exists(dm.ObjectFile);
    }

    //---------------------------------------------------------------------
    private void OnCanRenameSection(object sender, CanExecuteRoutedEventArgs e)
    {
      e.CanExecute = e.Parameter is ObjectModel om && !om.IsReadOnly;
    }

    //---------------------------------------------------------------------
    private void OnRenameSection(object sender, ExecutedRoutedEventArgs e)
    {
      tvObjects.BeginEdit();
    }

    //---------------------------------------------------------------------
    private void OnRemoveObject(object sender, ExecutedRoutedEventArgs e)
    {
      if (e.Parameter is ObjectModel om)
      {
        if (om.Object is Landschaft ls)
        {
          ZusiDocumentBase doc = ls.GetDocument();
          KeepInMind.RemoveFile(doc.Filename);
        }
        else if (om.Object is LandscapeObject lo)
        {
          KeepInMind.RemoveFile(lo.Filename);
        }
        KeepInMind.Save();

        CleanUpSection(om);
      }
    }

    //---------------------------------------------------------------------
    private void CleanUpSection(ObjectModel om)
    {
      if (om.Children.Count == 0)
      {
        ObjectModel papa = om.Parent;
        if (papa != null)
        {
          papa.Children.Remove(om);
          CleanUpSection(papa);
        }
        else
        {
          DataManager.Instance.Objects.Remove(om);
        }
      }
    }

    //private void OnIndexObjects(object sender, ExecutedRoutedEventArgs e)
    //{
    //  string imageFolder = "D:\\Zusi\\ObjectImages";
    //  string onnxModel = "D:\\Zusi\\Onnx\\vision_model.onnx";
    //  string indexFile = "D:\\Zusi\\index\\index.bin";

    //  ZusiObjektAlbum.Similaritysearch.BatchImageExporter.ExportAndIndexAll(imageFolder, onnxModel, indexFile);
    //}

    private CancellationTokenSource? _exportCts;

    private void OnCanIndexObjects(object sender, CanExecuteRoutedEventArgs e)
    {
      // Solange schon ein Lauf aktiv ist, den Menüpunkt sperren -
      // verhindert einen zweiten, parallel laufenden Export.
      e.CanExecute = _exportCts == null;
    }

    private async void OnIndexObjects(object sender, ExecutedRoutedEventArgs e)
    {
      string? imageFolder = null; // "D:\\Zusi\\ObjectImages";
      string onnxModel = DataManager.Instance.modelPath;
      string indexFile = DataManager.Instance.indexPath;

      var progress = new Progress<string>(msg => DataManager.Instance.SetStatusMessage(msg));
      _exportCts = new CancellationTokenSource();

      btnCancelIndexing.IsEnabled = true;
      btnCancelIndexing.Visibility = Visibility.Visible;
      CommandManager.InvalidateRequerySuggested(); // Menüpunkt neu bewerten (jetzt gesperrt)

      bool wasCancelled = false;

      try
      {
        await ZusiObjektAlbum.Similaritysearch.BatchImageExporter.ExportAndIndexAllAsync(
            imageFolder, onnxModel, indexFile, progress, _exportCts.Token);

        wasCancelled = _exportCts.IsCancellationRequested;

        MessageBox.Show(this, "Export & Indexierung abgeschlossen.", "Fertig",
            MessageBoxButton.OK, MessageBoxImage.Information);
      }
      catch (Exception ex)
      {
        MessageBox.Show(this, ex.Message, "Fehler bei Export/Indexierung",
            MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }
      finally
      {
        _exportCts?.Dispose();
        _exportCts = null;
        btnCancelIndexing.Visibility = Visibility.Collapsed;
        CommandManager.InvalidateRequerySuggested(); // Menüpunkt wieder freigeben
      }

      MessageBox.Show(this,
          wasCancelled ? "Export wurde abgebrochen (Fortschritt wurde gespeichert)." : "Export & Indexierung abgeschlossen.",
          "Fertig", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCancelIndexing_Click(object sender, RoutedEventArgs e)
    {
      _exportCts?.Cancel();
      btnCancelIndexing.IsEnabled = false; // verhindert Mehrfachklick, Abbruch braucht evtl. noch einen Moment
    }

    //---------------------------------------------------------------------
    private void OnCanCopyPathToClipboard(object sender, CanExecuteRoutedEventArgs e)
    {
      DataManager dm = DataManager.Instance;
      if (dm.ExportFile.IsActive)
      {
        Zusi3DModel z = dm.SelectedObject;
        e.CanExecute = z != null && z.ObjectModel.Object != null;
      }
      else
      {
        e.CanExecute = true;
      }
    }

    //---------------------------------------------------------------------
    private void OnCopyPathToClipboard(object sender, ExecutedRoutedEventArgs e)
    {
      try
      {
        ILandscapeObject ll = null;
        if (e.Parameter == null)
        {
          if (tvObjects.SelectedItem is ObjectModel om && om.Object is ILandscapeObject lo)
          {
            ll = lo;
          }
        }
        else if (e.Parameter is ObjectModel om && om.Object is ILandscapeObject lo)
        {
          ll = lo;
        }
        string fullpath = null;
        if (ll != null)
        {

          if (ll is LandscapeObject lo)
          {
            fullpath = lo.Filename;
          }
          else if (ll is Landschaft l)
          {
            fullpath = l.GetDocument().Filename;
          }
          DataPathType dtp = DataPathType.Unknown;
          string filename = System.IO.Path.GetDirectoryName(fullpath); // Zusi.GetRelativePathOf(fullpath, ref dtp);
          if (DataManager.Instance.ExportFile.IsActive)
          {
            DataManager.Instance.ExportFile.WriteLine(filename);
          }
          else
          {
            System.Windows.Forms.Clipboard.SetText(filename);
          }
        }
        else
        {
          // similarity search resultitem

          if (e.OriginalSource is ListBoxItem lm && lm.DataContext is ResultItem resultItem)
          {
            fullpath = resultItem.SourcePath;

            DataPathType dtp = DataPathType.Unknown;
            string filename = fullpath; // Zusi.GetRelativePathOf(fullpath, ref dtp);
            System.Windows.Forms.Clipboard.SetText(filename);
          }

        }
      }
      catch (Exception ex)
      {
        Log.Error("Fehler beim Kopieren des Pfads in die Zwischenablage", ex);
        MessageBox.Show(this, ex.Message, "Fehler beim Kopieren des Pfads", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    //---------------------------------------------------------------------
    private void OnCanCopyFilenamePathToClipboard(object sender, CanExecuteRoutedEventArgs e)
    {
      DataManager dm = DataManager.Instance;
      if (dm.ExportFile.IsActive)
      {
        Zusi3DModel z = dm.SelectedObject;
        e.CanExecute = z != null && z.ObjectModel.Object != null;
      }
      else
      {
        e.CanExecute = true;
      }
    }

    //---------------------------------------------------------------------
    private void OnCopyFilenamePathToClipboard(object sender, ExecutedRoutedEventArgs e)
    {
      try
      {
        ILandscapeObject ll = null;
        if (e.Parameter == null)
        {
          if (tvObjects.SelectedItem is ObjectModel om && om.Object is ILandscapeObject lo)
          {
            ll = lo;
          }
        }
        else if (e.Parameter is ObjectModel om && om.Object is ILandscapeObject lo)
        {
          ll = lo;
        }
        string fullpath = null;
        if (ll != null)
        {

          if (ll is LandscapeObject lo)
          {
            fullpath = lo.Filename;
          }
          else if (ll is Landschaft l)
          {
            fullpath = l.GetDocument().Filename;
          }
          DataPathType dtp = DataPathType.Unknown;
          string filename = fullpath; // Zusi.GetRelativePathOf(fullpath, ref dtp);
          if (DataManager.Instance.ExportFile.IsActive)
          {
            DataManager.Instance.ExportFile.WriteLine(filename);
          }
          else
          {
            System.Windows.Forms.Clipboard.SetText(filename);
          }
        }
        else
        {
          // similarity search resultitem

          if (e.OriginalSource is ListBoxItem lm && lm.DataContext is ResultItem resultItem)
          {
            fullpath = resultItem.SourcePath;

            DataPathType dtp = DataPathType.Unknown;
            string filename = fullpath; // Zusi.GetRelativePathOf(fullpath, ref dtp);
            System.Windows.Forms.Clipboard.SetText(filename);
          }

        }
      }
      catch (Exception ex)
      {
        Log.Error("Fehler beim Kopieren des Pfads in die Zwischenablage", ex);
        MessageBox.Show(this, ex.Message, "Fehler beim Kopieren des Pfads", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
    //---------------------------------------------------------------------
    private void OnManageFolders(object sender, ExecutedRoutedEventArgs e)
    {
      FolderManagerWindow dlg = new()
      {
        Owner = this
      };
      if (dlg.ShowDialog() == true)
      {
        KeepInMind.SyncFolders(dlg.Folders);
        KeepInMind.Save();

        if (dlg.UpdateFoldersImmediately)
        {
          DataManager.Instance.UpdateMyObjects();
        }
      }
    }

    // URLs auf euer Repo anpassen. Für Dateien >100 MB (z.B. das ONNX-Modell)
    // unbedingt eine GitHub-Release-Asset-URL verwenden, nicht raw.githubusercontent.com.
    private const string OnnxModelUrl = "https://github.com/haroldlinke/Zusi-Tools-ZusiObjektAlbum/releases/download/V8.0.1/vision_model.onnx";
    private const string OnnxRemBgModelUrl = "https://github.com/haroldlinke/Zusi-Tools-ZusiObjektAlbum/releases/download/V8.0.1/u2net.onnx";
    private const string IndexUrl = "https://github.com/haroldlinke/Zusi-Tools-ZusiObjektAlbum/releases/download/V8.0.1/index.bin";

    private async void OnDownloadIndex(object sender, RoutedEventArgs e)
    {
      string indexFile = DataManager.Instance.indexPath;

      var progressWindow = new DownloadProgressWindow { Owner = this };
      progressWindow.Show();

      try
      {
        //var onnxProgress = new Progress<int>(p => progressWindow.ReportProgress($"Lade ONNX-Modell... {p}%", p));
        //await ModelDownloader.ModelDownloader.DownloadFileAsync(OnnxModelUrl, onnxModel, onnxProgress, progressWindow.CancellationToken);

        var indexProgress = new Progress<int>(p => progressWindow.ReportProgress($"Lade index.bin... {p}%", p));
        await ModelDownloader.ModelDownloader.DownloadFileAsync(IndexUrl, indexFile, indexProgress, progressWindow.CancellationToken);

        progressWindow.Close();
        MessageBox.Show(this, "Index wurde erfolgreich heruntergeladen.", "Fertig",
            MessageBoxButton.OK, MessageBoxImage.Information);
      }
      catch (OperationCanceledException)
      {
        progressWindow.Close();
      }
      catch (Exception ex)
      {
        progressWindow.Close();
        MessageBox.Show(this, ex.Message, "Fehler beim Download", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void OnDownloadModel(object sender, RoutedEventArgs e)
    {
      string onnxModel = DataManager.Instance.modelPath;
      
      var progressWindow = new DownloadProgressWindow { Owner = this };
      progressWindow.Show();

      try
      {
        var onnxProgress = new Progress<int>(p => progressWindow.ReportProgress($"Lade ONNX-Modell... {p}%", p));
        await ModelDownloader.ModelDownloader.DownloadFileAsync(OnnxModelUrl, onnxModel, onnxProgress, progressWindow.CancellationToken);

        //var indexProgress = new Progress<int>(p => progressWindow.ReportProgress($"Lade index.bin... {p}%", p));
        //await ModelDownloader.ModelDownloader.DownloadFileAsync(IndexUrl, indexFile, indexProgress, progressWindow.CancellationToken);

        progressWindow.Close();
        MessageBox.Show(this, "Modell wurde erfolgreich heruntergeladen.", "Fertig",
            MessageBoxButton.OK, MessageBoxImage.Information);
      }
      catch (OperationCanceledException)
      {
        progressWindow.Close();
      }
      catch (Exception ex)
      {
        progressWindow.Close();
        MessageBox.Show(this, ex.Message, "Fehler beim Download", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void OnDownloadRemBgModel(object sender, RoutedEventArgs e)
    {
      string rembgonnxModel = DataManager.Instance.u2netModelPath;

      var progressWindow = new DownloadProgressWindow { Owner = this };
      progressWindow.Show();

      try
      {
        var onnxProgress = new Progress<int>(p => progressWindow.ReportProgress($"Lade RemBG-ONNX-Modell... {p}%", p));
        await ModelDownloader.ModelDownloader.DownloadFileAsync(OnnxRemBgModelUrl, rembgonnxModel, onnxProgress, progressWindow.CancellationToken);

        //var indexProgress = new Progress<int>(p => progressWindow.ReportProgress($"Lade index.bin... {p}%", p));
        //await ModelDownloader.ModelDownloader.DownloadFileAsync(IndexUrl, indexFile, indexProgress, progressWindow.CancellationToken);

        progressWindow.Close();
        MessageBox.Show(this, "RemBg-Modell wurde erfolgreich heruntergeladen.", "Fertig",
            MessageBoxButton.OK, MessageBoxImage.Information);
      }
      catch (OperationCanceledException)
      {
        progressWindow.Close();
      }
      catch (Exception ex)
      {
        progressWindow.Close();
        MessageBox.Show(this, ex.Message, "Fehler beim Download", MessageBoxButton.OK, MessageBoxImage.Error);
      }
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

      if (sender is not Grid element || element.DataContext is not Zusi3DModel item)
      {
        return;
      }

      try
      {
        ILandscapeObject ll = null;
        string fullpath = null;
        if (item.ObjectModel is ObjectModel om && om.Object is ILandscapeObject lo)
        {
          ll = lo;
        }
        
        if (ll != null)
        {

          if (ll is LandscapeObject lo1)
          {
            fullpath = lo1.Filename;
          }
          else if (ll is Landschaft l)
          {
            fullpath = l.GetDocument().Filename;
          }

        }

      if (string.IsNullOrEmpty(fullpath) || !System.IO.File.Exists(fullpath))
      {
        return;
      }

      // Exakt das Format, das Explorer beim Datei-Ziehen erzeugt (CF_HDROP) -
      // jede Anwendung, die Drag&Drop vom Explorer akzeptiert, akzeptiert das auch.
      var dataObject = new DataObject(DataFormats.FileDrop, new[] { fullpath });
      DragDrop.DoDragDrop(element, dataObject, DragDropEffects.Copy);

      }
      catch (Exception ex)
      {
        Log.Error("Fehler beim Kopieren des Pfads in die Zwischenablage", ex);
        MessageBox.Show(this, ex.Message, "Fehler beim Kopieren des Pfads", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    //private void Keywords_PreviewKeyUp(object sender, KeyEventArgs e)
    //{
    //  if (e.Key is Key.Up or Key.Down or Key.Left or Key.Right
    //            or Key.Enter or Key.Escape or Key.Tab)
    //    return;

    //  cbKeywords.IsDropDownOpen = true;
    //}

    //private void ClearKeyword_Click(object sender, RoutedEventArgs e)
    //{
    //  cbKeywords.Text = string.Empty;
    //  cbKeywords.Focus();
    //}

    private DataManager Vm => (DataManager)DataContext;   // Typ anpassen

    private bool _suppressInput;

    private void KeywordInput_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (_suppressInput) return;

      Vm.KeywordInput = tbKeywordInput.Text;                 // filtert die Vorschlagsliste
      lbKeywords.SelectedIndex = Vm.KeywordView.IsEmpty ? -1 : 0;
      popKeywords.IsOpen = tbKeywordInput.IsKeyboardFocused && !Vm.KeywordView.IsEmpty;
    }

    private void KeywordInput_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
      if (Vm.KeywordView.IsEmpty) return;
      if (lbKeywords.SelectedIndex < 0) lbKeywords.SelectedIndex = 0;
      popKeywords.IsOpen = true;
    }

    private void KeywordInput_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        => popKeywords.IsOpen = false;


    private void KeywordInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      switch (e.Key)
      {
        case Key.Down:
          if (!popKeywords.IsOpen)
          {
            if (!Vm.KeywordView.IsEmpty)
            {
              popKeywords.IsOpen = true;
              if (lbKeywords.SelectedIndex < 0) lbKeywords.SelectedIndex = 0;
            }
          }
          else MoveSelection(+1);
          e.Handled = true;
          break;

        case Key.Up:
          if (popKeywords.IsOpen) { MoveSelection(-1); e.Handled = true; }
          break;

        case Key.Enter:
          {
            var kw = (popKeywords.IsOpen ? lbKeywords.SelectedItem : null) as string
                     ?? (string.IsNullOrWhiteSpace(tbKeywordInput.Text)
                         ? null
                         : Vm.KeywordView.Cast<string>().FirstOrDefault());
            if (kw != null) CommitKeyword(kw);
            e.Handled = true;
            break;
          }

        case Key.Escape:
          if (popKeywords.IsOpen) { popKeywords.IsOpen = false; e.Handled = true; }
          break;

        case Key.Back when tbKeywordInput.Text.Length == 0:
          Vm.RemoveLastKeyword();                        // Backspace im leeren Feld löscht den letzten Chip
          break;
      }
    }

    private void KeywordItem_Click(object sender, MouseButtonEventArgs e)
    {
      if (sender is ListBoxItem { DataContext: string kw })
      {
        CommitKeyword(kw);
        e.Handled = true;
      }
    }

    private void MoveSelection(int delta)
    {
      int n = lbKeywords.Items.Count;
      if (n == 0) return;
      lbKeywords.SelectedIndex = Math.Clamp(lbKeywords.SelectedIndex + delta, 0, n - 1);
      lbKeywords.ScrollIntoView(lbKeywords.SelectedItem);
    }

    private void CommitKeyword(string kw)
    {
      Vm.AddKeyword(kw);                                     // löst Baumfilter + Listen-Refresh aus

      _suppressInput = true;
      try { tbKeywordInput.Clear(); }
      finally { _suppressInput = false; }
      Vm.KeywordInput = "";

      popKeywords.IsOpen = false;
      tbKeywordInput.Focus();
    }

    // Klick außerhalb von Eingabefeld und Liste schließt das Popup
    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
      base.OnPreviewMouseDown(e);
      if (popKeywords.IsOpen && !tbKeywordInput.IsMouseOver && !lbKeywords.IsMouseOver)
        popKeywords.IsOpen = false;
    }


    private void RemoveChip_Click(object sender, RoutedEventArgs e)
    {
      if (sender is FrameworkElement { DataContext: string kw })
        Vm.RemoveKeyword(kw);
    }
  }

}
