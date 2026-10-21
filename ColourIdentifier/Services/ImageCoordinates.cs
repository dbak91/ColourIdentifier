using ColourIdentifier.Models;

namespace ColourIdentifier.Services;

public readonly record struct ImageBounds(double Left, double Top, double Width, double Height);

public static class ImageCoordinates
{
    public static ImageBounds Fit(int imageWidth, int imageHeight, double viewWidth, double viewHeight)
    {
        if (imageWidth <= 0 || imageHeight <= 0 || viewWidth <= 0 || viewHeight <= 0)
            return default;

        double scale = Math.Min(viewWidth / imageWidth, viewHeight / imageHeight);
        double width = imageWidth * scale;
        double height = imageHeight * scale;
        return new((viewWidth - width) / 2, (viewHeight - height) / 2, width, height);
    }

    public static PixelPoint? ToPixel(double x, double y, ImageBounds bounds,
        int imageWidth, int imageHeight, bool clamp = false)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || imageWidth <= 0 || imageHeight <= 0)
            return null;

        if (!clamp && (x < bounds.Left || y < bounds.Top ||
                       x >= bounds.Left + bounds.Width || y >= bounds.Top + bounds.Height))
            return null;

        int px = (int)Math.Floor((x - bounds.Left) / bounds.Width * imageWidth);
        int py = (int)Math.Floor((y - bounds.Top) / bounds.Height * imageHeight);
        return new PixelPoint(Math.Clamp(px, 0, imageWidth - 1), Math.Clamp(py, 0, imageHeight - 1));
    }
}
