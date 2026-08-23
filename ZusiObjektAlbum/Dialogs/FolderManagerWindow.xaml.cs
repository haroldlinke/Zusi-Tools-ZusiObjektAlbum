using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;
using ZusiKlassenLib;
using ZusiObjektAlbum.Miscellaneous;
using ZusiObjektAlbum.MVVM;

namespace ZusiObjektAlbum.Dialogs
{
    /// <summary>
    /// Interaktionslogik für FolderManagerWindow.xaml
    /// </summary>
    public partial class FolderManagerWindow : Window
    {
        private class KimiComparer : IEqualityComparer<KeepInMindItem>
        {
            public bool Equals(KeepInMindItem x, KeepInMindItem y)
            {
                return GetHashCode(x) == GetHashCode(y);
            }

            public int GetHashCode(KeepInMindItem obj)
            {
                return obj == null || obj.Item == null ? 0 : obj.Item.ToLower().GetHashCode();
            }
        }

        private static readonly KimiComparer _kimiComparer = new();

        private readonly ObservableCollection<KeepInMindItem> _folders = new();
        private bool _dirty;

        public static readonly DependencyProperty UpdateFoldersImmediatelyProperty = DependencyProperty.Register(
            "UpdateFoldersImmediately",
            typeof(bool),
            typeof(FolderManagerWindow),
            new PropertyMetadata(true));
        public bool UpdateFoldersImmediately
        {
            get => (bool)GetValue(UpdateFoldersImmediatelyProperty);
            set => SetValue(UpdateFoldersImmediatelyProperty, value);
        }

        public static readonly RoutedUICommand AddFolderCommand = new("Ordner hinzufügen", "AddFolderCommand", typeof(FolderManagerWindow));
        public static readonly RoutedUICommand RemoveFolderCommand = new("Ordner entfernen", "RemoveFolderCommand", typeof(FolderManagerWindow));
        public static readonly RoutedUICommand SaveAndCloseWindowCommand = new("_Speichern", "SaveAndCloseWindowCommand", typeof(FolderManagerWindow),
            new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.F4, ModifierKeys.Control) }));

        public ObservableCollection<KeepInMindItem> Folders { get => _folders; }

        public FolderManagerWindow()
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(AddFolderCommand, OnAddFolder, OnCanAddFolder));
            CommandBindings.Add(new CommandBinding(RemoveFolderCommand, OnRemoveFolder, OnCanRemoveFolder));
            CommandBindings.Add(new CommandBinding(SaveAndCloseWindowCommand, OnSaveAndCloseWindow, OnCanSaveAndCloseWindow));

            KeepInMind.Folders.ForEach(kimi =>
            {
                kimi.PropertyChanged += Kimi_PropertyChanged;
                _folders.Add(kimi);
            });
        }

        private void Kimi_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _dirty = true;
        }

        private void OnCanAddFolder(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OnAddFolder(object sender, ExecutedRoutedEventArgs e)
        {
            using CommonOpenFileDialog dlg = new();
            dlg.IsFolderPicker = true;
            if (string.IsNullOrEmpty(dlg.InitialDirectory))
            {
                string p = Zusi.DataPath[DataPathType.Official];
#if false
                    if (!Directory.Exists(p))
                    {
                        p = Zusi.ZusiAlternateDataPath;
                    }
                    if (Directory.Exists(p))
                    {
                        dlg.InitialDirectory = System.IO.Path.GetDirectoryName(p);
                    }
#endif
            }
            dlg.Title = "Ordner importieren";

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                AddFolder(dlg.FileName);
            }
        }

        private void OnCanRemoveFolder(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = dgrdFolders.SelectedItem != null;
        }

        private void OnRemoveFolder(object sender, ExecutedRoutedEventArgs e)
        {
            if (dgrdFolders.SelectedItem is KeepInMindItem kimi)
            {
                kimi.PropertyChanged -= Kimi_PropertyChanged;
                _folders.Remove(kimi);
                _dirty = true;
            }
        }

        private void OnCanSaveAndCloseWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _dirty;
        }

        private void OnSaveAndCloseWindow(object sender, ExecutedRoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void DgrdFolders_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (string f in (string[])e.Data.GetData(DataFormats.FileDrop, false))
                {
                    if (Directory.Exists(f))
                    {
                        e.Effects = DragDropEffects.Copy;
                        return;
                    }
                }
            }

            e.Effects = DragDropEffects.None;
        }

        private void DgrdFolders_Drop(object sender, DragEventArgs e)
        {
            string folder = null;

            foreach (string f in (string[])e.Data.GetData(DataFormats.FileDrop, false))
            {
                if (Directory.Exists(f))
                {
                    if (folder == null || f.Length < folder.Length)
                    {
                        folder = f;
                    }
                }
            }

            if (!string.IsNullOrEmpty(folder))
            {
                AddFolder(folder);
            }
        }

        private void AddFolder(string folder)
        {
            KeepInMindItem kimi = new() { Item = folder };
            if (!_folders.Contains(kimi, _kimiComparer))
            {
                _folders.Add(kimi);
                _dirty = true;
            }
        }
    }
}
