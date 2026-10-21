using Android.Graphics;
using Android.Media;
using ColourIdentifier.Models;

namespace ColourIdentifier.Platforms.Android;

public sealed record LoadedImage(ImagePixels Pixels, bool WasReduced);

public static class ImageLoader
{
    private const int MaximumSide = 2048;

    public static async Task<LoadedImage> LoadAsync(FileResult file)
    {
        using var bounds = new BitmapFactory.Options { InJustDecodeBounds = true };
        using (var stream = await file.OpenReadAsync())
            BitmapFactory.DecodeStream(stream, null, bounds)?.Dispose();

        if (bounds.OutWidth <= 0 || bounds.OutHeight <= 0)
            throw new InvalidDataException("Android could not recognise this image.");

        int sampleSize = 1;
        while (Math.Ceiling((double)Math.Max(bounds.OutWidth, bounds.OutHeight) / sampleSize) > MaximumSide)
            sampleSize *= 2;

        using var options = new BitmapFactory.Options
        {
            InSampleSize = sampleSize,
            InPreferredConfig = Bitmap.Config.Argb8888,
            InScaled = false
        };
        using var input = await file.OpenReadAsync();
        using var decoded = BitmapFactory.DecodeStream(input, null, options)
            ?? throw new InvalidDataException("Android could not decode this image.");

        var orientation = Orientation.Normal;
        try
        {
            using var metadata = await file.OpenReadAsync();
            using var exif = new ExifInterface(metadata);
            orientation = (Orientation)exif.GetAttributeInt(ExifInterface.TagOrientation, (int)Orientation.Normal);
        }
        catch (Java.IO.IOException)
        {
            // Some Android-supported formats have no EXIF metadata.
        }
        catch (IOException)
        {
            // Metadata is optional; the decoded pixels remain usable.
        }

        using var transform = new Matrix();
        switch (orientation)
        {
            case Orientation.FlipHorizontal: transform.SetScale(-1, 1); break;
            case Orientation.Rotate180: transform.SetRotate(180); break;
            case Orientation.FlipVertical: transform.SetScale(1, -1); break;
            case Orientation.Transpose:
                transform.SetRotate(90); transform.PostScale(-1, 1); break;
            case Orientation.Rotate90: transform.SetRotate(90); break;
            case Orientation.Transverse:
                transform.SetRotate(-90); transform.PostScale(-1, 1); break;
            case Orientation.Rotate270: transform.SetRotate(-90); break;
        }

        // CreateBitmap can return decoded itself for the identity transformation.
        var oriented = Bitmap.CreateBitmap(decoded, 0, 0, decoded.Width, decoded.Height, transform, false)
            ?? throw new InvalidDataException("Could not orient this image.");
        try
        {
            var pixels = new int[checked(oriented.Width * oriented.Height)];
            oriented.GetPixels(pixels, 0, oriented.Width, 0, 0, oriented.Width, oriented.Height);
            return new LoadedImage(new ImagePixels(oriented.Width, oriented.Height, pixels), sampleSize > 1);
        }
        finally
        {
            if (!ReferenceEquals(oriented, decoded))
                oriented.Dispose();
        }
    }
}
