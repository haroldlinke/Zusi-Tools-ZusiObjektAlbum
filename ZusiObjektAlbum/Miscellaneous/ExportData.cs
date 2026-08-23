using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ZusiObjektAlbum.Miscellaneous
{
#if false
using Automatization;

    public class ExportFile : DependencyObject
    {
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

        public void Clear()
        {
            try
            {
                Zusi3Automatisation3DEditor.ImportDaDReset();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Automatisierungsfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Kill()
        {
            IsActive = false;
        }

        public void RemoveLine(int index)
        {
            try
            {
                Zusi3Automatisation3DEditor.ImportDaDSelectItem(index);
                Zusi3Automatisation3DEditor.ImportDaDRemoveFile();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Automatisierungsfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void WriteLine(string line)
        {
            try
            {
                Zusi3Automatisation3DEditor.ImportDaDAddFile(line);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Automatisierungsfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void OnFilenameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ExportFile)?.OnFilenameChanged((string)e.NewValue);
        }

        private void OnFilenameChanged(string value)
        {
            KeepInMind.ExportFile = value;
            KeepInMind.Save();
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
                try
                {
                    Zusi3Automatisation3DEditor.ImportDaDOpenList(Filename);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Automatisierungsfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
#endif
}
