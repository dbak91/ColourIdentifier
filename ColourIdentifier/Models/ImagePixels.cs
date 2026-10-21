namespace ColourIdentifier.Models;

public sealed class ImagePixels
{
    public int Width { get; }
    public int Height { get; }
    public int[] Argb { get; }

    public ImagePixels(int width, int height, int[] argb)
    {
        if (width <= 0 || height <= 0 || (long)width * height != argb.LongLength)
            throw new ArgumentException("Pixel data must match the image dimensions.");

        Width = width;
        Height = height;
        Argb = argb;
    }

    public RgbColour Average(PixelRegion region)
    {
        if (region.Left < 0 || region.Top < 0 || region.Right >= Width || region.Bottom >= Height ||
            region.Left > region.Right || region.Top > region.Bottom)
            throw new ArgumentOutOfRangeException(nameof(region));

        long red = 0, green = 0, blue = 0;
        for (int y = region.Top; y <= region.Bottom; y++)
        {
            for (int x = region.Left; x <= region.Right; x++)
            {
                uint pixel = (uint)Argb[y * Width + x];
                uint alpha = pixel >> 24;
                // Match the preview by compositing transparent pixels onto white.
                red += Composite((pixel >> 16) & 255, alpha);
                green += Composite((pixel >> 8) & 255, alpha);
                blue += Composite(pixel & 255, alpha);
            }
        }

        long count = region.PixelCount;
        return new RgbColour((byte)((red + count / 2) / count),
            (byte)((green + count / 2) / count), (byte)((blue + count / 2) / count));
    }

    private static uint Composite(uint channel, uint alpha) =>
        (channel * alpha + 255 * (255 - alpha) + 127) / 255;
}
