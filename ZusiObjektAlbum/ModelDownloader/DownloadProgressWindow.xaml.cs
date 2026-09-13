using System.ComponentModel;
using System.Threading;
using System.Windows;

namespace ZusiObjektAlbum
{
    public partial class DownloadProgressWindow : Window
    {
        private readonly CancellationTokenSource _cts = new();
        public CancellationToken CancellationToken => _cts.Token;

        public DownloadProgressWindow()
        {
            InitializeComponent();
        }

        public void ReportProgress(string status, int percent)
        {
            StatusText.Text = status;
            ProgressBarControl.Value = percent;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            CancelButton.IsEnabled = false;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _cts.Cancel();
        }
    }
}
