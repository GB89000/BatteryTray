namespace WirelessBatteryMonitor.Tray;

public static class BatteryIconGenerator
{
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    public static extern bool DestroyIcon(IntPtr handle);

    public static Icon CreateBatteryIcon(int percentage, AppSettings settings)
    {
        int size = 32;
        using var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);

        g.TextRenderingHint = System.Drawing.TextRenderingHint.SingleBitPerPixelGridFit;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

        g.Clear(Color.Transparent);

        // ==========================================
        // NEU: Hintergrundfarbe anwenden 
        // ==========================================
        if (!settings.IsBackgroundTransparent)
        {
            Color customBgColor = ColorTranslator.FromHtml(settings.BackgroundColorHex);
            using (var bgBrush = new SolidBrush(customBgColor))
            {
                g.FillRectangle(bgBrush, 0, 0, size, size);
            }
        }
        // ==========================================

        // Die dynamische Farbe wird NICHT mehr für den Akku genutzt, sondern später für den Text
        Color finalTextColor;
        if (settings.UseDynamicTextColor)
        {
            // Ampel-Farben verwenden
            finalTextColor = percentage > 50 ? Color.LimeGreen :
                             percentage > 20 ? Color.Gold :
                                               Color.Red;
        }
        else
        {
            // Die feste Wunschfarbe des Users verwenden (Farbe vom Offline-Icon)
            finalTextColor = ColorTranslator.FromHtml(settings.TextColorHex);
        }

        // Wenn es 100% sind, nutzen wir Größe 24, ansonsten fette 30
        float fontSize = percentage == 100 ? 24f : 30f;

        using var font = new Font("Arial Narrow", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);

        string text = percentage.ToString();
        var textSize = g.MeasureString(text, font);

        float textX = (size - textSize.Width) / 2;
        float textY = (size - textSize.Height) / 2;

        // WICHTIG: Die schwarze Umrandung wieder aktiviert! 
        // Ohne diese Umrandung wäre grüne Schrift auf weißem Akku unlesbar.
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x != 0 || y != 0)
                    g.DrawString(text, font, Brushes.Black, textX + x, textY + y);
            }
        }

        // 3. Textfarbe anwenden (Dynamisch: Grün/Gelb/Rot)
        using (var textBrush = new SolidBrush(finalTextColor))
        {
            g.DrawString(text, font, textBrush, textX, textY);
        }

        IntPtr hIcon = bmp.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    public static Icon CreateOfflineIcon(AppSettings settings)
    {
        int size = 32;
        using var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);

        g.TextRenderingHint = System.Drawing.TextRenderingHint.SingleBitPerPixelGridFit;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

        g.Clear(Color.Transparent);

        if (!settings.IsBackgroundTransparent)
        {
            Color customBgColor = ColorTranslator.FromHtml(settings.BackgroundColorHex);
            using (var bgBrush = new SolidBrush(customBgColor))
            {
                g.FillRectangle(bgBrush, 0, 0, size, size);
            }
        }

        using var font = new Font("Arial Narrow", 20, FontStyle.Bold, GraphicsUnit.Pixel);
        string text = "OFF";
        var textSize = g.MeasureString(text, font);
        float textX = (size - textSize.Width) / 2;
        float textY = (size - textSize.Height) / 2;

        // Auch bei "OFF" sorgt die Umrandung für einen sauberen Look
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x != 0 || y != 0)
                    g.DrawString(text, font, Brushes.Black, textX + x, textY + y);
            }
        }

        // Bei OFF nutzen wir exakt die Farbe, die der User in den Settings eingestellt hat
        Color customTextColor = ColorTranslator.FromHtml(settings.TextColorHex);
        using (var textBrush = new SolidBrush(customTextColor))
        {
            g.DrawString(text, font, textBrush, textX, textY);
        }

        IntPtr hIcon = bmp.GetHicon();
        return Icon.FromHandle(hIcon);
    }
}