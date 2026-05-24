namespace WirelessBatteryMonitor.Devices;

public class DeviceInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Manufacturer { get; set; }
    public string DeviceId { get; set; } // Die WinRT/Bluetooth ID
    public bool IsBluetooth { get; set; }

    // Standard-Konstruktor für Objekt-Initialisierer
    public DeviceInfo() { }
}


