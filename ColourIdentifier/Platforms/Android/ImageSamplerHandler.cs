using Android.Content;
using Android.Graphics;
using Android.Views;
using ColourIdentifier.Controls;
using ColourIdentifier.Models;
using ColourIdentifier.Services;
using Microsoft.Maui.Handlers;
using AColor = Android.Graphics.Color;
using AView = Android.Views.View;
using Paint = Android.Graphics.Paint;
using RectF = Android.Graphics.RectF;

namespace ColourIdentifier.Platforms.Android;

public sealed class ImageSamplerHandler : ViewHandler<ImageSampler, SamplingCanvas>
{
    private static readonly IPropertyMapper<ImageSampler, ImageSamplerHandler> SamplerMapper =
        new PropertyMapper<ImageSampler, ImageSamplerHandler>(ViewMapper)
        {
            [nameof(ImageSampler.Image)] = (handler, view) => handler.PlatformView.SetImage(view.Image),
            [nameof(ImageSampler.AreaMode)] = (handler, _) => handler.PlatformView.CancelSelection()
        };

    public ImageSamplerHandler() : base(SamplerMapper) { }

    protected override SamplingCanvas CreatePlatformView() => new(Context, VirtualView);

    protected override void DisconnectHandler(SamplingCanvas platformView)
    {
        platformView.SetImage(null);
        base.DisconnectHandler(platformView);
    }
}

public sealed class SamplingCanvas : AView
{
    private readonly ImageSampler _sampler;
    private readonly Paint _bitmapPaint = new() { FilterBitmap = false };
    private readonly Paint _outline = new() { AntiAlias = true, Color = AColor.Black };
    private readonly Paint _highlight = new() { AntiAlias = true, Color = AColor.White };
    private Bitmap? _bitmap;
    private PixelPoint? _start;
    private PixelRegion? _selection;

    public SamplingCanvas(Context context, ImageSampler sampler) : base(context)
    {
        _sampler = sampler;
        _outline.SetStyle(Paint.Style.Stroke);
        _highlight.SetStyle(Paint.Style.Stroke);
        _outline.StrokeWidth = 4 * Resources!.DisplayMetrics!.Density;
        _highlight.StrokeWidth = 2 * Resources.DisplayMetrics.Density;
        SetBackgroundColor(new AColor(30, 30, 30));
        ContentDescription = "Image sampling canvas";
    }

    public void SetImage(ImagePixels? image)
    {
        // Build the replacement first so an allocation failure keeps the previous preview.
        var replacement = image is null ? null :
            Bitmap.CreateBitmap(image.Argb, image.Width, image.Height, Bitmap.Config.Argb8888!);
        var previous = _bitmap;
        _bitmap = replacement;
        previous?.Dispose();
        CancelSelection();
    }

    public void CancelSelection()
    {
        _start = null;
        _selection = null;
        Parent?.RequestDisallowInterceptTouchEvent(false);
        Invalidate();
    }

    private ImageBounds Bounds => _bitmap is null ? default :
        ImageCoordinates.Fit(_bitmap.Width, _bitmap.Height, Width, Height);

    protected override void OnDraw(Canvas canvas)
    {
        base.OnDraw(canvas);
        if (_bitmap is null)
            return;

        var bounds = Bounds;
        using var destination = new RectF((float)bounds.Left, (float)bounds.Top,
            (float)(bounds.Left + bounds.Width), (float)(bounds.Top + bounds.Height));
        using var background = new Paint { Color = AColor.White };
        canvas.DrawRect(destination, background);
        canvas.DrawBitmap(_bitmap, null, destination, _bitmapPaint);

        if (_selection is not { } region)
            return;

        float left = (float)(bounds.Left + region.Left * bounds.Width / _bitmap.Width);
        float top = (float)(bounds.Top + region.Top * bounds.Height / _bitmap.Height);
        float right = (float)(bounds.Left + (region.Right + 1) * bounds.Width / _bitmap.Width);
        float bottom = (float)(bounds.Top + (region.Bottom + 1) * bounds.Height / _bitmap.Height);
        if (region.PixelCount == 1)
        {
            float x = (left + right) / 2;
            float y = (top + bottom) / 2;
            float radius = 12 * Resources!.DisplayMetrics!.Density;
            foreach (var paint in new[] { _outline, _highlight })
            {
                canvas.DrawLine(x - radius, y, x + radius, y, paint);
                canvas.DrawLine(x, y - radius, x, y + radius, paint);
            }
        }
        else
        {
            canvas.DrawRect(left, top, right, bottom, _outline);
            canvas.DrawRect(left, top, right, bottom, _highlight);
        }
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (!Enabled || e is null || _bitmap is null)
            return false;

        if (e.ActionMasked == MotionEventActions.Cancel || e.ActionMasked == MotionEventActions.PointerDown)
        {
            CancelSelection();
            return true;
        }

        var point = ImageCoordinates.ToPixel(e.GetX(), e.GetY(), Bounds, _bitmap.Width, _bitmap.Height,
            clamp: _start is not null);
        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
                if (point is null)
                    return false;
                _start = point;
                _selection = PixelRegion.Between(point.Value, point.Value);
                Parent?.RequestDisallowInterceptTouchEvent(true);
                Invalidate();
                return true;
            case MotionEventActions.Move when _start is { } start && point is { } end:
                _selection = _sampler.AreaMode ? PixelRegion.Between(start, end) : PixelRegion.Between(end, end);
                Invalidate();
                return true;
            case MotionEventActions.Up when _start is { } start && point is { } end:
                var region = _sampler.AreaMode ? PixelRegion.Between(start, end) : PixelRegion.Between(end, end);
                _selection = region;
                _start = null;
                Parent?.RequestDisallowInterceptTouchEvent(false);
                PerformClick();
                Invalidate();
                _sampler.Select(region);
                return true;
        }

        return _start is not null;
    }

    public override bool PerformClick()
    {
        base.PerformClick();
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _bitmap?.Dispose();
            _bitmap = null;
            _bitmapPaint.Dispose();
            _outline.Dispose();
            _highlight.Dispose();
        }
        base.Dispose(disposing);
    }
}
