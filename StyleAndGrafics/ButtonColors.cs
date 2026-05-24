using System.Windows.Media;

namespace WirelessBatteryMonitor.StyleAndGrafics
{
    public class ButtonColors
    {
        // ---------------------------------------------------------
        // BUTTON COLORS
        // ---------------------------------------------------------
        public static RadialGradientBrush ButtonGreen() => new RadialGradientBrush
        {
            GradientOrigin = new System.Windows.Point(0.5, 0.5),
            Center = new System.Windows.Point(0.5, 0.5),
            RadiusX = 0.7,
            RadiusY = 0.7,
            GradientStops =
            {
                new GradientStop(System.Windows.Media.Color.FromArgb(225, 0, 255, 0), 0.0),
                new GradientStop(System.Windows.Media.Color.FromArgb(225, 0, 225, 0), 0.2),
                new GradientStop(System.Windows.Media.Color.FromArgb(225, 0, 175, 0), 0.5),
                new GradientStop(System.Windows.Media.Color.FromArgb(225, 0, 125, 0), 0.75),
                new GradientStop(System.Windows.Media.Colors.DimGray, 1.0)
            }
        };

        public static RadialGradientBrush ButtonRed() => new RadialGradientBrush
        {
            GradientOrigin = new System.Windows.Point(0.5, 0.5),
            Center = new System.Windows.Point(0.5, 0.5),
            RadiusX = 0.8,
            RadiusY = 0.7,
            GradientStops =
            {
                new GradientStop(System.Windows.Media.Color.FromArgb(225, 255, 0, 0), 0.0),
                new GradientStop(System.Windows.Media.Color.FromArgb(225, 225, 0, 0), 0.2),
                new GradientStop(System.Windows.Media.Color.FromArgb(225, 175, 0, 0), 0.5),
                new GradientStop(System.Windows.Media.Color.FromArgb(225, 125, 0, 0), 0.75),
                new GradientStop(System.Windows.Media.Colors.DimGray, 1.0)
            }
        };
    }
}
