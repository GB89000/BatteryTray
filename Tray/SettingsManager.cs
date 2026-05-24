using System.IO;
using System.Text.Json;

namespace WirelessBatteryMonitor
{
    // Diese Klasse spiegelt deine Einstellungen wider
    public class AppSettings
    {
        public bool StartHidden { get; set; } = false;
        public int RefreshTimeSeconds { get; set; } = 3;

        // --- Farbeinstellungen ---
        public string TextColorHex { get; set; } = "#FFFFFF";       // Standard: Weiß
        public string BackgroundColorHex { get; set; } = "#000000"; // Standard: Schwarz
        public bool IsBackgroundTransparent { get; set; } = true;   // Standard: Transparent
        public bool UseDynamicTextColor { get; set; } = true; // Standardmäßig eingeschaltet

        // Hier merken wir uns die Namen der ausgewählten Geräte
        public List<string> SelectedBluetooth { get; set; } = new();
        public List<string> SelectedDongles { get; set; } = new();
    }

    // Dieser Manager speichert die Einstellungen automatisch in deinem AppData-Ordner
    public static class SettingsManager
    {
        // Nutzt jetzt LocalApplicationData -> Pfad: C:\Users\[DeinName]\AppData\Local\BatteryTray\settings.json
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BatteryTray", "settings.json");

        public static AppSettings Load()
        {
            if (File.Exists(FilePath))
            {
                try
                {
                    string json = File.ReadAllText(FilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsManager] Load-Error: {ex.Message}");
                    return new AppSettings();
                }
            }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                // Stellt sicher, dass der Ordner "BatteryTray" in AppData existiert, bevor die Datei geschrieben wird!
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

                // Schreibt die Datei mit schönen Zeilenumbrüchen (WriteIndented = true)
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsManager] Save-Error: {ex.Message}");
            }
        }
    }
}