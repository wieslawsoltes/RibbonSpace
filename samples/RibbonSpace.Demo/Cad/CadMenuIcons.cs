using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using RibbonSpace.Model;
using SkiaSharp;

namespace RibbonSpace.Demo.Cad;

/// <summary>
/// Gives <see cref="MenuFlyoutItem"/>s layered line-art icons. Menu items only accept an <see cref="IconElement"/>
/// (RibbonSpace converts glyph icons only), so the sample rasterizes the CAD path icon (stored in the item's
/// <c>Tag</c>, which <c>FollowLastChoice</c> split buttons also use for the primary part) into an
/// <see cref="ImageIcon"/> for the current theme.
/// </summary>
public static class CadMenuIcons
{
    private static readonly Dictionary<(string, bool), BitmapImage> Cache = [];

    /// <summary>Sets icons on every menu item (recursively) whose Tag is a path <see cref="RibbonIcon"/>.</summary>
    public static void Apply(IEnumerable<MenuFlyoutItemBase> items, bool dark)
    {
        foreach (var item in items)
        {
            switch (item)
            {
                case MenuFlyoutSubItem sub:
                    if (sub.Tag is RibbonIcon subIcon && Create(subIcon, dark) is { } subImage)
                    {
                        sub.Icon = subImage;
                    }

                    Apply(sub.Items, dark);
                    break;
                case MenuFlyoutItem menuItem when menuItem.Tag is RibbonIcon icon && Create(icon, dark) is { } image:
                    menuItem.Icon = image;
                    break;
            }
        }
    }

    /// <summary>Creates an image icon for a layered path icon.</summary>
    public static ImageIcon? Create(RibbonIcon icon, bool dark)
    {
        if (icon.Kind != RibbonIconKind.Path)
        {
            return null;
        }

        try
        {
            if (!Cache.TryGetValue((icon.Value, dark), out var bitmap))
            {
                bitmap = new BitmapImage();
                using var stream = new MemoryStream(Render(icon.Value, icon.ViewBoxSize, 40, dark ? new SKColor(0xD8, 0xDE, 0xE6) : new SKColor(0x2F, 0x37, 0x42)));
                bitmap.SetSource(stream.AsRandomAccessStream());
                Cache[(icon.Value, dark)] = bitmap;
            }

            return new ImageIcon { Source = bitmap, Width = 20, Height = 20 };
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException or NotImplementedException)
        {
            return null;
        }
    }

    /// <summary>Renders layered path data into a PNG.</summary>
    public static byte[] Render(string layered, double viewBox, int pixels, SKColor themed)
    {
        using var surface = SKSurface.Create(new SKImageInfo(pixels, pixels, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        canvas.Scale((float)(pixels / viewBox));
        foreach (var layer in RibbonIconLayer.Parse(layered))
        {
            using var path = SKPath.ParseSvgPathData(layer.Data);
            if (path is null)
            {
                continue;
            }

            var color = themed;
            if (layer.Color is { } hex && SKColor.TryParse(hex, out var fixedColor))
            {
                color = fixedColor;
            }

            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = color.WithAlpha((byte)(color.Alpha * Math.Clamp(layer.Opacity, 0, 1))),
                Style = layer.StrokeThickness > 0 ? SKPaintStyle.Stroke : SKPaintStyle.Fill,
                StrokeWidth = (float)layer.StrokeThickness,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
            };
            canvas.DrawPath(path, paint);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
