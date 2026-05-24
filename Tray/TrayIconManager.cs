using BatteryStatusIndicator.Devices;
using Microsoft.Win32;
using System.Windows;
using WirelessBatteryMonitor.Devices;

// Alias für Konfliktlösung
using WpfApp = System.Windows.Application;
using FontStyle = System.Drawing.FontStyle;

namespace WirelessBatteryMonitor.Tray;

public class TrayIconManager : IDisposable
{
    private readonly Dictionary<DeviceInfo, NotifyIcon> _deviceIcons = new();
    private readonly Dictionary<DeviceInfo, int> _lastKnownLevels = new();
    private readonly CompositeBatteryService _batteryService;
    private readonly System.Windows.Threading.DispatcherTimer _timer;
    private NotifyIcon _mainAppIcon;
    private AppSettings _currentSettings; // <-- Hier cachen wir die Einstellungen!

    public TrayIconManager(CompositeBatteryService batteryService, List<DeviceInfo> devices)
    {
        // 1. Einmaliges Laden beim Start
        _currentSettings = SettingsManager.Load();

        _batteryService = batteryService;

        foreach (var device in devices)
        {
            AddDeviceIcon(device);
        }

        // Fallback-Icon, falls die Liste leer ist!
        if (devices.Count == 0)
        {
            CreateMainAppIcon();
        }

        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        var initialSettings = SettingsManager.Load();
        int startSeconds = initialSettings.RefreshTimeSeconds < 1 ? 1 : initialSettings.RefreshTimeSeconds;

        _timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(startSeconds) };
        _timer.Tick += async (_, _) => await UpdateAllBatteriesAsync();
        _timer.Start();

        _ = UpdateAllBatteriesAsync();
    }

    private async Task UpdateAllBatteriesAsync()
    {
        var devices = _deviceIcons.Keys.ToList();

        foreach (var device in devices)
        {
            // Wir merken uns das Icon am Anfang NICHT mehr direkt, da es gelöscht werden könnte
            if (!_deviceIcons.ContainsKey(device)) continue;

            try
            {
                // Das Warten dauert eventuell ein paar Millisekunden...
                var level = await _batteryService.GetBatteryLevelAsync(device);

                // --- SICHERHEITS-CHECK ---
                // Wurde in der Zwischenzeit auf "Apply" geklickt und das Icon gelöscht?
                // Wenn ja (currentIcon ist null/weg), brechen wir hier sofort ab!
                if (!_deviceIcons.TryGetValue(device, out var currentIcon)) continue;

                if (level == null)
                {
                    if (_lastKnownLevels.TryGetValue(device, out int lastLevel))
                        UpdateOfflineIcon(currentIcon, $"{device.Name}: Off (Last Known: {lastLevel}%)");
                    else
                        UpdateOfflineIcon(currentIcon, $"{device.Name}: Off");

                    continue;
                }

                int percentage = level.Value;
                _lastKnownLevels[device] = percentage;
                string onlineText = $"{device.Name}: {percentage}%";

                WpfApp.Current.Dispatcher.Invoke(() =>
                {
                    // Wir nutzen jetzt currentIcon, weil wir sicher wissen, dass es noch existiert
                    if (!currentIcon.Visible) currentIcon.Visible = true;
                });

                UpdateIcon(currentIcon, percentage, onlineText);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Wakeup-Fehler bei {device.Name}: {ex.Message}");
            }
        }
    }

    public async void UpdateActiveDevices(List<DeviceInfo> activeDevices)
    {
        try
        {
            // 1. Alte Geräte entfernen (Vergleich NUR noch über den Namen!)
            var devicesToRemove = _deviceIcons.Keys.Where(od => !activeDevices.Any(nd => nd.Name == od.Name)).ToList();
            foreach (var d in devicesToRemove)
            {
                if (_deviceIcons.TryGetValue(d, out var icon))
                {
                    icon.Visible = false;
                    icon.Icon?.Dispose();
                    icon.Dispose();
                    _deviceIcons.Remove(d);
                    _lastKnownLevels.Remove(d);
                }
            }

            // 2. Neue Geräte nacheinander hinzufügen (Vergleich NUR noch über den Namen!)
            var currentKeys = _deviceIcons.Keys.ToList();
            var devicesToAdd = activeDevices.Where(nd => !currentKeys.Any(od => od.Name == nd.Name)).ToList();

            foreach (var d in devicesToAdd)
            {
                AddDeviceIcon(d);

                // Pause für Windows
                await Task.Delay(300);
            }

            // 3. Fallback-Icon (Zahnrad) prüfen
            if (_deviceIcons.Count == 0)
            {
                CreateMainAppIcon();
            }
            else if (_mainAppIcon != null)
            {
                _mainAppIcon.Visible = false;
                _mainAppIcon.Dispose();
                _mainAppIcon = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TRAY-Error] UpdateActiveDevices: {ex.Message}");
        }
    }

    public void AddDeviceIcon(DeviceInfo device)
    {
        try
        {
            // Gibt es das Icon schon? (Abfrage strikt nach NAME, DeviceId wird ignoriert!)
            if (_deviceIcons.Keys.Any(d => d.Name == device.Name)) return;

            // Icon sofort erstellen
            var notifyIcon = new NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = true,
                Text = $"{device.Name}: Initialization..."
            };

            var contextMenu = new ContextMenuStrip();
            var openItem = new ToolStripMenuItem("Open Settings", null, (_, _) => ShowMainWindow());
            openItem.Font = new System.Drawing.Font(openItem.Font, System.Drawing.FontStyle.Bold);

            contextMenu.Items.Add(openItem);
            contextMenu.Items.Add("-");
            contextMenu.Items.Add(new ToolStripMenuItem($"Device: {device.Name}") { Enabled = false });
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Close", null, (_, _) => ExitApplication());

            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.MouseDoubleClick += (_, _) => ShowMainWindow();

            // In unser Wörterbuch eintragen
            _deviceIcons.Add(device, notifyIcon);

            // Akku im Hintergrund checken
            _ = Task.Run(async () =>
            {
                try
                {
                    var level = await _batteryService.GetBatteryLevelAsync(device, System.Threading.CancellationToken.None);

                    if (!_deviceIcons.TryGetValue(device, out var currentIcon)) return;

                    if (level.HasValue)
                    {
                        _lastKnownLevels[device] = level.Value;
                        UpdateIcon(currentIcon, level.Value, $"{device.Name}: {level.Value}%");
                    }
                    else
                    {
                        UpdateOfflineIcon(currentIcon, $"{device.Name}: Off");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Error during initial update: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Error during icon creation: {ex.Message}");
        }
    }

    private void UpdateIcon(NotifyIcon icon, int percentage, string tooltipText)
    {
        WpfApp.Current.Dispatcher.Invoke(() =>
        {
            var oldIcon = icon.Icon;

            // Wir nutzen jetzt die Gecachten Settings (_currentSettings) 
            // statt hier SettingsManager.Load() aufzurufen!
            icon.Icon = BatteryIconGenerator.CreateBatteryIcon(percentage, _currentSettings);
            icon.Text = tooltipText;

            CleanupOldIcon(oldIcon);
        });
    }

    // ==========================================
    // FARBEN SOFORT ÜBERNEHMEN & CACHE ERNEUERN
    // ==========================================
    public void ForceRedrawIcons()
    {
        // 2. Settings NEU laden, da der Nutzer sie gerade geändert hat!
        _currentSettings = SettingsManager.Load();

        _lastKnownLevels.Clear();
        _ = UpdateAllBatteriesAsync();
    }

    private void UpdateOfflineIcon(NotifyIcon icon, string tooltipText)
    {
        WpfApp.Current.Dispatcher.Invoke(() =>
        {
            var oldIcon = icon.Icon;

            // Aufruf unserer neuen Generator-Klasse!
            icon.Icon = BatteryIconGenerator.CreateOfflineIcon(_currentSettings);

            if (icon.Text != tooltipText) icon.Text = tooltipText;
            if (!icon.Visible) icon.Visible = true;

            CleanupOldIcon(oldIcon);
        });
    }

    private void CleanupOldIcon(Icon oldIcon)
    {
        if (oldIcon != null && !ReferenceEquals(oldIcon, SystemIcons.Application))
        {
            BatteryIconGenerator.DestroyIcon(oldIcon.Handle);
            oldIcon.Dispose();
        }
    }

    private void ShowMainWindow()
    {
        WpfApp.Current.Dispatcher.Invoke(() =>
        {
            var win = WpfApp.Current.Windows.OfType<MainWindow>().FirstOrDefault();

            if (win == null)
            {
                win = new MainWindow();
                WpfApp.Current.MainWindow = win;
            }

            if (!win.IsVisible) win.Show();
            if (win.WindowState == WindowState.Minimized) win.WindowState = WindowState.Normal;

            win.Activate();
            win.Topmost = true;
            win.Topmost = false;
            win.Focus();
        });
    }

    private async void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            System.Diagnostics.Debug.WriteLine("[Power] System has woken up. Starting re-initialization...");

            await Task.Delay(5000);
            await _batteryService.RefreshAllProvidersAsync();

            if (WpfApp.Current is App currentApp)
            {
                await currentApp.AddBluetoothDevicesAsync(_batteryService);
            }

            await UpdateAllBatteriesAsync();
        }
    }

    private void ExitApplication()
    {
        _timer.Stop();
        Dispose();
        WpfApp.Current.Shutdown();
    }

    public void Dispose()
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _timer.Stop();
        foreach (var icon in _deviceIcons.Values)
        {
            icon.Visible = false;
            icon.Icon?.Dispose();
            icon.Dispose();
        }
        _deviceIcons.Clear();
        _lastKnownLevels.Clear();
    }

    private void CreateMainAppIcon()
    {
        if (_mainAppIcon != null) return;

        _mainAppIcon = new NotifyIcon
        {
            // Extrahiert automatisch das Icon, das du deiner EXE gegeben hast!
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName),
            Visible = true,
            Text = "BatteryTray - Settings"
        };

        var contextMenu = new ContextMenuStrip();
        var openItem = new ToolStripMenuItem("Open Settings", null, (_, _) => ShowMainWindow());
        openItem.Font = new Font(openItem.Font, FontStyle.Bold);

        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Close", null, (_, _) => ExitApplication());

        _mainAppIcon.ContextMenuStrip = contextMenu;
        _mainAppIcon.MouseDoubleClick += (_, _) => ShowMainWindow();
    }

    // ==========================================
    // TIMER LIVE ANPASSEN
    // ==========================================
    public void UpdateRefreshTime(int seconds)
    {
        // Sicherheits-Check: Niemals unter 1 Sekunde abfragen, 
        // um den Bluetooth-Adapter/Prozessor nicht zu überlasten!
        if (seconds < 1) seconds = 1;

        // Wenn sich die Zeit gar nicht geändert hat, machen wir nichts
        if (_timer.Interval.TotalSeconds == seconds) return;

        // Ansonsten: Neue Zeit setzen! (Der Timer übernimmt das beim nächsten Tick automatisch)
        _timer.Interval = TimeSpan.FromSeconds(seconds);
    }
}