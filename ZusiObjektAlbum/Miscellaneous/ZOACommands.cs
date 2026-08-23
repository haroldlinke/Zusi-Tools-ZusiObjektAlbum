using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ZusiObjektAlbum.Miscellaneous
{
    public static class ZOACommands
    {
        public static readonly RoutedUICommand ImportObjectCommand = new RoutedUICommand("_Importieren", "ImportObjectCommand", typeof(ZOACommands),
            new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.F5) }));

        public static readonly RoutedUICommand ImportFolderCommand = new RoutedUICommand("Im_portieren", "ImportFolderCommand", typeof(ZOACommands),
            new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.F6) }));

#if false
        public static readonly RoutedUICommand ExportObjectCommand = new RoutedUICommand("_Exportieren", "ExportObjectCommand", typeof(ZOACommands),
            new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.F9) }));
#endif

        public static readonly RoutedUICommand RemoveSectionCommand = new RoutedUICommand("Ordner leeren und ent_fernen", "RemoveSectionCommand", typeof(ZOACommands));

        public static readonly RoutedUICommand ManageFoldersCommand = new RoutedUICommand("Ordner _verwalten", "ManageFoldersCommand", typeof(ZOACommands),
            new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.F7) }));

        public static readonly RoutedUICommand RemoveObjectCommand = new RoutedUICommand("_Objekt aus der Übersicht entfernen", "RemoveObjectCommand", typeof(ZOACommands));

        public static readonly RoutedUICommand RenameSectionCommand = new RoutedUICommand("_Umbenennen", "RenameSectionCommand", typeof(ZOACommands),
            new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.F2) }));

        public static readonly RoutedUICommand CopyPathToClipboardCommand = new RoutedUICommand("_Dateipfad kopieren", "CopyPathToClipboardCommand", typeof(ZOACommands),
            new InputGestureCollection(new InputGesture[] { new KeyGesture(Key.C, ModifierKeys.Control) }));

        public static readonly RoutedUICommand RemoveItemCommand = new RoutedUICommand("Dateipfad _entfernen", "RemoveItemCommand", typeof(ZOACommands));

        public static readonly RoutedUICommand ClearExportFileCommand = new RoutedUICommand("Exportdatei _leeren", "ClearExportFileCommand", typeof(ZOACommands));
    }
}
