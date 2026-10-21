namespace ColourIdentifier.Models;

public readonly record struct PixelPoint(int X, int Y);

// Bounds are inclusive: a tap is a 1 × 1 region.
public readonly record struct PixelRegion(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left + 1;
    public int Height => Bottom - Top + 1;
    public long PixelCount => (long)Width * Height;

    public static PixelRegion Between(PixelPoint start, PixelPoint end) => new(
        Math.Min(start.X, end.X), Math.Min(start.Y, end.Y),
        Math.Max(start.X, end.X), Math.Max(start.Y, end.Y));
}
