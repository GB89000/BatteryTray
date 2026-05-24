using HidSharp;

namespace WirelessBatteryMonitor.Devices;

public class RedragonBatteryProvider : IDeviceBatteryProvider
{
    public string Name => "Redragon Wireless Battery Provider";

    private const int VendorId = 0x258A;
    private const int ProductId = 0x002F;

    public bool CanHandle(DeviceInfo device)
    {
        return device.Manufacturer.Contains("Redragon", StringComparison.OrdinalIgnoreCase);
    }

    public async Task RefreshAsync()
    {
        await Task.Run(() =>
        {
            var forceReEnumeration = DeviceList.Local.GetHidDevices().ToList();
        });
    }

    public async Task<int?> GetBatteryLevelAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var hidDevices = DeviceList.Local.GetHidDevices(VendorId, ProductId);

        foreach (var hidDevice in hidDevices)
        {
            try
            {
                int maxLen = hidDevice.GetMaxFeatureReportLength();
                if (maxLen < 4) continue;

                using var stream = hidDevice.Open();

                // Kurze Timeouts, damit die App nicht hängt
                stream.ReadTimeout = 500;
                stream.WriteTimeout = 500;

                // 1. WAKE-UP BEFEHL SENDEN (Ersetzt die Redragon-Software!)
                byte[] pingCommand = new byte[maxLen];
                pingCommand[0] = 0x05;
                pingCommand[1] = 0x90;
                try { stream.SetFeature(pingCommand); } catch { }

                // Kurze Pause
                await Task.Delay(50, cancellationToken);

                // 2. AKKU AUSLESEN (Aus dem Cache des Dongles oder frisch von der Maus)
                byte[] feature = new byte[maxLen];
                feature[0] = 0x05;
                stream.GetFeature(feature);

                if (feature.Length >= 4)
                {
                    int battery = feature[3];

                    if (battery > 0 && battery <= 100)
                    {
                        // Maus liefert einen gültigen Wert
                        return battery;
                    }
                }
            }
            catch
            {
                // Wenn ein Interface sich nicht öffnen lässt, ignorieren wir es
                continue;
            }
        }

        // Fällt nur auf 'null' zurück, wenn der Dongle physisch abgezogen wird
        return null;
    }
}