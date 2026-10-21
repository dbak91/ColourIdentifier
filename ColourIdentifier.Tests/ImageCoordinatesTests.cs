using ColourIdentifier.Models;
using ColourIdentifier.Services;

namespace ColourIdentifier.Tests;

public class ImageCoordinatesTests
{
    [Fact]
    public void LetterboxingIsExcludedFromPointSelection()
    {
        var bounds = ImageCoordinates.Fit(400, 200, 300, 300);
        Assert.Equal(new ImageBounds(0, 75, 300, 150), bounds);
        Assert.Null(ImageCoordinates.ToPixel(150, 74, bounds, 400, 200));
        Assert.Null(ImageCoordinates.ToPixel(150, 225, bounds, 400, 200));
        Assert.Equal(new PixelPoint(200, 100), ImageCoordinates.ToPixel(150, 150, bounds, 400, 200));
    }

    [Fact]
    public void PortraitImageMapsAtBothEdges()
    {
        var bounds = ImageCoordinates.Fit(100, 200, 300, 200);
        Assert.Equal(new ImageBounds(100, 0, 100, 200), bounds);
        Assert.Equal(new PixelPoint(0, 0), ImageCoordinates.ToPixel(100, 0, bounds, 100, 200));
        Assert.Equal(new PixelPoint(99, 199), ImageCoordinates.ToPixel(199.9, 199.9, bounds, 100, 200));
        Assert.Null(ImageCoordinates.ToPixel(200, 100, bounds, 100, 200));
    }

    [Fact]
    public void DragOutsideImageClampsToNearestEdge()
    {
        var bounds = ImageCoordinates.Fit(100, 200, 300, 200);
        Assert.Equal(new PixelPoint(0, 199), ImageCoordinates.ToPixel(-30, 300, bounds, 100, 200, clamp: true));
        Assert.Equal(new PixelPoint(99, 0), ImageCoordinates.ToPixel(400, -40, bounds, 100, 200, clamp: true));
    }

    [Fact]
    public void UnmeasuredViewDoesNotSelectPixels() =>
        Assert.Null(ImageCoordinates.ToPixel(0, 0, ImageCoordinates.Fit(100, 100, 0, 0), 100, 100));
}
