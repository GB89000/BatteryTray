# BatteryTray – Benutzerhandbuch & Dokumentation

🌍 *[Click here for the English Documentation](#-english-documentation)*
🇩🇪 *[Klicke hier für die deutsche Dokumentation](#-deutsche-dokumentation)*

---

## 🇩🇪 Deutsche Dokumentation

### 1. Über BatteryTray
BatteryTray ist ein ressourcenschonendes, leichtgewichtiges Open-Source-Tool für Windows. Es ermöglicht dir, den Akkustand deiner kabellosen Headsets und Peripheriegeräte direkt unten in der Windows-Taskleiste (Tray-Bereich) abzulesen. Der große Vorteil: Du musst dafür nicht die schweren und oft ressourcenhungrigen Original-Treiber der Hersteller (wie Poly Lens oder SteelSeries GG) im Hintergrund laufen lassen. Zudem ist durch das Icon sofort ersichtlich, ob ein Gerät verbunden und eingeschaltet ist oder sich im Standby befindet (OFF-Status).

> ⚠️ **Hinweis zur Windows SmartScreen-Warnung**
> Wenn du das Setup startest, zeigt Windows möglicherweise eine blaue Warnung an ("Der Computer wurde durch Windows geschützt"). Dies passiert, da BatteryTray ein kostenloses Open-Source-Projekt ist und ich kein teures Entwickler-Zertifikat gekauft habe, um die .exe-Datei digital zu signieren.
> 
> **So kannst du die App trotzdem installieren:**
> Klicke in dem blauen Fenster auf **Weitere Informationen** und anschließend auf den Button **Trotzdem ausführen**.
> 
> Da der gesamte Quellcode hier auf GitHub transparent einsehbar ist, kannst du dir sicher sein, dass die App sicher ist. Alternativ kannst du dir den Code auch einfach selbst in Visual Studio kompilieren!

### 2. App
 <img width="399" height="195" alt="image" src="https://github.com/user-attachments/assets/d2593651-098f-4ffa-a121-524e9aed0071" />
 <img width="783" height="587" alt="image" src="https://github.com/user-attachments/assets/560f596b-dc00-4852-8c90-57d96be86dca" />
 <img width="409" height="368" alt="image" src="https://github.com/user-attachments/assets/2589c6d6-af6e-450f-ae7d-8784c51bcec3" />

### 3. Systemanforderungen
* **Betriebssystem:** Getestet und optimiert für Windows 10 (kompatibel mit Windows 11).
* **Voraussetzung:** Aktuelle .NET Desktop Runtime (wird normalerweise von Windows automatisch verwaltet).
* **Hinweis zu Software-Konflikten:** Die Original-Software des Herstellers (z. B. Poly Lens) sollte idealerweise vollständig geschlossen sein. Laufen beide Programme gleichzeitig, kann es zu Konflikten beim Auslesen der USB-Daten kommen.

### 4. Unterstützte Geräte & Firmware
Grundsätzlich sollten alle Bluetooth-Geräte funktionieren, die ihren Akkustand auch in den Windows-Einstellungen unter „Bluetooth und andere Geräte“ anzeigen. 
Bei Geräten mit USB-Dongles liest das Tool die Hardware-Sensoren direkt aus. Die Software muss ggf. einmal installiert und gestartet werden, damit der passende Treiber eingerichtet wird. Danach kann sie dauerhaft geschlossen bleiben.

Bisher wurden folgende Geräte erfolgreich auf ihre Genauigkeit getestet:
* **Redragon** M910-KS (Firmware: 1.0)
* **SteelSeries Arctis** Nova 7P (Firmware: 2.31)
* **SteelSeries Arctis** Nova 7 (Firmware: 2.31)
* **Poly** Voyager 4320 mit BT700 Dongle (Firmware Headset: 0.0.2134.3260 | Dongle: 0.0.1415.5115)

> **Hinweis:** Bei zukünftigen Firmware-Updates der Hersteller kann sich das USB-Protokoll ändern. Wir bemühen uns, BatteryTray bei solchen Änderungen zeitnah anzupassen.

### 5. Bedienung & Einstellungen
Sobald BatteryTray gestartet ist, nistet es sich unten rechts in der Taskleiste ein.
* **Akkustand:** Ein kurzer Blick auf das Icon zeigt dir sofort den aktuellen Prozentwert.
* **OFF-Status:** Wird „OFF“ angezeigt, ist das Gerät ausgeschaltet, nicht verbunden, im Ruhezustand oder nicht kompatibel.
* **Einstellungen:** Ein Rechts- oder Doppelklick auf das Icon öffnet das Menü. Hier kannst du unter anderem aktivieren, dass das Tool beim Windows-Start automatisch im Hintergrund geladen wird.
* **Info-Panel:** Über das „i“-Symbol oben rechts im Menü erreichst du unsere Links zu GitHub, Support und diesem Handbuch.
* **Speicherort der Einstellungen:** `%LOCALAPPDATA%\BatteryTray` (entspricht `C:\Users\<YourUsername>\AppData\Local\BatteryTray`)

### 6. FAQ & Fehlerbehebung (Troubleshooting)

**Frage: Mein Gerät ist eingeschaltet, aber das Icon zeigt "OFF" oder aktualisiert sich nicht.**
**Antwort:** Einige Geräte (wie z. B. die Redragon Maus) funken ihren Akkustand aus Energiespargründen nicht ununterbrochen. Um die Maus "aufzuwecken" und das Tool zur Aktualisierung zu zwingen, reicht es, eine beliebige Taste am Gerät zu drücken. Alternativ schaltet das Icon automatisch auf OFF, wenn das Headset oder die Maus länger als `3* Refresh Time` keine Daten mehr sendet oder physisch ausgeschaltet wurde.

**Frage: Warum zeigt Poly Lens z. B. 100 % an, aber BatteryTray zeigt etwas weniger (z. B. 97 %)?**
**Antwort:** BatteryTray liest den echten, ungeschönten physikalischen Hardware-Sensor der Batterie aus. Original-Software "rundet" die Werte oft künstlich auf 100 % auf, um den Nutzer nicht zu irritieren – auch wenn die Batterie aus chemischen Schutzgründen nie wirklich randvoll geladen wird. Wir haben jedoch eine Skalierung (Virtual Top End) integriert, um das Verhalten der Original-Software so gut wie möglich zu simulieren.

### 7. Community: Ein neues Gerät hinzufügen
Dein Gerät wird noch nicht unterstützt? Da BatteryTray Open Source ist, bist du herzlich eingeladen, mitzuhelfen! Du kannst den Code für neue Hardware selbst beisteuern – zum Beispiel ganz einfach mit Unterstützung einer KI wie Gemini.

> **Hinweis für Entwickler:** Einige Dongle-Geräte lassen sich leider nur schwer integrieren, da ihre Herstellerprotokolle den Akkustand nicht zuverlässig oder offen mitteilen (z. B. HyperX Cloud II Headset mit dem Kingston Dongle, Baujahr 2021).

### 8. Lizenz & Feedback
**Lizenz**
BatteryTray ist ein Open-Source-Projekt und wird unter der **GNU General Public License v3.0 (GPLv3)** bereitgestellt. Das bedeutet, du kannst die Software frei nutzen, verändern und weiterverbreiten, solange alle Modifikationen unter derselben Lizenz ebenfalls Open Source bleiben. Weitere Details zur Lizenzierung findest du in der `LICENSE`-Datei hier im Repository.

**Fehler melden & Wünsche äußern**
Du hast einen Bug gefunden oder eine tolle Idee für ein neues Feature? Am besten erstellst du dafür direkt hier auf GitHub ein neues **Issue** im "Issues"-Tab. Alternativ kannst du mir auch gerne Feedback oder Fragen über **Instagram** zukommen lassen (Links dazu findest du im Info-Panel der App).

---

## 🇬🇧 English Documentation

### 1. About BatteryTray
BatteryTray is a resource-friendly, lightweight open-source tool for Windows. It allows you to check the battery level of your wireless headsets and peripherals directly in the Windows taskbar (tray area). The main advantage: You don't have to run the heavy and often resource-hungry original manufacturer drivers (like Poly Lens or SteelSeries GG) in the background. Additionally, a quick glance at the icon instantly shows whether a device is connected and turned on or in standby (OFF status).

> ⚠️ **Note on the Windows SmartScreen Warning**
> When you launch the setup, Windows might display a blue warning screen ("Windows protected your PC"). This happens because BatteryTray is a free open-source project and I haven't purchased an expensive developer certificate to digitally sign the .exe file.
> 
> **Here is how you can install the app anyway:**
> Click on **More info** in the blue window, and then click the **Run anyway** button.
> 
> Since the entire source code is transparently available here on GitHub, you can rest assured that the app is safe. Alternatively, you can simply compile the code yourself in Visual Studio!

### 2. App
 <img width="399" height="195" alt="image" src="https://github.com/user-attachments/assets/d2593651-098f-4ffa-a121-524e9aed0071" />
 <img width="783" height="587" alt="image" src="https://github.com/user-attachments/assets/560f596b-dc00-4852-8c90-57d96be86dca" />
 <img width="409" height="368" alt="image" src="https://github.com/user-attachments/assets/2589c6d6-af6e-450f-ae7d-8784c51bcec3" />

### 3. System Requirements
* **Operating System:** Tested and optimized for Windows 10 (compatible with Windows 11).
* **Prerequisites:** Current .NET Desktop Runtime (usually managed automatically by Windows).
* **Note on Software Conflicts:** The manufacturer's original software (e.g., Poly Lens) should ideally be completely closed. If both programs run simultaneously, conflicts may occur when reading the USB data.

### 4. Supported Devices & Firmware
Generally, all Bluetooth devices that display their battery level in the Windows settings under 'Bluetooth & other devices' should work. For devices with USB dongles, the tool reads the hardware sensors directly. The manufacturer's software may need to be installed and launched once to set up the appropriate driver. Afterward, it can remain permanently closed.

So far, the following devices have been successfully tested for accuracy:
* **Redragon** M910-KS (Firmware: 1.0)
* **SteelSeries Arctis** Nova 7P (Firmware: 2.31)
* **SteelSeries Arctis** Nova 7 (Firmware: 2.31)
* **Poly** Voyager 4320 with BT700 Dongle (Firmware Headset: 0.0.2134.3260 | Dongle: 0.0.1415.5115)

> **Note:** Manufacturer firmware updates may change the USB protocol in the future. We strive to update BatteryTray promptly if such changes occur.

### 5. Usage & Settings
Once BatteryTray is started, it nests in the bottom right corner of the taskbar.
* **Battery Level:** A quick look at the icon immediately shows you the current percentage.
* **OFF Status:** If "OFF" is displayed, the device is turned off, disconnected, in sleep mode, or not compatible.
* **Settings:** A right-click or double-click on the icon opens the menu. Here you can, among other things, enable the tool to launch automatically in the background on Windows startup.
* **Info Panel:** Clicking the "i" icon in the top right corner of the menu will take you to our GitHub page, support links, and this manual.
* **Settings Location:** `%LOCALAPPDATA%\BatteryTray` (corresponds to `C:\Users\<YourUsername>\AppData\Local\BatteryTray`)

### 6. FAQ & Troubleshooting

**Question: My device is turned on, but the icon shows "OFF" or does not update.**
**Answer:** Some devices (like the Redragon mouse) do not continuously broadcast their battery level to save power. To "wake up" the mouse and force the tool to update, simply press any button on the device. Alternatively, the icon automatically switches to OFF if the headset or mouse stops sending data for more than `3* Refresh Time` or has been physically turned off.

**Question: Why does Poly Lens show 100%, but BatteryTray shows slightly less (e.g., 97%)?**
**Answer:** BatteryTray reads the real, unvarnished physical hardware sensor of the battery. Original software often artificially "rounds up" the values to 100% to avoid confusing the user – even though the battery is never truly fully charged for chemical protection reasons. However, we have integrated a scaling feature (Virtual Top End) to simulate the behavior of the original software as closely as possible.

### 7. Community: Adding a New Device
Is your device not supported yet? Since BatteryTray is open source, you are highly encouraged to contribute! You can submit the code for new hardware yourself – for example, with the easy help of an AI like Gemini.

> **Note for Developers:** Some dongle devices are unfortunately difficult to integrate because their manufacturer protocols do not reliably or openly share the battery level (e.g., HyperX Cloud II Headset with the Kingston Dongle, built in 2021).

### 8. License & Bug Reporting
**License**
BatteryTray is an open-source project released under the **GNU General Public License v3.0 (GPLv3)**. This means you are free to use, modify, and redistribute the software, provided that all modifications remain open source under the same license. For more details on the licensing terms, please refer to the `LICENSE` file in this repository.

**Bug Reporting & Feature Requests**
Found a bug or have a great idea for a new feature? The best way to let me know is by opening a new **Issue** right here in the "Issues" tab on GitHub. Alternatively, feel free to drop me your feedback or questions via **Instagram** (you can find the links in the app's Info Panel).

