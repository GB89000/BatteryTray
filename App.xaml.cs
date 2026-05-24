using BatteryStatusIndicator.Devices;
using System.Windows;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using WirelessBatteryMonitor.Devices;
using WirelessBatteryMonitor.Tray;

namespace WirelessBatteryMonitor;

public partial class App : System.Windows.Application
{
    // --- NEU: 1. Aus "private _trayIconManager" wird eine öffentliche Property ---
    public TrayIconManager? TrayManager { get; private set; }

    private CompositeBatteryService _batteryService;
    private bool _isInitialized = false;

    // Zwischenspeicher, damit wir die gefundenen Geräte beim Klick auf "Apply" wiederfinden
    private List<DeviceInfo> _availableDongles = new();
    private List<DeviceInfo> _foundBluetooth = new();

    public App()
    {
        InitializeComponent();
        this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        try
        {
            var settings = SettingsManager.Load();
            var mainWindow = new MainWindow();
            this.MainWindow = mainWindow;

            if (!settings.StartHidden) mainWindow.Show();

            // 1. Dongles definieren (Mit expliziter Zuweisung der Eigenschaften!)
            _availableDongles = new List<DeviceInfo>
            {
                new DeviceInfo { DeviceId = "redragon-m910", Name = "Redragon M910-KS", Manufacturer = "Redragon" },
                new DeviceInfo { DeviceId = "arctisnova7p", Name = "Arctis Nova 7P", Manufacturer = "SteelSeries" },
                new DeviceInfo { DeviceId = "arctisnova7", Name = "Arctis Nova 7", Manufacturer = "SteelSeries" },
                new DeviceInfo { DeviceId = "arctisnova7x", Name = "Arctis Nova 7X", Manufacturer = "SteelSeries" },
                new DeviceInfo { DeviceId = "arctisnova7gen2", Name = "Arctis Nova 7 Gen 2", Manufacturer = "SteelSeries" },
                new DeviceInfo { DeviceId = "voyager4320", Name = "Poly Voyager 4320", Manufacturer = "Poly" }
            };

            _batteryService = new CompositeBatteryService();
            _batteryService.RegisterProvider(new RedragonBatteryProvider());

            // ========================================================
            // 2. DIE NEUEN PROVIDER REGISTRIEREN
            // ========================================================

            // Wir erstellen einen "Arbeiter" NUR für das 7P (PlayStation)
            _batteryService.RegisterProvider(new SteelSeriesNova7Provider(
                deviceId: "arctisnova7p",
                name: "SteelSeries Arctis Nova 7P",
                productIds: new[] { 0x22A7 }
            ));

            // Wir erstellen einen ZWEITEN "Arbeiter" aus demselben Skript NUR für das normale 7er
            _batteryService.RegisterProvider(new SteelSeriesNova7Provider(
                deviceId: "arctisnova7",
                name: "SteelSeries Arctis Nova 7 (PC)",
                productIds: new[] { 0x22A1, 0x2202} // (Nach Januar-Frimware Update, Standard/PC)
            ));

            _batteryService.RegisterProvider(new SteelSeriesNova7Provider(
                deviceId: "arctisnova7x",
                name: "SteelSeries Arctis Nova 7X",
                productIds: new[] { 0x22A5 } 
            ));

            _batteryService.RegisterProvider(new SteelSeriesNova7Provider(
                deviceId: "arctisnova7gen2", 
                name: "SteelSeries Arctis Nova 7 Gen 2",
                productIds: new[] { 0x227E }
            ));

            _batteryService.RegisterProvider(new PolyVoyager4320Provider(
                deviceId: "voyager4320",
                name: "Poly Voyager 4320",
                productIds: new[] { 0x02E6 }
            ));

            _batteryService.RegisterProvider(new BluetoothBatteryProvider());

            // 2. Hier weisen wir den Manager der neuen Property zu
            TrayManager = new TrayIconManager(_batteryService, new List<DeviceInfo>());

            // 3. Icons basierend auf den aktuellen Settings direkt laden!
            RefreshTrayIcons();

            // 4. Bluetooth im Hintergrund scannen (sagt dem Fenster am Ende Bescheid)
            _ = Task.Run(async () => await ScanAndInitializeBluetoothAsync(mainWindow));
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Crash at startup: {ex.Message}");
        }
    }

    // ==========================================
    // WIRD VOM MAIN-WINDOW GERUFEN BEIM KLICK AUF "APPLY"
    // ==========================================
    public void RefreshTrayIcons()
    {
        var settings = SettingsManager.Load();
        var activeDevices = new List<DeviceInfo>();

        System.Diagnostics.Debug.WriteLine($"[APPLY] Loading Settings... Dongles found in Settings: {settings.SelectedDongles.Count}");

        foreach (var dongleName in settings.SelectedDongles)
        {
            var match = _availableDongles.FirstOrDefault(d => d.Name == dongleName);
            if (match != null)
            {
                activeDevices.Add(match);
                System.Diagnostics.Debug.WriteLine($"[APPLY] Dongle '{dongleName}' found and added to the active list..");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[APPLY] ERROR: Dongle '{dongleName}' could NOT be found in the internal list!");
            }
        }

        foreach (var btName in settings.SelectedBluetooth)
        {
            var match = _foundBluetooth.FirstOrDefault(d => d.Name == btName);
            if (match != null) activeDevices.Add(match);
        }

        // 1. Icons aktualisieren
        System.Diagnostics.Debug.WriteLine($"[APPLY] Sending a total of {activeDevices.Count} devices to the Tray Manager.");

        // 2. Hier verwenden wir ebenfalls die neue Property "TrayManager" ---
        TrayManager?.UpdateActiveDevices(activeDevices);

        // 3. Timer-Geschwindigkeit sofort anpassen!
        TrayManager?.UpdateRefreshTime(settings.RefreshTimeSeconds);
    }

    private async Task ScanAndInitializeBluetoothAsync(MainWindow mainWindow)
    {
        try
        {
            var processedNames = new HashSet<string>();

            // ---------------------------------------------------------
            // 1. Suche nach modernen BLE-Geräten (GATT)
            // ---------------------------------------------------------
            string bleSelector = GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.Battery);
            var bleDevices = await DeviceInformation.FindAllAsync(bleSelector);

            // ---------------------------------------------------------
            // 2. Suche nach gekoppelten Bluetooth Classic Geräten
            // ---------------------------------------------------------
            string classicSelector = global::Windows.Devices.Bluetooth.BluetoothDevice.GetDeviceSelectorFromPairingState(true);
            var classicDevices = await DeviceInformation.FindAllAsync(classicSelector);

            // Hilfsfunktion, um doppelten Code zu vermeiden
            void ProcessFoundDevice(DeviceInformation device)
            {
                if (string.IsNullOrEmpty(device.Name) || processedNames.Contains(device.Name)) return;
                if (device.Name.Contains("Arctis")) return; // Deine gewollte Arctis-Ausnahme

                processedNames.Add(device.Name);

                // Nur hinzufügen, wenn wir es nicht schon kennen
                if (!_foundBluetooth.Any(b => b.Name == device.Name))
                {
                    _foundBluetooth.Add(new DeviceInfo
                    {
                        Name = device.Name,
                        Manufacturer = "Bluetooth",
                        DeviceId = device.Id,
                        IsBluetooth = true
                    });
                    System.Diagnostics.Debug.WriteLine($"[BT-SCAN] Added to list: {device.Name} (ID: {device.Id})");
                }
            }

            // 1. BLE-Geräte in die Liste aufnehmen
            foreach (var device in bleDevices)
            {
                ProcessFoundDevice(device);
            }

            // 2. Classic-Geräte aufnehmen (JETZT OHNE DEN STRENGEN FILTER!)
            foreach (var device in classicDevices)
            {
                ProcessFoundDevice(device);
            }

            // Dem Fenster mitteilen, welche Geräte gefunden wurden
            var btNames = _foundBluetooth.Select(d => d.Name).ToList();
            var dongleNames = _availableDongles.Select(d => d.Name).ToList();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                mainWindow.SetAvailableDevices(btNames, dongleNames);

                // Wichtig: Falls ein Bluetooth-Gerät durch die Settings schon aktiv war, lad das Icon nach
                RefreshTrayIcons();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BT-ERROR]: {ex.Message}");
        }
    }

    public async Task AddBluetoothDevicesAsync(CompositeBatteryService service)
    {
        // Nach dem PC-Standby einfach den Scan wiederholen, um Geräte aufzuwecken
        if (this.MainWindow is MainWindow win)
        {
            await ScanAndInitializeBluetoothAsync(win);
        }
    }
}