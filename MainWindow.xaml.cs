using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace WirelessBatteryMonitor
{
    public partial class MainWindow : Window
    {
        private System.Windows.Controls.ComboBox[] _btComboBoxes;
        private System.Windows.Controls.ComboBox[] _dongleComboBoxes;

        private List<string> _allBtDevices;
        private List<string> _allDongleProviders;

        private bool _isUpdating = false;
        private const string NoneOption = "None";

        public MainWindow()
        {
            InitializeComponent();

            _btComboBoxes = new System.Windows.Controls.ComboBox[] { CmbBt1, CmbBt2, CmbBt3, CmbBt4, CmbBt5, CmbBt6, CmbBt7, CmbBt8, CmbBt9, CmbBt10 };
            _dongleComboBoxes = new System.Windows.Controls.ComboBox[] { CmbDongle1, CmbDongle2, CmbDongle3, CmbDongle4, CmbDongle5, CmbDongle6, CmbDongle7, CmbDongle8, CmbDongle9, CmbDongle10 };

            // Starten mit leeren Listen (werden später von der App befüllt)
            _allBtDevices = new List<string>();
            _allDongleProviders = new List<string>();

            LoadSettingsToUI();
        }

        // ==========================================
        // SCHNITTSTELLE FÜR DIE ECHTEN GERÄTE
        // ==========================================
        public void SetAvailableDevices(List<string> btDevices, List<string> dongleProviders)
        {
            _allBtDevices = btDevices ?? new List<string>();
            _allDongleProviders = dongleProviders ?? new List<string>();

            // Jetzt haben wir die echten Daten! Wir laden die UI sofort neu, 
            // damit die Dropdowns befüllt und die Settings angewendet werden.
            LoadSettingsToUI();
        }
                

        // ==========================================
        // EINSTELLUNGEN LADEN & SPEICHERN
        // ==========================================

        private void LoadSettingsToUI()
        {
            _isUpdating = true; // Events blockieren, damit WPF sich nicht im Kreis dreht

            var settings = SettingsManager.Load();
            ChkStartHidden.IsChecked = settings.StartHidden;
            TxtRefreshTime.Text = settings.RefreshTimeSeconds.ToString();

            // NEU: Farbeinstellungen laden
            ChkTransparentBg.IsChecked = settings.IsBackgroundTransparent;
            TxtBgColorHex.Text = settings.BackgroundColorHex;
            TxtTextColorHex.Text = settings.TextColorHex;
            ChkDynamicTextColor.IsChecked = settings.UseDynamicTextColor;

            // Wenn Transparent aktiv ist, blenden/grauen wir das Hintergrund-Textfeld aus
            PanelBgColor.IsEnabled = !settings.IsBackgroundTransparent;

            // 1. Ziel-Auswahl für Bluetooth auslesen
            var targetBt = new string[_btComboBoxes.Length];
            for (int i = 0; i < _btComboBoxes.Length; i++)
            {
                if (i < settings.SelectedBluetooth.Count && _allBtDevices.Contains(settings.SelectedBluetooth[i]))
                    targetBt[i] = settings.SelectedBluetooth[i];
                else
                    targetBt[i] = NoneOption;
            }

            // 2. Ziel-Auswahl für Dongles auslesen
            var targetDongle = new string[_dongleComboBoxes.Length];
            for (int i = 0; i < _dongleComboBoxes.Length; i++)
            {
                if (i < settings.SelectedDongles.Count && _allDongleProviders.Contains(settings.SelectedDongles[i]))
                    targetDongle[i] = settings.SelectedDongles[i];
                else
                    targetDongle[i] = NoneOption;
            }

            // 3. Jetzt alles sauber in einem Rutsch zuweisen (WPF freundlich!)
            ApplySelectionsAndFilter(_btComboBoxes, _allBtDevices, targetBt);
            ApplySelectionsAndFilter(_dongleComboBoxes, _allDongleProviders, targetDongle);

            _isUpdating = false; // Events wieder erlauben

            using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            ChkAutostart.IsChecked = key?.GetValue("BatteryTray") != null;
        }

        private void UpdateDropdowns(System.Windows.Controls.ComboBox[] comboBoxes, List<string> masterList)
        {
            _isUpdating = true; // <-- WICHTIG: Verhindert Endlosschleifen beim Klicken!

            var currentSelections = new string[comboBoxes.Length];
            for (int i = 0; i < comboBoxes.Length; i++)
            {
                currentSelections[i] = comboBoxes[i].SelectedItem as string ?? NoneOption;
            }

            ApplySelectionsAndFilter(comboBoxes, masterList, currentSelections);

            _isUpdating = false;
        }



        private void ApplySelectionsAndFilter(System.Windows.Controls.ComboBox[] comboBoxes, List<string> masterList, string[] desiredSelections)
        {
            for (int i = 0; i < comboBoxes.Length; i++)
            {
                var cb = comboBoxes[i];
                var currentSelection = desiredSelections[i];

                // Welche Geräte sind von den ANDEREN Boxen belegt?
                var takenByOthers = desiredSelections
                    .Where((val, index) => index != i && val != NoneOption)
                    .ToList();

                var availableItems = new List<string> { NoneOption };
                availableItems.AddRange(masterList.Where(device => !takenByOthers.Contains(device)));

                // WICHTIG: Nur einmal zuweisen, WPF verschluckt sich sonst!
                cb.ItemsSource = availableItems;
                cb.SelectedItem = currentSelection;
            }
        }

        private void SetAutostart(bool enable)
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (enable)
            {
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                key.SetValue("BatteryTray", $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue("BatteryTray", false);
            }
        }

        // ==========================================
        // EVENTS
        // ==========================================

        private void CmbBt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdating) return;
            UpdateDropdowns(_btComboBoxes, _allBtDevices);
        }

        private void CmbDongle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdating) return;
            UpdateDropdowns(_dongleComboBoxes, _allDongleProviders);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            var infoWin = new InfoWindow();

            // Setzt das Einstellungsfenster als "Besitzer", damit das Info-Fenster 
            // schön zentriert darüber auftaucht.
            infoWin.Owner = this;

            // ShowDialog blockiert das Hauptfenster, bis das Info-Fenster geschlossen wird.
            // Das verhindert, dass der User aus Versehen 10 Info-Fenster öffnet.
            infoWin.ShowDialog();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        // 2. Klick-Event für die Checkbox (damit sich das Textfeld dynamisch anpasst)
        private void ChkTransparentBg_Click(object sender, RoutedEventArgs e)
        {
            PanelBgColor.IsEnabled = ChkTransparentBg.IsChecked != true;
        }

        private void BtnPickTextColor_Click(object sender, RoutedEventArgs e)
        {
            // Wir rufen den klassischen Windows-Dialog auf
            using var colorDialog = new System.Windows.Forms.ColorDialog();

            // Kleines Extra: Wenn schon ein gültiger Hex-Code im Textfeld steht, 
            // markieren wir diese Farbe direkt im Dialog als Startwert!
            try
            {
                colorDialog.Color = System.Drawing.ColorTranslator.FromHtml(TxtTextColorHex.Text);
            }
            catch
            {
                // Falls der User vorher Quatsch ins Textfeld getippt hat, ignorieren wir das einfach.
            }

            // Dialog anzeigen. Wenn der User auf "OK" klickt...
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // ... übersetzen wir die ausgewählte Farbe zurück in einen Hex-String 
                // und schreiben ihn in das Textfeld!
                TxtTextColorHex.Text = System.Drawing.ColorTranslator.ToHtml(colorDialog.Color);
            }
        }

        private void BtnPickBgColor_Click(object sender, RoutedEventArgs e)
        {
            // Wir rufen den klassischen Windows-Dialog auf
            using var colorDialog = new System.Windows.Forms.ColorDialog();

            // Kleines Extra: Wenn schon ein gültiger Hex-Code im Textfeld steht, 
            // markieren wir diese Farbe direkt im Dialog als Startwert!
            try
            {
                colorDialog.Color = System.Drawing.ColorTranslator.FromHtml(TxtBgColorHex.Text);
            }
            catch
            {
                // Falls der User vorher Quatsch ins Textfeld getippt hat, ignorieren wir das einfach.
            }

            // Dialog anzeigen. Wenn der User auf "OK" klickt...
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // ... übersetzen wir die ausgewählte Farbe zurück in einen Hex-String 
                // und schreiben ihn in das Textfeld!
                TxtBgColorHex.Text = System.Drawing.ColorTranslator.ToHtml(colorDialog.Color);
            }
        }

        private void btnDefaults_Click(object sender, RoutedEventArgs e)
        {
            // Eine frische Instanz erzeugen (enthält alle Standardwerte aus Schritt 1)
            var defaultSettings = new AppSettings();

            // UI-Elemente mit den Standardwerten überschreiben
            ChkStartHidden.IsChecked = defaultSettings.StartHidden;
            TxtRefreshTime.Text = defaultSettings.RefreshTimeSeconds.ToString();

            ChkTransparentBg.IsChecked = defaultSettings.IsBackgroundTransparent;
            TxtBgColorHex.Text = defaultSettings.BackgroundColorHex;
            TxtTextColorHex.Text = defaultSettings.TextColorHex;
            ChkDynamicTextColor.IsChecked = defaultSettings.UseDynamicTextColor;

            // Hintergrund-Eingabefeld je nach Transparenz-Standard aktivieren/deaktivieren
            PanelBgColor.IsEnabled = !defaultSettings.IsBackgroundTransparent;

            // Alle Bluetooth- und Dongle-Dropdowns wieder auf "None" (Index 0) zurücksetzen
            _isUpdating = true; // Events kurz blockieren, damit WPF ruhig bleibt

            foreach (var cb in _btComboBoxes)
            {
                if (cb.Items.Count > 0) cb.SelectedIndex = 0; // Setzt auf "None"
            }

            foreach (var cb in _dongleComboBoxes)
            {
                if (cb.Items.Count > 0) cb.SelectedIndex = 0; // Setzt auf "None"
            }

            _isUpdating = false;

            // Trigger die Dropdown-Filter-Updates manuell einmal, damit die UI weiß, 
            // dass die Geräte wieder in allen Boxen verfügbar sind.
            UpdateDropdowns(_btComboBoxes, _allBtDevices);
            UpdateDropdowns(_dongleComboBoxes, _allDongleProviders);
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            // 1. Refresh Time prüfen
            if (!int.TryParse(TxtRefreshTime.Text, out int refreshTime))
            {
                System.Windows.MessageBox.Show("Please enter a valid number for the refresh time!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Ausgewählte Geräte aus den Dropdowns sammeln (Alle, die nicht "None" sind)
            var activeBluetooth = _btComboBoxes
                .Select(cb => cb.SelectedItem as string)
                .Where(val => val != null && val != NoneOption)
                .ToList();

            var activeDongles = _dongleComboBoxes
                .Select(cb => cb.SelectedItem as string)
                .Where(val => val != null && val != NoneOption)
                .ToList();

            // ===============================================================
            // 3. Settings speichern (HIER SIND DIE FARBEN JETZT INTEGRIERT!)
            // ===============================================================
            var settings = new AppSettings
            {
                StartHidden = ChkStartHidden.IsChecked == true,
                RefreshTimeSeconds = refreshTime,
                SelectedBluetooth = activeBluetooth,
                SelectedDongles = activeDongles,

                // --- NEU: Farbeinstellungen speichern ---
                IsBackgroundTransparent = ChkTransparentBg.IsChecked == true,
                BackgroundColorHex = TxtBgColorHex.Text,
                TextColorHex = TxtTextColorHex.Text,
                UseDynamicTextColor = ChkDynamicTextColor.IsChecked == true
            };

            SettingsManager.Save(settings);

            // 4. Autostart in Registry setzen
            SetAutostart(ChkAutostart.IsChecked == true);

            // TODO: Später hier den Timer im TrayIconManager an die neue refreshTime anpassen!

            // Dem Hauptprogramm sagen: "Lade die Icons SOFORT neu!"
            if (System.Windows.Application.Current is App currentApp)
            {
                // Dein bisheriger Aufruf
                currentApp.RefreshTrayIcons();

                // HIER rufen wir unsere neue Methode für die Farben auf!
                // (Falls TrayManager in deiner App.xaml.cs public zugänglich ist)
                if (currentApp.TrayManager != null)
                {
                    currentApp.TrayManager.ForceRedrawIcons();
                }
            }

            // 5. Fenster verstecken (Apply = Speichern + Schließen)
            this.Hide();
        }


        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            // 1. Änderungen verwerfen -> Wir laden den alten Stand in die UI
            LoadSettingsToUI();

            // 2. WPF-Magie: Wir nutzen den Dispatcher mit der Priorität "ContextIdle".
            // Das bedeutet: WPF wartet ab, bis alle visuellen Updates (Dropdown-Texte) 
            // zu 100% fertig gezeichnet sind. Erst danach wird das Fenster versteckt!
            this.Dispatcher.InvokeAsync(() =>
            {
                this.Hide();
            }, System.Windows.Threading.DispatcherPriority.ContextIdle);
        }
    }
}