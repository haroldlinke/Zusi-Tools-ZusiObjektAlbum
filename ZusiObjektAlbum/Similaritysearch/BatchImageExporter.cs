using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using ZusiKlassenLib.Landscape;
using ZusiObjektAlbum.MVVM;
using ZusiObjektAlbum.Core;

namespace ZusiObjektAlbum.Similaritysearch
{
    /// <summary>
    /// Rendert Objekte der Zusi-Objektdatenbank und embedded sie per CLIP
    /// direkt in eine index.bin für die Ähnlichkeitssuche - OHNE die
    /// gerenderten Bilder dauerhaft auf der Platte zu speichern. Das Bild
    /// existiert nur kurz im Speicher (RenderTargetBitmap), wird sofort
    /// eingebettet und danach verworfen.
    ///
    /// Für Thumbnails in der Ergebnisliste gibt es RenderPreview: rendert bei
    /// Bedarf ein einzelnes Bild direkt aus dem im Index gespeicherten echten
    /// .ls3-Pfad - die Suchseite cached das Ergebnis selbst im Speicher
    /// (siehe ObjectPreviewImageConverter), damit nicht bei jedem Scrollen
    /// neu gerendert wird.
    ///
    /// Wiederverwendet bewusst die vorhandene Lade-/Kamera-Logik aus
    /// ObjectModel, Zusi3DModel und CameraData.
    ///
    /// Wiederaufnehmbar: ein Objekt, das schon in index.bin steht, wird beim
    /// nächsten Lauf übersprungen (kein erneutes Laden/Rendern nötig). Um ein
    /// einzelnes Objekt gezielt zu aktualisieren (z.B. nach einer
    /// Modelländerung): seinen Eintrag aus dem Index entfernen (oder die
    /// ganze index.bin löschen) und den Lauf erneut starten.
    ///
    /// Muss auf einem STA-Thread mit aktivem Dispatcher laufen, da
    /// RenderTargetBitmap einen Visual-Tree/Layout-Pass braucht. Am
    /// einfachsten aus App.xaml.cs (OnStartup) oder einem Menü-/Command-
    /// Handler heraus aufrufen, siehe Hinweis am Ende dieser Datei.
    /// </summary>
    public static class BatchImageExporter
    {
        private const int ImageSize = 512;

        // 46° war die in Zusi3DModel für die Kamera-Abstandsberechnung
        // verwendete Halbwinkel-Annahme (siehe "Math2D.Radians(46)" dort) -
        // das FieldOfView der Kamera muss dazu passen (voller Öffnungswinkel).
        private const double FieldOfViewDegrees = 92.0;

        // Nach wie vielen Objekten die index.bin zwischengespeichert wird -
        // bei einem Abbruch mitten im 12.000er-Lauf geht so nur der Fortschritt
        // seit dem letzten Speichern verloren, nicht der ganze Lauf.
        private const int IndexSaveInterval = 200;

    private static readonly string[] ViewNames = { "left", "right", "back", "front" };

    //---------------------------------------------------------------------
    /// <summary>
    /// Blockierende Variante für den Headless-CLI-Aufruf (--export-and-index,
    /// siehe App.xaml.cs-Beispiel unten) - dort ist UI-Reaktionsfähigkeit
    /// irrelevant, weil ohnehin kein MainWindow gezeigt wird.
    ///
    /// Für den Aufruf aus einem Menüpunkt der laufenden GUI stattdessen
    /// ExportAndIndexAllAsync verwenden, damit die Anwendung während der
    /// (mehrstündigen) Laufzeit bedienbar bleibt.
    ///
    /// debugImageFolder ist OPTIONAL (null = Standard, empfohlen): wird
    /// nur noch verwendet, um zusätzlich zur Indizierung auch PNGs auf die
    /// Platte zu schreiben (z.B. um Kamera/Beleuchtung mal visuell zu
    /// prüfen). Für den normalen Betrieb einfach null übergeben - dann
    /// werden KEINE der ~60.000 Bilddateien mehr angelegt, die gerenderten
    /// Bilder existieren nur kurz im Speicher zum Einbetten.
    /// </summary>
    public static void ExportAndIndexAll(string? debugImageFolder, string modelPath, string indexPath)
        {
            var progress = new Progress<string>(Console.WriteLine);
            ExportAndIndexAllAsync(debugImageFolder, modelPath, indexPath, progress).GetAwaiter().GetResult();
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Wie ExportAndIndexAll, aber async und UI-freundlich: gibt nach
        /// jedem Objekt kurz an den Dispatcher ab (Dispatcher.Yield), damit
        /// Eingabe- und Zeichenereignisse verarbeitet werden können, statt die
        /// Anwendung für die gesamte (mehrstündige) Laufzeit einzufrieren.
        ///
        /// WICHTIG: Das WPF-Rendern (RenderTargetBitmap) selbst MUSS auf dem
        /// UI-Thread bleiben, das lässt sich nicht per Task.Run auslagern -
        /// "async" heißt hier "kooperativ Luft lassen", nicht "auf einem
        /// anderen Thread rendern". Muss deshalb weiterhin vom UI-Thread aus
        /// aufgerufen werden (z.B. aus einem Menü-/Command-Handler).
        ///
        /// progress bekommt dieselben Fortschrittsmeldungen wie zuvor über
        /// Console.WriteLine - z.B. direkt an eine Statusleisten-Property
        /// binden. cancellationToken erlaubt optional, einen laufenden Export
        /// sauber abzubrechen (aktueller Objekt-Durchlauf wird noch fertig,
        /// dann wird die index.bin final gespeichert).
        /// </summary>
        public static async Task ExportAndIndexAllAsync(
            string? debugImageFolder,
            string modelPath,
            string indexPath,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(debugImageFolder))
            {
                Directory.CreateDirectory(debugImageFolder);
                progress?.Report($"Hinweis: Debug-Bilder werden zusätzlich nach '{debugImageFolder}' geschrieben.");
            }

            progress?.Report("Lese Objektdatenbank ein...");
            List<ObjectModel> leaves = LoadLeaves();
            progress?.Report($"{leaves.Count} Objekte gefunden.");

            progress?.Report("Lade CLIP-ONNX-Modell...");
            using var embedder = new ClipEmbedder(modelPath);

            EmbeddingIndex index = LoadOrCreateIndex(indexPath);
            progress?.Report($"Index geladen: {index.ObjectCount} Objekte, {index.Count} Ansichten.");

            var stopwatch = Stopwatch.StartNew();
            var failedLog = new List<string>();
            int embedded = 0, fullySkipped = 0;
            bool cancelled = false;

            for (int i = 0; i < leaves.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancelled = true;
                    progress?.Report("Abbruch angefordert - speichere Fortschritt...");
                    break;
                }

                ObjectModel om = leaves[i];
                try
                {
                    bool didEmbed = ProcessObjectWithIndex(om, embedder, index, debugImageFolder);
                    if (didEmbed) embedded++;
                    else fullySkipped++;
                }
                catch (Exception ex)
                {
                    failedLog.Add($"{om.DisplayName}: {ex.Message}");
                }

                int done = i + 1;
                if (done % IndexSaveInterval == 0 || done == leaves.Count)
                {
                    index.SaveToFile(indexPath);
                    progress?.Report(
                        $"{done}/{leaves.Count}  (embedded: {embedded}, übersprungen: {fullySkipped}, " +
                        $"fehler: {failedLog.Count})  |  index.bin gespeichert ({index.ObjectCount} Objekte)");
                }

                // Kooperativ an den Dispatcher abgeben: verarbeitet in der
                // Zwischenzeit liegen gebliebene UI-Ereignisse (Klicks, Repaint),
                // bevor es mit dem nächsten Objekt weitergeht.
                await Dispatcher.Yield(DispatcherPriority.Background);
            }

            if (cancelled)
            {
                index.SaveToFile(indexPath);
            }

            if (failedLog.Count > 0)
            {
                string logFolder = !string.IsNullOrEmpty(debugImageFolder)
                    ? debugImageFolder
                    : System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(indexPath)) ?? ".";
                File.WriteAllLines(System.IO.Path.Combine(logFolder, "failed.txt"), failedLog);
            }

            string summary = cancelled
                ? $"Abgebrochen nach {FormatDuration(stopwatch.Elapsed)}. "
                : $"Fertig nach {FormatDuration(stopwatch.Elapsed)}. ";

            progress?.Report(
                summary +
                $"{embedded} neu/erneut eingebettet, {fullySkipped} bereits vorhanden, " +
                $"{failedLog.Count} fehlgeschlagen.");
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Wie ExportAndIndexAll, aber für ein einzelnes Objekt - praktisch,
        /// um die Pipeline an ein paar Testobjekten durchzuspielen.
        /// </summary>
        public static void ExportAndIndexSingle(ObjectModel om, ClipEmbedder embedder, string indexPath, string? debugImageFolder = null)
        {
            EmbeddingIndex index = LoadOrCreateIndex(indexPath);
            bool didEmbed = ProcessObjectWithIndex(om, embedder, index, debugImageFolder);
            index.SaveToFile(indexPath);

            Console.WriteLine($"'{om.DisplayName}': eingebettet={didEmbed}");
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Rendert live ein einzelnes Vorschaubild für ein Objekt anhand
        /// seines echten .ls3-Quellpfads (wie er im Index als SourcePath
        /// steht) und der gewünschten Ansicht ("left"/"right"/"back"/"front").
        /// Für Thumbnails in der Ergebnisliste gedacht - kein Caching hier,
        /// das übernimmt der Aufrufer (siehe ObjectPreviewImageConverter).
        /// Gibt null zurück, wenn das Objekt nicht geladen werden konnte.
        /// </summary>
        public static BitmapSource? RenderPreview(string sourceLs3Path, string viewName)
        {
            try
            {
                LandschaftsDatei ld = new(sourceLs3Path);
                ld.Parse(true);

                ObjectModel om = new(ld.Root);
                Zusi3DModel model3D = new(om);
                if (om.IsErroneous || model3D.Model == null)
                {
                    return null;
                }

                int viewIndex = Array.IndexOf(ViewNames, viewName);
                if (viewIndex < 0 || viewIndex >= model3D.Cameras.Count)
                {
                    viewIndex = 0;
                }

                return RenderSingleView(model3D, model3D.Cameras[viewIndex]);
            }
            catch
            {
                return null;
            }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Debug-Hilfsmethode: rendert ein einzelnes Objekt UND schreibt die
        /// 4 Ansichten als PNG auf die Platte - für die visuelle Prüfung nach
        /// Änderungen an Kamera/Beleuchtung. Nicht Teil des normalen
        /// Indizierungs-Durchlaufs mehr (der schreibt keine Dateien mehr).
        /// </summary>
        public static bool ExportSingleForDebugging(ObjectModel om, string outputFolder)
        {
            try
            {
                LoadFully(om);

                Zusi3DModel model3D = new(om);
                if (om.IsErroneous || model3D.Model == null)
                {
                    return false;
                }

                string objectId = ComputeObjectId(GetSourceFileName(om));
                string dir = System.IO.Path.Combine(outputFolder, objectId);
                System.IO.Directory.CreateDirectory(dir);

                for (int i = 0; i < ViewNames.Length && i < model3D.Cameras.Count; i++)
                {
                    BitmapSource bmp = RenderSingleView(model3D, model3D.Cameras[i]);
                    SavePng(bmp, System.IO.Path.Combine(dir, ViewNames[i] + ".png"));
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        //---------------------------------------------------------------------
        private static EmbeddingIndex LoadOrCreateIndex(string indexPath)
        {
            return File.Exists(indexPath) ? EmbeddingIndex.LoadFromFile(indexPath) : new EmbeddingIndex();
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Kernlogik: rendert (falls noch nicht indiziert) alle 4 Ansichten
        /// direkt in den Speicher, embedded sie sofort und verwirft die
        /// Bilder danach wieder - außer debugImageFolder ist gesetzt, dann
        /// wird zusätzlich eine PNG-Kopie geschrieben. Gibt zurück, ob
        /// tatsächlich (neu) eingebettet wurde.
        /// </summary>
        private static bool ProcessObjectWithIndex(ObjectModel om, ClipEmbedder embedder, EmbeddingIndex index, string? debugImageFolder)
        {
            string sourceFile = GetSourceFileName(om);
            string objectId = ComputeObjectId(sourceFile);

            if (index.ContainsObject(objectId))
            {
                return false;
            }

            LoadFully(om);

            Zusi3DModel model3D = new(om);
            if (om.IsErroneous || model3D.Model == null)
            {
                throw new InvalidOperationException("Modell ist leer oder fehlerhaft.");
            }

            string? debugDir = null;
            if (!string.IsNullOrEmpty(debugImageFolder))
            {
                debugDir = System.IO.Path.Combine(debugImageFolder, objectId);
                System.IO.Directory.CreateDirectory(debugDir);
            }

            index.RemoveObject(objectId);
            for (int i = 0; i < ViewNames.Length && i < model3D.Cameras.Count; i++)
            {
                BitmapSource bmp = RenderSingleView(model3D, model3D.Cameras[i]);

                float[] vector = embedder.ComputeEmbedding(bmp);
                index.Add(new ViewEmbedding(objectId, ViewNames[i], vector, sourceFile));

                if (debugDir != null)
                {
                    SavePng(bmp, System.IO.Path.Combine(debugDir, ViewNames[i] + ".png"));
                }
            }

            return true;
        }

        //---------------------------------------------------------------------
        private static List<ObjectModel> LoadLeaves()
        {
            ObservableCollection<ObjectModel> tree = ObjectModel.Create();
            var leaves = new List<ObjectModel>();
            CollectLeaves(tree, leaves);
            return leaves;
        }

        //---------------------------------------------------------------------
        private static void CollectLeaves(IEnumerable<ObjectModel> nodes, List<ObjectModel> result)
        {
            foreach (ObjectModel om in nodes)
            {
                if (om.Object != null)
                {
                    result.Add(om);
                }
                else if (om.Children != null)
                {
                    CollectLeaves(om.Children, result);
                }
            }
        }

        //---------------------------------------------------------------------
        private static string FormatDuration(TimeSpan ts)
        {
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}min";
            if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}min {ts.Seconds}s";
            return $"{ts.Seconds}s";
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Lädt ein lazy referenziertes LandscapeObject vollständig - exakt
        /// wie in DataManager.OnSelectedObjectModelChanged.
        /// </summary>
        private static void LoadFully(ObjectModel om)
        {
            if (om.Object is LandscapeObject lo)
            {
                LandschaftsDatei ld = new(lo.Filename);
                ld.Parse(true);
                om.Object = ld.Root;
            }

            if (om.Object is not Landschaft)
            {
                throw new InvalidOperationException("Objekt konnte nicht als Landschaft geladen werden.");
            }
        }

        //---------------------------------------------------------------------
        private static string GetSourceFileName(ObjectModel om)
        {
            return om.Object switch
            {
                LandscapeObject lo => lo.Filename,
                Landschaft ls => ls.GetDocument()?.Filename ?? om.DisplayName,
                _ => om.DisplayName
            };
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Deterministische, eindeutige Objekt-ID aus dem echten Quellpfad -
        /// wird als ObjectId im Embedding-Index verwendet (und, falls gesetzt,
        /// auch als Ordnername für Debug-Bilder), damit Resume-/Update-Checks
        /// stabil funktionieren.
        /// </summary>
        private static string ComputeObjectId(string sourceFile)
        {
            string baseId = SanitizeFileName(
                System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetFileNameWithoutExtension(sourceFile)));
            string hash = ComputeShortHash(sourceFile);
            return $"{baseId}_{hash}";
        }

        //---------------------------------------------------------------------
        private static string ComputeShortHash(string input)
        {
            byte[] hashBytes = SHA1.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hashBytes, 0, 4).ToLowerInvariant();
        }

        //---------------------------------------------------------------------
        private static string SanitizeFileName(string name)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        //---------------------------------------------------------------------
        private static BitmapSource RenderSingleView(Zusi3DModel model3D, CameraData cameraData)
        {
            var camera = new PerspectiveCamera
            {
                Position = cameraData.Position,
                LookDirection = cameraData.LookDirection,
                UpDirection = cameraData.UpDirection,
                FieldOfView = FieldOfViewDegrees,
                NearPlaneDistance = 0.01,
                FarPlaneDistance = 10000
            };

            var modelVisual = new ModelVisual3D
            {
                Content = model3D.Model,
                Transform = model3D.Transform
            };

            var lightGroup = new Model3DGroup();
            lightGroup.Children.Add(new AmbientLight(Colors.White));
            lightGroup.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-1, -1, -2)));
            var lightVisual = new ModelVisual3D { Content = lightGroup };

            var viewport = new Viewport3D
            {
                Width = ImageSize,
                Height = ImageSize,
                Camera = camera
            };
            viewport.Children.Add(modelVisual);
            viewport.Children.Add(lightVisual);

            var background = new Rectangle
            {
                Width = ImageSize,
                Height = ImageSize,
                Fill = Brushes.White
            };

            var container = new Grid { Width = ImageSize, Height = ImageSize };
            container.Children.Add(background);
            container.Children.Add(viewport);

            container.Measure(new Size(ImageSize, ImageSize));
            container.Arrange(new Rect(0, 0, ImageSize, ImageSize));
            container.UpdateLayout();

            var rtb = new RenderTargetBitmap(ImageSize, ImageSize, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(container);
            rtb.Freeze();

            return rtb;
        }

        //---------------------------------------------------------------------
        private static void SavePng(BitmapSource bmp, string path)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            encoder.Save(fs);
        }
    }
}

// ---------------------------------------------------------------------------
// Integration in App.xaml.cs (Beispiel, für den Headless-CLI-Fall):
//
//   protected override void OnStartup(StartupEventArgs e)
//   {
//       base.OnStartup(e);
//
//       // Rendern UND indizieren, OHNE Bilder zu speichern (empfohlen):
//       //   ZusiObjektAlbum.exe --export-and-index "C:\Zusi\clip.onnx" "C:\Zusi\index.bin"
//       if (e.Args.Length >= 3 && e.Args[0] == "--export-and-index")
//       {
//           ZusiObjektAlbum.Tools.BatchImageExporter.ExportAndIndexAll(null, e.Args[1], e.Args[2]);
//           Shutdown();
//           return;
//       }
//
//       // ... normaler Startup, MainWindow anzeigen ...
//   }
//
// Aus einem Menü-/Command-Handler in der laufenden GUI stattdessen
// ExportAndIndexAllAsync(null, modelPath, indexPath, progress, cancellationToken)
// verwenden (siehe vorheriger OnIndexObjects-Handler) - der erste Parameter
// (debugImageFolder) einfach auf null setzen, dann werden keine der ~60.000
// Bilddateien mehr angelegt.
//
// Nach einem Abbruch (Absturz, Ctrl+C, Neustart) einfach denselben Befehl
// nochmal ausführen - bereits indizierte Objekte werden automatisch
// übersprungen. Neue Objekte (z.B. nach einem Zusi-Update) werden beim
// nächsten Lauf automatisch mit ergänzt, ohne die bestehende index.bin
// komplett neu aufzubauen.
// ---------------------------------------------------------------------------
