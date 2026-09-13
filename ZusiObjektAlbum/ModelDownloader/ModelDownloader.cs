using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ZusiObjektAlbum.ModelDownloader
{
    /// <summary>
    /// Lädt eine Datei per HTTP herunter und meldet den Fortschritt in Prozent.
    /// Schreibt zunächst in eine .download-Temp-Datei und benennt erst nach
    /// erfolgreichem Abschluss um, damit bei Abbruch/Fehler keine
    /// halbfertige Zieldatei liegen bleibt.
    ///
    /// Hinweis: raw.githubusercontent.com begrenzt einzelne Dateien auf
    /// 100 MB - für größere ONNX-Modelle stattdessen die Browser-Download-URL
    /// eines GitHub-Release-Assets verwenden (bis 2 GB), z.B.
    /// https://github.com/&lt;user&gt;/&lt;repo&gt;/releases/download/&lt;tag&gt;/vision_model.onnx
    /// </summary>
    public static class ModelDownloader
    {
        private static readonly HttpClient HttpClient = new();

        public static async Task DownloadFileAsync(
            string url,
            string destinationPath,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            string? dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string tempPath = destinationPath + ".download";

            using (HttpResponseMessage response = await HttpClient.GetAsync(
                       url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                long? totalBytes = response.Content.Headers.ContentLength;

                await using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using FileStream fileStream = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);

                byte[] buffer = new byte[81920];
                long totalRead = 0;
                int bytesRead;
                int lastReportedPercent = -1;

                while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    totalRead += bytesRead;

                    if (totalBytes is > 0)
                    {
                        int percent = (int)(totalRead * 100 / totalBytes.Value);
                        if (percent != lastReportedPercent)
                        {
                            progress?.Report(percent);
                            lastReportedPercent = percent;
                        }
                    }
                }
            }

            File.Copy(tempPath, destinationPath, overwrite: true);
            File.Delete(tempPath);
        }
    }
}
