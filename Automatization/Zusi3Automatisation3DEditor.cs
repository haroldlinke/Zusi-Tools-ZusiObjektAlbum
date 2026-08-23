using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace Automatization
{
    ///<summary>Stellt Methoden für das fernsteuern des 3D-Editors bereit.</summary>
    public class Zusi3Automatisation3DEditor : Zusi3AutomatisationCore
    {
        ///<summary>Öffnet den Dialog Drag&amp;Drop.</summary>
        protected static void OpenObjektimport(AutomationElement everything, System.Diagnostics.Process[] prc)
        {
            AutomationElement[] editorItself = GetChildrenForAEByCondition(new AutomationElement[] { everything }, GetConditionWindow(prc, "TFormZusi3DEditor"));
            AutomationElement[] editorSymbolleisten1 = GetChildrenForAEByCondition(editorItself, GetConditionDockParents());
            AutomationElement[] editorSymbolleisten2LsCreate =
                                   GetChildrenForAEByCondition(editorSymbolleisten1, new AndCondition(new PropertyCondition(AutomationElement.ClassNameProperty, "TTBXToolbar"),
                                                                                                      new PropertyCondition(AutomationElement.NameProperty, "Landschaft erstellen")));
            AutomationElement[] objDaD = GetChildrenForAEByCondition(editorSymbolleisten2LsCreate, new PropertyCondition(AutomationElement.NameProperty, "Objektimport Drag&Drop..."));
            //Kann eine ArgumentOutOfRangeException auslösen, ist so beabsichtigt.
            InvokePattern objDaDInv = (InvokePattern)objDaD[0].GetCurrentPattern(InvokePattern.Pattern);
            objDaDInv.Invoke();
        }
        ///<summary>Führt die angegebene Aktion im Dialog Drag&amp;Drop aus.</summary>
        protected static string[] PerformImportDaDAction(int actionIndex, int selectIndex, string fileName)
        {
            System.Diagnostics.Process[] prc = System.Diagnostics.Process.GetProcessesByName("Zusi3DEditor");
            AutomationElement everything = AutomationElement.RootElement;
            AutomationElement[] dateiImportWindows = GetChildrenForAEByCondition(new AutomationElement[] { everything }, GetConditionWindow(prc, "TFormImportDateien"));
            if (dateiImportWindows.Length == 0)
            {
                OpenObjektimport(everything, prc);
                dateiImportWindows = GetChildrenForAEByConditionBlocking(new AutomationElement[] { everything }, GetConditionWindow(prc, "TFormImportDateien"), 1);
            }
            if (actionIndex < 0)
            {
                AutomationElement[] listBox = GetChildrenForAEByCondition(dateiImportWindows, new PropertyCondition(AutomationElement.ClassNameProperty, "TListBox"));
                AutomationElement[] listEntrys = GetAllChildrenForAE(listBox);
                if (actionIndex == -1)
                    return GetAllNamesForArr(listEntrys);
                else if (actionIndex == -2)
                {
                    SelectionItemPattern listEntrysPattern = (SelectionItemPattern)listEntrys[selectIndex].GetCurrentPattern(SelectionItemPattern.Pattern);
                    listEntrysPattern.Select();
                    return null;
                }
                throw new System.ArgumentOutOfRangeException();
            }
            else
            {
                AutomationElement[] symbolleiste1 = GetChildrenForAEByCondition(dateiImportWindows, GetConditionDockParents());
                AutomationElement[] symbolleiste2 = GetChildrenForAEByCondition(symbolleiste1, new PropertyCondition(AutomationElement.ClassNameProperty, "TTBXToolbar"));

                //Wahlmöglichkeit: Über Index oder über Bezeichner abrufen: Nachteil Bezeichner: Nicht international und langsamer. Nachteil Index: Gefahr, dass das nichtdeterministisch wird.
                AutomationElement[] allSymbolActions = GetAllChildrenForAE(symbolleiste2);
                InvokePattern symbolAction = (InvokePattern)allSymbolActions[actionIndex].GetCurrentPattern(InvokePattern.Pattern);
                symbolAction.Invoke();

                if ((actionIndex == 0) || (actionIndex == 9) || (actionIndex == 10))
                {
                    AutomationElement[] editorItself = GetChildrenForAEByConditionBlocking(new AutomationElement[] { everything }, GetConditionWindow(prc, "TFormZusi3DEditor"), 1);
                    AutomationElement[] openFileDialog = GetChildrenForAEByConditionBlocking(editorItself, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window), 1);
                    AutomationElement[] openContent = GetAllChildrenForAE(openFileDialog);

                    //Das arbeiten mit dem OpenFileDialog ist nicht ganz einfach, insbesondere hinsichtlich des Dateinamens sowie des Speichern bzw. Öffnen-Buttons selbst. Hohe Störungsgefahr.
                    bool found = false;
                    foreach (AutomationElement content1 in openContent)
                    {
                        if (!content1.TryGetCurrentPattern(ValuePattern.Pattern, out object valp0))
                            continue;
                        ValuePattern valp = (ValuePattern)valp0;
                        valp.SetValue(fileName);
                        found = true;
                    }
                    if (!found)
                    {
                        AutomationElement[] openContent2 = GetChildrenForAEByCondition(openFileDialog, new PropertyCondition(AutomationElement.ClassNameProperty, "DUIViewWndClassName"));

                        for (int i = 0; i < 4; ++i)
                        {

                            foreach (AutomationElement content1 in openContent2)
                            {
                                if (!content1.TryGetCurrentPattern(ValuePattern.Pattern, out object valp0))
                                    continue;
                                ValuePattern valp = (ValuePattern)valp0;
                                if (valp.Current.IsReadOnly)
                                    continue;
                                valp.SetValue(fileName);
                                found = true;
                            }
                            if (found)
                                break;

                            openContent2 = GetAllChildrenForAE(openContent2);
                        }
                    }
                    if (!found)
                        throw new System.IO.IOException();

                    AutomationElement[] buttons = GetChildrenForAEByCondition(openFileDialog, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
                    InvokePattern buttonsPattern = (InvokePattern)buttons[0].GetCurrentPattern(InvokePattern.Pattern);
                    buttonsPattern.Invoke();
                }

                return null;
            }
        }

        ///<summary>Fügt eine Datei zur Liste Objektimport Drag&amp;Drop hinzu.</summary>
        ///<param name="fullFileName">Die einzufügende Datei, es wird der volle Systempfad benötigt.</param>
        ///<exception cref="System.IO.IOException">Die Zusi-Benutzeroberflächer verhielt sich nicht wie erwartet.</exception>
        ///<exception cref="System.TimeoutException">Ein Dialog wurde nicht innerhalb der erwarteten Zeitspanne geöffnet oder konnte nicht erkannt werden.</exception>
        ///<exception cref="System.IndexOutOfRangeException">Die Zusi-Benutzeroberfläche ist nicht in einem Zustand, der diese Aktion zulässt.</exception>
        public static void ImportDaDAddFile(string fullFileName) { PerformImportDaDAction(0, 0, fullFileName); }
        ///<summary>Entfernt die aktuell ausgewählte Datei aus der Liste Objektimport Drag&amp;Drop.</summary>
        ///<exception cref="System.IO.IOException">Die Zusi-Benutzeroberflächer verhielt sich nicht wie erwartet.</exception>
        ///<exception cref="System.TimeoutException">Ein Dialog wurde nicht innerhalb der erwarteten Zeitspanne geöffnet oder konnte nicht erkannt werden.</exception>
        ///<exception cref="System.IndexOutOfRangeException">Die Zusi-Benutzeroberfläche ist nicht in einem Zustand, der diese Aktion zulässt.</exception>
        public static void ImportDaDRemoveFile() { PerformImportDaDAction(1, 0, null); }
        ///<summary>Leert die Liste Objektimport Drag&amp;Drop.</summary>
        ///<exception cref="System.IO.IOException">Die Zusi-Benutzeroberflächer verhielt sich nicht wie erwartet.</exception>
        ///<exception cref="System.TimeoutException">Ein Dialog wurde nicht innerhalb der erwarteten Zeitspanne geöffnet oder konnte nicht erkannt werden.</exception>
        ///<exception cref="System.IndexOutOfRangeException">Die Zusi-Benutzeroberfläche ist nicht in einem Zustand, der diese Aktion zulässt.</exception>
        public static void ImportDaDReset() { PerformImportDaDAction(2, 0, null); }
        ///<summary>Schiebt die ausgewählte Datei in der Liste Objektimport Drag&amp;Drop eines nach oben.</summary>
        ///<exception cref="System.IO.IOException">Die Zusi-Benutzeroberflächer verhielt sich nicht wie erwartet.</exception>
        ///<exception cref="System.TimeoutException">Ein Dialog wurde nicht innerhalb der erwarteten Zeitspanne geöffnet oder konnte nicht erkannt werden.</exception>
        ///<exception cref="System.IndexOutOfRangeException">Die Zusi-Benutzeroberfläche ist nicht in einem Zustand, der diese Aktion zulässt.</exception>
        static void ImportDaDUp() { PerformImportDaDAction(4, 0, null); }
        ///<summary>Schiebt die ausgewählte Datei in der Liste Objektimport Drag&amp;Drop eines nach unten.</summary>
        ///<exception cref="System.IO.IOException">Die Zusi-Benutzeroberflächer verhielt sich nicht wie erwartet.</exception>
        ///<exception cref="System.TimeoutException">Ein Dialog wurde nicht innerhalb der erwarteten Zeitspanne geöffnet oder konnte nicht erkannt werden.</exception>
        ///<exception cref="System.IndexOutOfRangeException">Die Zusi-Benutzeroberfläche ist nicht in einem Zustand, der diese Aktion zulässt.</exception>
        public static void ImportDaDDown() { PerformImportDaDAction(5, 0, null); }
        ///<summary>Öffnet eine Liste zum Objektimport Drag&amp;Drop.</summary>
        ///<param name="fullFileName">Die einzufügende Dateiliste, es wird der volle Systempfad benötigt.</param>
        ///<exception cref="System.IO.IOException">Die Zusi-Benutzeroberflächer verhielt sich nicht wie erwartet.</exception>
        ///<exception cref="System.TimeoutException">Ein Dialog wurde nicht innerhalb der erwarteten Zeitspanne geöffnet oder konnte nicht erkannt werden.</exception>
        ///<exception cref="System.IndexOutOfRangeException">Die Zusi-Benutzeroberfläche ist nicht in einem Zustand, der diese Aktion zulässt.</exception>
        public static void ImportDaDOpenList(string fullFileName) { PerformImportDaDAction(9, 0, fullFileName); }
        ///<summary>Speichert den Objektimport Drag&amp;Drop in einer Liste.</summary>
        ///<param name="fullFileName">Die einzufügende Dateiliste, es wird der volle Systempfad benötigt.</param>
        ///<exception cref="System.IO.IOException">Die Zusi-Benutzeroberflächer verhielt sich nicht wie erwartet.</exception>
        ///<exception cref="System.TimeoutException">Ein Dialog wurde nicht innerhalb der erwarteten Zeitspanne geöffnet oder konnte nicht erkannt werden.</exception>
        ///<exception cref="System.IndexOutOfRangeException">Die Zusi-Benutzeroberfläche ist nicht in einem Zustand, der diese Aktion zulässt.</exception>
        public static void ImportDaDSaveList(string fullFileName) { PerformImportDaDAction(10, 0, fullFileName); }

        ///<summary>Ruft die Liste aktueller Dateien im Objektimport Drag&amp;Drop ab.</summary>
        ///<exception cref="System.IO.IOException">Die Zusi-Benutzeroberflächer verhielt sich nicht wie erwartet.</exception>
        ///<exception cref="System.TimeoutException">Ein Dialog wurde nicht innerhalb der erwarteten Zeitspanne geöffnet oder konnte nicht erkannt werden.</exception>
        ///<exception cref="System.IndexOutOfRangeException">Die Zusi-Benutzeroberfläche ist nicht in einem Zustand, der diese Aktion zulässt.</exception>
        public static string[] ImportDaDGetList() { return PerformImportDaDAction(-1, 0, null); }
        ///<summary>Wählt den Index des angegebenen Eintrags in der Liste Objektimport Drag&amp;Drop aus.</summary>
        ///<exception cref="System.IO.IOException">Die Zusi-Benutzeroberflächer verhielt sich nicht wie erwartet.</exception>
        ///<exception cref="System.TimeoutException">Ein Dialog wurde nicht innerhalb der erwarteten Zeitspanne geöffnet oder konnte nicht erkannt werden.</exception>
        ///<exception cref="System.IndexOutOfRangeException">Die Zusi-Benutzeroberfläche ist nicht in einem Zustand, der diese Aktion zulässt.</exception>
        public static void ImportDaDSelectItem(int i) { PerformImportDaDAction(-2, i, null); }
    }
}
