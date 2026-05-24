using HidSharp;

namespace WirelessBatteryMonitor.Devices;

public class SteelSeriesNova7Provider : IDeviceBatteryProvider
{
    private readonly string _deviceId;
    private readonly int[] _productIds;

    public string Name { get; }

    private const int VendorId = 0x1038;

    // NEU: Der Konstruktor! Hier sagen wir dem Skript, für wen es arbeitet.
    public SteelSeriesNova7Provider(string deviceId, string name, int[] productIds)
    {
        _deviceId = deviceId;
        Name = name;
        _productIds = productIds;
    }

    public bool CanHandle(DeviceInfo device)
    {
        // GANZ WICHTIG: Er reagiert jetzt nicht mehr auf "SteelSeries" generell,
        // sondern NUR NOCH, wenn die exakte ID (z.B. "arctisnova7p") angefragt wird!
        return device.DeviceId == _deviceId;
    }

    public async Task<int?> GetBatteryLevelAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        // Sucht nur nach den Product-IDs, die wir ihm beim Start mitgegeben haben
        var hidDevices = DeviceList.Local.GetHidDevices(VendorId)
                                 .Where(d => _productIds.Contains(d.ProductID))
                                 .ToList();

        foreach (var hidDevice in hidDevices)
        {
            if (!hidDevice.DevicePath.Contains("mi_03", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                using var stream = hidDevice.Open();
                stream.ReadTimeout = 1000;

                byte[] report = new byte[hidDevice.GetMaxOutputReportLength()];
                if (report.Length < 2) continue;

                report[0] = 0x00;
                report[1] = 0xB0;

                stream.Write(report);
                await Task.Delay(100, cancellationToken);

                byte[] response = new byte[hidDevice.GetMaxInputReportLength()];
                int bytesRead = stream.Read(response, 0, response.Length);

                if (bytesRead > 3 && response[1] == 0xB0)
                {
                    byte statusByte = response[2];

                    if (statusByte != 0x03) return null;

                    int battery = response[3];
                    if (battery >= 0 && battery <= 100) return battery;
                }
            }
            catch (TimeoutException)
            {
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SteelSeries Error on mi_03: {ex.Message}");
            }
        }

        return null;
    }

    public Task RefreshAsync() => Task.CompletedTask;
}