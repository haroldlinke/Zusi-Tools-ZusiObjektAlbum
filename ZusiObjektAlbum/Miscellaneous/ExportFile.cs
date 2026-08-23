using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using ZusiObjektAlbum.Dialogs;

namespace ZusiObjektAlbum.Miscellaneous
{
#if true
    public class ExportFile : DependencyObject
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ObservableCollection<string> _content = new ObservableCollection<string>();
        private ExportFileWindow _window;

        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
            "IsActive",
            typeof(bool),
            typeof(ExportFile),
            new PropertyMetadata(false, OnIsActiveChanged, OnCoerceIsActive));
        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public static readonly DependencyProperty IsTopmostProperty = DependencyProperty.Register(
            "IsTopmost",
            typeof(bool),
            typeof(ExportFile));
        public bool IsTopmost
        {
            get => (bool)GetValue(IsTopmostProperty);
            set => SetValue(IsTopmostProperty, value);
        }

        public static readonly DependencyProperty FilenameProperty = DependencyProperty.Register(
            "Filename",
            typeof(string),
            typeof(ExportFile),
            new PropertyMetadata(null, OnFilenameChanged));
        public string Filename
        {
            get => (string)GetValue(FilenameProperty);
            set => SetValue(FilenameProperty, value);
        }

        public ObservableCollection<string> Content { get => _content; }

        public ExportFile()
        {
            Filename = KeepInMind.ExportFile;
        }

        public void Clear()
        {
            _content.Clear();
        }

        public void Kill()
        {
            IsActive = false;
        }

        public void RemoveLine(int index)
        {
            if (index >= 0 && index < _content.Count)
            {
                _content.RemoveAt(index);
            }
        }

        public void WriteLine(string line)
        {
            _content.Add(line);
        }

        private static object OnCoerceIsActive(DependencyObject d, object baseValue)
        {
            return (d as ExportFile).OnCoerceIsActive((bool)baseValue);
        }

        private bool OnCoerceIsActive(bool value)
        {
            return value && !string.IsNullOrEmpty(Filename);
        }

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ExportFile)?.OnIsActiveChanged((bool)e.NewValue);
        }

        private void OnIsActiveChanged(bool value)
        {
            if (value)
            {
                Window mainWindow = Application.Current.MainWindow;
                _window = new ExportFileWindow
                {
                    Owner = mainWindow
                };
                _window.Closed += ExportWindow_Closed;

                _window.Left = mainWindow.Left + mainWindow.ActualWidth - _window.Width;
                _window.Top = mainWindow.Top;

                _window.Show();
            }
            else
            {
                _window?.Close();

                using (FileStream fs = new FileStream(Filename, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        foreach (string line in _content)
                        {
                            sw.WriteLine(line);
                        }
                        sw.Flush();
                    }
                }
            }
        }

        private void ExportWindow_Closed(object sender, EventArgs e)
        {
            _window = null;
            IsActive = false;
        }

        private static void OnFilenameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ExportFile)?.OnFilenameChanged((string)e.NewValue);
        }

        private void OnFilenameChanged(string value)
        {
            KeepInMind.ExportFile = value;
            KeepInMind.Save();

            if (File.Exists(Filename))
            {
                using (FileStream fs = new FileStream(value, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            _content.Add(line);
                        }
                    }
                }
            }
            else
            {
                _content.Clear();
                try
                {
                    string folder = Path.GetDirectoryName(Filename);
                    Directory.CreateDirectory(folder);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    MessageBox.Show(ex.Message, "Export/Import-Datei - Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
#endif
}
