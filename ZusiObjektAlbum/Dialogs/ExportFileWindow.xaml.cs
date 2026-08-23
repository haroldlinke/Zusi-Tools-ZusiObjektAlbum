using System;
using System.Collections.Generic;
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
using ZusiObjektAlbum.Miscellaneous;
using ZusiObjektAlbum.MVVM;

namespace ZusiObjektAlbum.Dialogs
{
    /// <summary>
    /// Interaktionslogik für ExportFileWindow.xaml
    /// </summary>
    public partial class ExportFileWindow : Window
    {
        public ExportFileWindow()
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(ZOACommands.RemoveItemCommand, OnRemoveItem, OnCanRemoveItem));
        }

        private void OnCanRemoveItem(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = lvFiles.SelectedItem != null;
        }

        private void OnRemoveItem(object sender, ExecutedRoutedEventArgs e)
        {
            DataManager.Instance.ExportFile.RemoveLine(lvFiles.SelectedIndex);
        }
    }
}
