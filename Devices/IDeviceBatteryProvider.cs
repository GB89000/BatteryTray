namespace WirelessBatteryMonitor.Devices;

public interface IDeviceBatteryProvider
{
    string Name { get; }

    bool CanHandle(DeviceInfo device);
    Task<int?> GetBatteryLevelAsync(DeviceInfo device, CancellationToken ct);

    Task RefreshAsync();
}
