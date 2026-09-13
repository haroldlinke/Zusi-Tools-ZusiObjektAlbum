using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace ZusiObjektAlbum
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);


      if (e.Args.Length >= 2 && e.Args[0] == "--export-images")
      {
        ZusiObjektAlbum.Similaritysearch.BatchImageExporter.ExportAndIndexAll(e.Args[1], e.Args[2], e.Args[3]);
        //ZusiObjektAlbum.Similaritysearch.BatchImageExporter.ExportAllParallel(e.Args[1], degreeOfParallelism: 4);
        Shutdown();
        return;
      }


      FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));


        }
    }
}
