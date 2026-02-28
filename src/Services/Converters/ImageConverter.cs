using ImageMagick;

namespace EdsMediaArchiver.Services.Converters;

public interface IImageConverter
{
    Task<bool> ConvertToJpgAsync(string sourcePath, string destPath);
}

/// <summary>
/// Uses Magick.NET for image conversion and file type detection.
/// Only required for converting XMP-only formats (WebP, BMP, GIF, TIFF) to JPG.
/// </summary>
public class ImageConverter : IImageConverter
{
    /// <summary>
    /// Converts an image to JPG with quality and colorspace settings matching
    /// the original CLI invocation: -quality 95 -sampling-factor 4:2:0 -colorspace sRGB.
    /// </summary>
    public async Task<bool> ConvertToJpgAsync(string sourcePath, string destPath)
    {
        try
        {
            using var image = new MagickImage();
            await image.ReadAsync(sourcePath);

            image.Quality = 95;
            image.Settings.SetDefine("jpeg:sampling-factor", "4:2:0");
            image.ColorSpace = ColorSpace.sRGB;

            await image.WriteAsync(destPath, MagickFormat.Jpeg);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
