using System.Globalization;

namespace ColourIdentifier.Models;

public readonly record struct RgbColour(byte R, byte G, byte B)
{
    public string Hex => $"#{R:X2}{G:X2}{B:X2}";
    public string CssRgb => string.Create(CultureInfo.InvariantCulture, $"rgb({R}, {G}, {B})");
}
