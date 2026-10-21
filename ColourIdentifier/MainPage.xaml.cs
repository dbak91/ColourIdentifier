using ColourIdentifier.Models;
using ColourIdentifier.Platforms.Android;
using ColourIdentifier.Services;

namespace ColourIdentifier;

public partial class MainPage : ContentPage
{
    private RgbColour? _selectedColour;

    public MainPage()
    {
        InitializeComponent();
        Sampler.SelectionChanged += OnSelectionChanged;
    }

    private async void OnImportClicked(object? sender, EventArgs e) =>
        await ChooseImageAsync(useCamera: false);

    private async void OnCameraClicked(object? sender, EventArgs e) =>
        await ChooseImageAsync(useCamera: true);

    private async Task ChooseImageAsync(bool useCamera)
    {
        if (!ImportButton.IsEnabled)
            return;

        ImportButton.IsEnabled = CameraButton.IsEnabled = false;
        try
        {
            if (useCamera && !MediaPicker.Default.IsCaptureSupported)
            {
                Status.Text = "No camera is available. You can still import an image.";
                return;
            }

            var file = useCamera
                ? await MediaPicker.Default.CapturePhotoAsync()
                : await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Select an image",
                    FileTypes = FilePickerFileType.Images
                });
            if (file is null)
            {
                Status.Text = "Selection cancelled. Your previous image is unchanged.";
                return;
            }

            Status.Text = "Loading image…";
            Sampler.IsEnabled = false;
            var loaded = await Task.Run(() => ImageLoader.LoadAsync(file));
            Sampler.Image = loaded.Pixels;
            ImageInfo.Text = $"{file.FileName} • {loaded.Pixels.Width} × {loaded.Pixels.Height} sampled pixels";
            Status.Text = loaded.WasReduced
                ? "Large image reduced to at most 2048 pixels per side for memory safety."
                : "Image loaded. Ready to sample.";
            _selectedColour = null;
            ColourName.Text = "No colour selected";
            HexValue.Text = "CSS hex: —";
            RgbValue.Text = "CSS rgb(): —";
            Swatch.Color = Colors.Transparent;
            CopyHexButton.IsEnabled = CopyRgbButton.IsEnabled = false;
            SelectionInfo.Text = "Coordinates use the sampled image, starting at (0, 0).";
        }
        catch (PermissionException)
        {
            Status.Text = "Camera permission was denied. Your previous image is unchanged.";
            await DisplayAlertAsync("Camera permission needed",
                "Allow camera access in Android settings to take a photo, or use Import image instead.", "OK");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Status.Text = "Could not load image. Your previous image is unchanged.";
            await DisplayAlertAsync(useCamera ? "Cannot take or open photo" : "Cannot open image",
                useCamera
                    ? "The camera may be unavailable, or the photo could not be decoded. Try again or import an image."
                    : "Choose a readable PNG, JPEG, WebP or another image supported by Android. " +
                      "The file may be unsupported, damaged, unavailable or too large to decode.", "OK");
        }
        catch (OperationCanceledException)
        {
            Status.Text = "Selection cancelled. Your previous image is unchanged.";
        }
        finally
        {
            Sampler.IsEnabled = true;
            ImportButton.IsEnabled = CameraButton.IsEnabled = true;
        }
    }

    private void OnAreaToggled(object? sender, ToggledEventArgs e)
    {
        Sampler.AreaMode = e.Value;
        Instructions.Text = e.Value
            ? "Drag across the image; release to average the rectangle."
            : "Tap the image to identify one pixel.";
    }

    private void OnSelectionChanged(object? sender, PixelRegion region)
    {
        if (Sampler.Image is not { } image)
            return;

        var colour = image.Average(region);
        _selectedColour = colour;
        ColourName.Text = $"Colour: {ColourNames.Nearest(colour)} (closest match)";
        HexValue.Text = $"CSS hex: {colour.Hex}";
        RgbValue.Text = $"CSS rgb(): {colour.CssRgb}";
        Swatch.Color = Color.FromRgb(colour.R, colour.G, colour.B);
        SelectionInfo.Text = region.PixelCount == 1
            ? $"Point ({region.Left}, {region.Top}) • 1 pixel"
            : $"({region.Left}, {region.Top})–({region.Right}, {region.Bottom}) • " +
              $"{region.Width} × {region.Height} • {region.PixelCount:N0} pixels";
        CopyHexButton.IsEnabled = CopyRgbButton.IsEnabled = true;
        Status.Text = "Colour sampled. Transparent pixels are blended onto white.";
    }

    private async void OnCopyHexClicked(object? sender, EventArgs e) =>
        await CopyAsync(_selectedColour?.Hex);

    private async void OnCopyRgbClicked(object? sender, EventArgs e) =>
        await CopyAsync(_selectedColour?.CssRgb);

    private async Task CopyAsync(string? value)
    {
        if (value is null)
            return;

        try
        {
            await Clipboard.Default.SetTextAsync(value);
            Status.Text = $"Copied {value}";
        }
        catch (Exception)
        {
            Status.Text = "Clipboard is unavailable. You can read the value above.";
        }
    }
}
