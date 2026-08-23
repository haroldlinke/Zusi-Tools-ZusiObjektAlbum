using log4net;
using SovomaLib;
using SovomaLib.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using ZusiObjektAlbum.Dialogs;
using ZusiObjektAlbum.Miscellaneous;
using ZusiObjektAlbum.MVVM;
using ZusiObjektAlbum.ValidationRules;

namespace ZusiObjektAlbum
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MainWindow));

        private bool _dataLoadComplete;

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
            CommandBindings.Add(new CommandBinding(ZOACommands.ClearExportFileCommand, DataManager.Instance.OnClearExportFile, DataManager.Instance.OnCanClearExportFile));

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

#if false
        private Point _startPoint;

        //---------------------------------------------------------------------
        private void ObjectView_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("objectmodel"))
            {

            }
        }

        //---------------------------------------------------------------------
        private void ObjectView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("objectmodel"))
            {

            }
        }

        //---------------------------------------------------------------------
        private void ObjectView_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(tvObjects);
            Vector diff = _startPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                //GetNearestContainer
            }
        }

        //---------------------------------------------------------------------
        private void ObjectView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(tvObjects);
        }
#endif

        #region command handler

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
            e.CanExecute = !Validation.GetHasError(tbxNewSection1) && !string.IsNullOrEmpty(dm.ObjectFile) && File.Exists(dm.ObjectFile);
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

            if (ll != null)
            {
                string fullpath = null;
                if (ll is LandscapeObject lo)
                {
                    fullpath = lo.Filename;
                }
                else if (ll is Landschaft l)
                {
                    fullpath = l.GetDocument().Filename;
                }
                DataPathType dtp = DataPathType.Unknown;
                string filename = Zusi.GetRelativePathOf(fullpath, ref dtp);
                if (DataManager.Instance.ExportFile.IsActive)
                {
                    DataManager.Instance.ExportFile.WriteLine(filename);
                }
                else
                {
                    Clipboard.SetText(filename);
                }
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

        #endregion
    }
}
