using HidSharp;

namespace WirelessBatteryMonitor.Devices;

public class PolyVoyager4320Provider : IDeviceBatteryProvider
{
    private readonly string _deviceId;
    private readonly int[] _productIds;
    public string Name { get; }

    private const int VendorId = 0x047F;
    private int? _lastBatteryLevel = null;
    private bool _isListening = false;

    public PolyVoyager4320Provider(string deviceId, string name, int[] productIds)
    {
        _deviceId = deviceId;
        Name = name;
        _productIds = productIds;
    }

    public bool CanHandle(DeviceInfo device) => device.DeviceId == _deviceId;

    public async Task<int?> GetBatteryLevelAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        if (!_isListening)
        {
            _isListening = true;
            Thread listenerThread = new Thread(StartListeningLoop)
            {
                IsBackground = true,
                Name = "PolyVoyagerListener"
            };
            listenerThread.Start();
        }

        return _lastBatteryLevel;
    }

    private void StartListeningLoop()
    {
        byte[] batteryPing = new byte[] { 0x07, 0x01, 0x01, 0x10, 0x06, 0x20, 0x00, 0x00, 0x02, 0x0A, 0x1A };
        var initialSettings = SettingsManager.Load();
        int currentRefreshInterval = initialSettings.RefreshTimeSeconds < 1 ? 1 : initialSettings.RefreshTimeSeconds;

        while (true)
        {
            var activeStreams = new System.Collections.Generic.List<HidStream>();

            try
            {
                var hidDevices = DeviceList.Local.GetHidDevices(VendorId)
                                         .Where(d => _productIds.Contains(d.ProductID) && d.GetMaxInputReportLength() >= 16)
                                         .ToList();

                foreach (var device in hidDevices)
                {
                    try
                    {
                        var stream = device.Open();
                        stream.ReadTimeout = 10;
                        activeStreams.Add(stream);
                    }
                    catch { } // Zugriff verweigert ignorieren
                }

                if (activeStreams.Count == 0)
                {
                    Thread.Sleep(2000);
                    continue;
                }

                // Großer Puffer für maximale Stabilität!
                byte[] buffer = new byte[1024];
                DateTime lastPingTime = DateTime.MinValue;
                DateTime lastValidResponse = DateTime.Now;

                while (true)
                {
                    // ==========================================
                    // 1. DER DYNAMISCHE PINGER
                    // ==========================================
                    if ((DateTime.Now - lastPingTime).TotalSeconds >= currentRefreshInterval)
                    {
                        lastPingTime = DateTime.Now;
                        var settings = SettingsManager.Load();
                        currentRefreshInterval = settings.RefreshTimeSeconds < 1 ? 1 : settings.RefreshTimeSeconds;

                        foreach (var stream in activeStreams)
                        {
                            try
                            {
                                int outLen = stream.Device.GetMaxOutputReportLength();
                                if (outLen >= batteryPing.Length)
                                {
                                    byte[] outBuffer = new byte[outLen];
                                    Array.Copy(batteryPing, outBuffer, batteryPing.Length);
                                    stream.Write(outBuffer);
                                }
                                else
                                {
                                    int featLen = stream.Device.GetMaxFeatureReportLength();
                                    if (featLen >= batteryPing.Length)
                                    {
                                        byte[] featBuffer = new byte[featLen];
                                        Array.Copy(batteryPing, featBuffer, batteryPing.Length);
                                        stream.SetFeature(featBuffer);
                                    }
                                }
                            }
                            catch { } // Fehler auf bestimmten Kanälen ignorieren
                        }
                    }

                    // ==========================================
                    // 2. DAS RADAR & DIE POLY-GLÄTTUNG
                    // ==========================================
                    foreach (var stream in activeStreams)
                    {
                        try
                        {
                            int readLength = stream.Device.GetMaxInputReportLength();
                            if (readLength > buffer.Length) readLength = buffer.Length;

                            int bytesRead = stream.Read(buffer, 0, readLength);

                            if (bytesRead >= 16 && buffer[0] == 0x07 && buffer[8] == 0x03 && buffer[9] == 0x0A && buffer[10] == 0x1A)
                            {
                                lastValidResponse = DateTime.Now;

                                int rawValue = (buffer[14] << 8) | buffer[15];
                                double preciseSensorBattery = rawValue / 10.0;

                                double maxSensorValue = 90.0;
                                double scaledBattery = (preciseSensorBattery / maxSensorValue) * 100.0;
                                int displayBattery = (int)Math.Round(scaledBattery);

                                if (displayBattery > 100) displayBattery = 100;

                                _lastBatteryLevel = displayBattery;
                            }
                        }
                        catch (TimeoutException) { }
                        catch (Exception) { throw new Exception("Device disconnected"); }
                    }

                    // ==========================================
                    // 3. DER DYNAMISCHE WATCHDOG
                    // ==========================================
                    if ((DateTime.Now - lastValidResponse).TotalSeconds > (currentRefreshInterval * 3))
                    {
                        if (_lastBatteryLevel != null)
                        {
                            _lastBatteryLevel = null;
                            System.Diagnostics.Debug.WriteLine($"[POLY] Connection timeout -> Set icon to OFF");
                        }
                    }

                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[POLY] Connection lost: {ex.Message}");
                _lastBatteryLevel = null;
            }
            finally
            {
                foreach (var stream in activeStreams)
                {
                    try { stream.Dispose(); } catch { }
                }
            }

            Thread.Sleep(currentRefreshInterval * 1000);
        }
    }

    public Task RefreshAsync() => Task.CompletedTask;
}