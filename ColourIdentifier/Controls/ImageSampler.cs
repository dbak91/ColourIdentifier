using ColourIdentifier.Models;

namespace ColourIdentifier.Controls;

public sealed class ImageSampler : View
{
    private ImagePixels? _image;
    private bool _areaMode;

    public ImagePixels? Image
    {
        get => _image;
        set
        {
            var previous = _image;
            _image = value;
            try
            {
                Handler?.UpdateValue(nameof(Image));
            }
            catch
            {
                _image = previous;
                throw;
            }
        }
    }

    public bool AreaMode
    {
        get => _areaMode;
        set
        {
            _areaMode = value;
            Handler?.UpdateValue(nameof(AreaMode));
        }
    }

    public event EventHandler<PixelRegion>? SelectionChanged;

    public void Select(PixelRegion region) => SelectionChanged?.Invoke(this, region);
}
