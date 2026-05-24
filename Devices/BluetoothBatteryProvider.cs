using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using WirelessBatteryMonitor.Devices;

namespace BatteryStatusIndicator.Devices
{
    public class BluetoothBatteryProvider : IDeviceBatteryProvider
    {
        public string Name => "Bluetooth Battery Provider";

        public bool CanHandle(DeviceInfo device)
        {
            return device != null && device.IsBluetooth;
        }

        public async Task<int?> GetBatteryLevelAsync(DeviceInfo device, CancellationToken cancellationToken)
        {
            System.Diagnostics.Debug.WriteLine($"[BT-DEBUG] GetBatteryLevel was called for: {device.Name}");

            if (device == null || string.IsNullOrEmpty(device.DeviceId))
                return null;

            // 1. Versuch: Über BLE GATT auslesen (Maus etc.)
            try
            {
                using var bluetoothDevice = await BluetoothLEDevice.FromIdAsync(device.DeviceId).AsTask(cancellationToken);

                if (bluetoothDevice != null && bluetoothDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
                {
                    var batteryServices = await bluetoothDevice.GetGattServicesForUuidAsync(GattServiceUuids.Battery);
                    if (batteryServices.Status == GattCommunicationStatus.Success && batteryServices.Services.Count > 0)
                    {
                        using var batteryService = batteryServices.Services[0];
                        var characteristics = await batteryService.GetCharacteristicsForUuidAsync(GattCharacteristicUuids.BatteryLevel);

                        if (characteristics.Status == GattCommunicationStatus.Success && characteristics.Characteristics.Count > 0)
                        {
                            var batteryLevelCharacteristic = characteristics.Characteristics[0];
                            var result = await batteryLevelCharacteristic.ReadValueAsync();

                            if (result.Status == GattCommunicationStatus.Success)
                            {
                                var reader = DataReader.FromBuffer(result.Value);
                                byte level = reader.ReadByte();
                                System.Diagnostics.Debug.WriteLine($"[BT-BATTERY] {device.Name} read via BLE: {level}%");
                                return (int)level;
                            }
                        }
                    }
                }
            }
            catch (ArgumentException)
            {
                System.Diagnostics.Debug.WriteLine($"[BT-BATTERY] {device.Name} ist kein BLE-Gerät (ArgumentException).");
            }
            catch (OperationCanceledException) { return null; }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BT-BATTERY-ERROR] BLE path failed for {device.Name}: {ex.Message}");
            }

            // ====================================================================
            // 2. Check, ob das Classic-Gerät überhaupt AN und VERBUNDEN ist
            // ====================================================================
            try
            {
                // Wir nutzen hier BluetoothDevice (Classic) statt BluetoothLEDevice
                using var classicBtDevice = await global::Windows.Devices.Bluetooth.BluetoothDevice.FromIdAsync(device.DeviceId).AsTask(cancellationToken);

                if (classicBtDevice != null && classicBtDevice.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
                {
                    System.Diagnostics.Debug.WriteLine($"[BT-BATTERY] {device.Name} ist offline (Classic ConnectionStatus = Disconnected). Sende OFF.");
                    return null; // Beendet die Abfrage sofort -> TrayIconManager zeigt OFF
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BT-BATTERY-CONNECTION-CHECK] Failed for {device.Name}: {ex.Message}");
            }

            // 3. Versuch (Fallback): Bruteforce über die physische MAC-Adresse
            System.Diagnostics.Debug.WriteLine($"[BT-BATTERY] Gerät ist online. Falling back to MAC Address hunt for {device.Name}...");
            return await GetBatteryLevelFromMacAddressAsync(device.DeviceId);
        }

        /// <summary>
        /// Extrahiert die MAC-Adresse und zwingt Windows, alle echten Hardware-Knoten (DevNodes)
        /// mit dieser MAC-Adresse offenzulegen. Findet auch entkoppelte HFP-Knoten zuverlässig.
        /// </summary>
        private async Task<int?> GetBatteryLevelFromMacAddressAsync(string deviceId)
        {
            try
            {
                // Die ID sieht so aus: Bluetooth#Bluetooth11:22:33:44:55:66-AA:BB:CC:DD:EE:FF
                // Wir brauchen den Teil nach dem Bindestrich (die Remote MAC des Headsets).
                if (!deviceId.Contains("-"))
                {
                    System.Diagnostics.Debug.WriteLine("[BT-BATTERY] Invalid DeviceID format. Cannot extract MAC.");
                    return null;
                }

                string remoteMac = deviceId.Substring(deviceId.LastIndexOf('-') + 1);
                // Windows Hardware-IDs (BTHENUM) nutzen keine Doppelpunkte
                string cleanMac = remoteMac.Replace(":", "").ToUpper();

                System.Diagnostics.Debug.WriteLine($"[BT-BATTERY] Hunting for MAC: {cleanMac}...");

                string batteryPropertyKey = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";
                string batteryLifeKey = "System.Devices.BatteryLife";
                string[] requestedProps = { batteryPropertyKey, batteryLifeKey };

                // AQS-Filter: "~~" bedeutet "enthält". 
                // Sucht im gesamten PC nach Hardware, deren Instanz-ID diese MAC beinhaltet.
                string aqsFilter = $"System.Devices.DeviceInstanceId:~~\"{cleanMac}\"";

                // Blitzschnelle Suche direkt im Hardware-Baum
                var hardwareNodes = await DeviceInformation.FindAllAsync(
                    aqsFilter,
                    requestedProps,
                    DeviceInformationKind.Device
                );

                System.Diagnostics.Debug.WriteLine($"[BT-BATTERY] Found {hardwareNodes.Count} associated hardware nodes for MAC {cleanMac}.");

                foreach (var node in hardwareNodes)
                {
                    if (node.Properties.TryGetValue(batteryPropertyKey, out var bat1) && bat1 != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[BT-BATTERY] BINGO! Found on hidden node '{node.Name}': {bat1}%");
                        return Convert.ToInt32(bat1);
                    }
                    if (node.Properties.TryGetValue(batteryLifeKey, out var bat2) && bat2 != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[BT-BATTERY] BINGO! Found on hidden node '{node.Name}' (System): {bat2}%");
                        return Convert.ToInt32(bat2);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[BT-BATTERY] Still no battery found on any node matching MAC {cleanMac}.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BT-BATTERY-MAC-ERROR]: {ex.Message}");
            }

            return null;
        }

        public Task RefreshAsync()
        {
            return Task.CompletedTask;
        }
    }
}