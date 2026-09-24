using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;

// Allgemeine Informationen über eine Assembly werden über die folgenden
// Attribute gesteuert. Ändern Sie diese Attributwerte, um die Informationen zu ändern,
// die einer Assembly zugeordnet sind.
[assembly: AssemblyTitle("Zusi•Objektalbum")]
[assembly: AssemblyDescription("Zusi Objects Explorer")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("ZusiObjektAlbum")]
[assembly: AssemblyCopyright("Copyright © 2018-2026 Holger Maaß, Harold Linke")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Durch Festlegen von ComVisible auf FALSE werden die Typen in dieser Assembly
// für COM-Komponenten unsichtbar.  Wenn Sie auf einen Typ in dieser Assembly von
// COM aus zugreifen müssen, sollten Sie das ComVisible-Attribut für diesen Typ auf "True" festlegen.
[assembly: ComVisible(false)]

//Um mit dem Erstellen lokalisierbarer Anwendungen zu beginnen, legen Sie
//<UICulture>ImCodeVerwendeteKultur</UICulture> in der .csproj-Datei
//in einer <PropertyGroup> fest.  Wenn Sie in den Quelldateien beispielsweise Deutsch
//(Deutschland) verwenden, legen Sie <UICulture> auf \"de-DE\" fest.  Heben Sie dann die Auskommentierung
//des nachstehenden NeutralResourceLanguage-Attributs auf.  Aktualisieren Sie "en-US" in der nachstehenden Zeile,
//sodass es mit der UICulture-Einstellung in der Projektdatei übereinstimmt.

//[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]


[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //Speicherort der designspezifischen Ressourcenwörterbücher
                                     //(wird verwendet, wenn eine Ressource auf der Seite nicht gefunden wird,
                                     // oder in den Anwendungsressourcen-Wörterbüchern nicht gefunden werden kann.)
    ResourceDictionaryLocation.SourceAssembly //Speicherort des generischen Ressourcenwörterbuchs
                                              //(wird verwendet, wenn eine Ressource auf der Seite nicht gefunden wird,
                                              // designspezifischen Ressourcenwörterbuch nicht gefunden werden kann.)
)]

// log4net
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

// 0.1 - 07.10.2018
// initiale Version

// 0.2 - 08.10.2018
// Objekte aus Catenary verfügbar gemacht
// Datepfad eines Objektes kann in die Zwischenablage kopiert werden
// Bugfix: fehlerhafte XML-Datei führte zum Absturz
// ...1 und ff: Fehlersuche

// 0.2.1 - 08.10.2018
// Datepfad eines Objektes kann in eine Exportdatei kopiert werden

// 0.3 - 11.10.2018
// Oberfläche umgestaltet
// Verwaltung für Ordner in "Eigene Objekte" hinzugefügt

// 0.3.1 - 11.10.2018
// Dateipfadexport in Zwischenablage wieder möglich

// 0.4 - 11.10.2018
// Rundumsicht: Objekt per Maus drehbar um Y- und Z-Achse

// 0.4.1 - 11.10.2018
// Animation: Storyboard wieder entfernt, reine Double-Animation eingebaut

// 0.4.1 - 11.10.2018
// ZusiKlassenLib erneuert

// 0.4.2 - ?
// ?

// 0.4.3 - 20.05.2019
// alle Bibliotheken erneuert

// 0.4.3.1 - 23.05.2019
// Poschmann-Spezial (Fehlersuche: Window außerhalb des Monitors)

// 0.4.4 - 23.05.2019
// Bugfix: Window außerhalb des Monitors

// 0.5 - 13.06.2019
// Bugfix: Window außerhalb des Monitors bei mehreren Monitoren
// Möglichkeit des alternativen Datenpfades (statt Zusi.ZusiDataPath) hinzugefügt

// 0.6 - 31.08.2019
// Anpassung an Datenpfade Zusi 3.3

// 0.7 - 05.05.2021
// Anpassung an die Bibliotheken
// Automatisches Einlesen nur noch vom DatenOffiziell
// Landschaften werden zunächst als Dateipfad gespeichert und erst beim Öffnen geparst

// 8.0.1 - 14.9.2026
// Umstellung auf .net8
// Ähnlichkeitssuche hinzugefügt

// 8.0.2 - 15.9.2026
// Hintergrundentfernen für Ähnlichkeitssuche

// 8.0.3 - 16.9.2026
// Einfügen Bild mit ctrl+V überarbeitet, sodass auch Bilder aus der Zwischenablage eingefügt werden können, die mit ctrl-c aus dem Explorer kopiert wurden
// Kopieren von Dateipfad von 3D-Modelle in die Zwischenablage erweitert


[assembly: AssemblyVersion("8.0.5")]
