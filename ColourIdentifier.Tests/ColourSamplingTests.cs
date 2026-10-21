using ColourIdentifier.Models;
using ColourIdentifier.Services;

namespace ColourIdentifier.Tests;

public class ColourSamplingTests
{
    private static readonly ImagePixels Image = new(2, 2,
    [
        unchecked((int)0xFFFF0000), unchecked((int)0xFF00FF00),
        unchecked((int)0xFF0000FF), unchecked((int)0xFFFFFFFF)
    ]);

    [Fact]
    public void PointReturnsExactPixelAndCssValues()
    {
        var colour = Image.Average(new PixelRegion(0, 0, 0, 0));
        Assert.Equal("#FF0000", colour.Hex);
        Assert.Equal("rgb(255, 0, 0)", colour.CssRgb);
        Assert.Equal("Red", ColourNames.Nearest(colour));
    }

    [Fact]
    public void RectangleAveragesAllPixelsIncludingEdges()
    {
        var region = PixelRegion.Between(new PixelPoint(1, 1), new PixelPoint(0, 0));
        Assert.Equal(4, region.PixelCount);
        Assert.Equal(new RgbColour(128, 128, 128), Image.Average(region));
        Assert.Equal(new RgbColour(128, 255, 128), Image.Average(new PixelRegion(1, 0, 1, 1)));
    }

    [Fact]
    public void TransparentPixelsAreCompositedOntoWhite()
    {
        var image = new ImagePixels(2, 1, [0x00000000, unchecked((int)0x80FF0000)]);
        Assert.Equal("#FFFFFF", image.Average(new PixelRegion(0, 0, 0, 0)).Hex);
        Assert.Equal("#FF7F7F", image.Average(new PixelRegion(1, 0, 1, 0)).Hex);
    }

    [Theory]
    [InlineData(-1, 0, 0, 0)]
    [InlineData(0, 0, 2, 1)]
    [InlineData(1, 1, 0, 0)]
    public void RejectsInvalidRegions(int left, int top, int right, int bottom) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Image.Average(new PixelRegion(left, top, right, bottom)));

    [Fact]
    public void RejectsMismatchedPixelBuffer() =>
        Assert.Throws<ArgumentException>(() => new ImagePixels(2, 2, [0]));

    [Theory]
    [InlineData(254, 1, 2, "Red")]
    [InlineData(102, 51, 153, "Rebecca Purple")]
    [InlineData(0, 0, 0, "Black")]
    [InlineData(255, 255, 255, "White")]
    [InlineData(0, 255, 0, "Lime")]
    public void FindsNearestCssName(byte r, byte g, byte b, string expected) =>
        Assert.Equal(expected, ColourNames.Nearest(new RgbColour(r, g, b)));
}
