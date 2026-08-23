using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace Automatization
{
    ///<summary>Stellt Methoden für das fernsteuern bereit.</summary>
    public abstract class Zusi3AutomatisationCore
    {
        ///<summary>Erstellt eine Bedingung, mit der Schwebende (sofern das Fenster ausgewählt ist) und Angedockte (immer) Toolboxen ausgewählt werden.</summary>
        protected static Condition GetConditionDockParents()
        {
            return new OrCondition(new PropertyCondition(AutomationElement.ClassNameProperty, "TTBXDock"),
                                    new PropertyCondition(AutomationElement.ClassNameProperty, "TTBXFloatingWindowParent"));
        }
        ///<summary>Erstellt eine Bedingung, mit der alle zu diesen Prozessen gehörenden Fenster der angegebenen Klasse ausgewählt werden.</summary>
        protected static Condition GetConditionWindow(System.Diagnostics.Process[] prc, string className)
        {
            Condition PrcCondition = null;
            if (prc.Length == 0)
                PrcCondition = Condition.FalseCondition;
            else
            {
                foreach (System.Diagnostics.Process p1 in prc)
                {
                    if (PrcCondition == null)
                        PrcCondition = new PropertyCondition(AutomationElement.ProcessIdProperty, prc[0].Id);
                    else
                        PrcCondition = new OrCondition(PrcCondition, new PropertyCondition(AutomationElement.ProcessIdProperty, prc[0].Id));
                }
            }

            return new AndCondition(PrcCondition, new PropertyCondition(AutomationElement.ClassNameProperty, className));
        }
        ///<summary>Wandelt die FindAll-Methode des AutomationElement in ein Array um.</summary>
        protected static AutomationElement[] GetSomeoneForAEByCondition(AutomationElement[] baseAEl, Condition cond, TreeScope scope)
        {
            AutomationElementCollection[] coll = new AutomationElementCollection[baseAEl.Length];
            int valueCount = 0;
            for (int i = 0; i < baseAEl.Length; ++i)
            {
                AutomationElement el = baseAEl[i];
                coll[i] = el.FindAll(scope, cond);
                valueCount += coll[i].Count;
            }
            AutomationElement[] value = new AutomationElement[valueCount];
            valueCount = 0;
            for (int i = 0; i < baseAEl.Length; ++i)
            {
                coll[i].CopyTo(value, valueCount);
                valueCount += coll[i].Count;
            }
            return value;
        }
        ///<summary>Wandelt die FindAll-Methode des AutomationElement in ein Array um und ergänzt sie um ein Timeout von 60 Sekunden (zuzüglich Bearbeitungszeit Automatisierungsanfragen).</summary>
        protected static AutomationElement[] GetSomeoneForAEByConditionBlocking(AutomationElement[] baseAEl, Condition cond, TreeScope scope, int minCount)
        {
            AutomationElement[] value = GetSomeoneForAEByCondition(baseAEl, cond, scope);
            for (int i = 0; i < 200; ++i)
            {
                if (value.Length >= minCount)
                    return value;
                System.Threading.Thread.Sleep(300);
                value = GetSomeoneForAEByCondition(baseAEl, cond, scope);
            }
            throw new System.TimeoutException();
        }
        ///<summary>Sucht alle Kinder des angegebenen AutomationElement.</summary>
        protected static AutomationElement[] GetChildrenForAEByCondition(AutomationElement[] baseAEl, Condition cond) { return GetSomeoneForAEByCondition(baseAEl, cond, TreeScope.Children); }
        ///<summary>Sucht alle Kinder des angegebenen AutomationElement und erwartet mindestens die angegebene Anzahl ein Einträgen innerhalb 60+x Sekunden.</summary>
        protected static AutomationElement[] GetChildrenForAEByConditionBlocking(AutomationElement[] baseAEl, Condition cond, int minCount) { return GetSomeoneForAEByConditionBlocking(baseAEl, cond, TreeScope.Children, minCount); }
        ///<summary>Sucht alle Kinder des angegebenen AutomationElement.</summary>
        protected static AutomationElement[] GetAllChildrenForAE(AutomationElement[] baseAEl) { return GetChildrenForAEByCondition(baseAEl, Condition.TrueCondition); }
        ///<summary>Gibt die Namen-Eigenschaft aller AutomationElemente aus.</summary>
        protected static string[] GetAllNamesForArr(AutomationElement[] input)
        {
            string[] value = new string[input.Length];
            for (int i = 0; i < input.Length; ++i)
            {
                value[i] = (string)input[i].GetCurrentPropertyValue(AutomationElement.NameProperty);
            }
            return value;
        }
    }
}
