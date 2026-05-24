using System.Diagnostics;
using System.Windows;
using System.IO;
using System.Windows.Input;

namespace WirelessBatteryMonitor
{
    public partial class InfoWindow : Window
    {
        public InfoWindow()
        {
            InitializeComponent();
        }

        // Erlaubt das Verschieben des rahmenlosen Fensters
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        // Schließt nur dieses Info-Fenster
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // --- DEINE LINKS ---

        private void BtnInstagram_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://www.instagram.com/georgblack1989/");
        }

        private void BtnGitHub_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/GB89000?tab=repositories");
        }

        private void BtnPayPal_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://buymeacoffee.com/georgblack89");
        }

        private void BtnDescription_Click(object sender, RoutedEventArgs e)
        {
            // Pfad zur PDF-Datei (hier wird angenommen, dass sie im gleichen Ordner wie die App liegt)
            string pdfName = "Documentation.pdf";
            string pdfPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, pdfName);

            try
            {
                if (File.Exists(pdfPath))
                {
                    // Startet die PDF mit der im Windows hinterlegten Standard-Anwendung
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = pdfPath,
                        UseShellExecute = true // Wichtig ab .NET Core / .NET 5+
                    });
                }
                else
                {
                    System.Windows.MessageBox.Show($"The file '{pdfName}' was not found in the application directory.",
                                    "File not found",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"The documentation could not be opened:\n{ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        // Hilfsmethode, um den Standard-Browser sauber zu öffnen
        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Could not open the link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}