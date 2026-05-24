using WirelessBatteryMonitor.Devices;

namespace BatteryStatusIndicator.Devices;

public class CompositeBatteryService
{
    private readonly List<IDeviceBatteryProvider> _providers = new();

    public void RegisterProvider(IDeviceBatteryProvider provider)
    {
        _providers.Add(provider);
    }

    public async Task RefreshAllProvidersAsync()
    {
        foreach (var provider in _providers)
        {
            try
            {
                await provider.RefreshAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Provider Refresh failed: {ex.Message}");
            }
        }
    }

    public async Task<int?> GetBatteryLevelAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        foreach (var provider in _providers)
        {
            if (!provider.CanHandle(device))
                continue;

            var level = await provider.GetBatteryLevelAsync(device, cancellationToken);
            if (level.HasValue)
                return level;
        }

        return null;
    }
}
