using EdsMediaArchiver.Definitions;
using EdsMediaArchiver.Helpers;
using EdsMediaArchiver.Models;
using ImageMagick;

namespace EdsMediaArchiver.Services.Compressors;

public interface IImageCompressor : IMediaCompressor { }

/// <summary>
/// Compresses XMP-only image formats (WebP, BMP, TIFF) to JPG.
/// </summary>
public class ImageCompressor(IExifToolService exif, IUserPreferences preferences) : IImageCompressor
{
    public static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        MediaType.Jpeg, MediaType.Png, MediaType.Bmp
        // Any other types may be adversely affected by the current compressor, like HEIC/HEIF that are
        // more efficient than the compressor's output JPEG.
    };

    public bool IsSupported(string actualType) => SupportedTypes.Contains(actualType);

    public async Task<string> CompressAsync(string sourcePath, string outputDirectory, string fileType)
    {
        var outputExtension = ".jpg";
        var sourceExtension = Path.GetExtension(sourcePath);
        var outputPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(sourcePath) + outputExtension);
        if (outputExtension == sourceExtension && preferences.Standardize)
        {
            return sourcePath; // source already .jpg
        }

        using var image = new MagickImage();
        await image.ReadAsync(sourcePath);
        if (outputExtension == sourceExtension && preferences.Standardize == false)
        {
            // CRITERIA CHECK: If already .jpg, check if it actually needs Compression
            bool isTooLarge = image.Width > 1920 || image.Height > 1920;
            bool isHighQuality = image.Quality > 88;

            if (!isTooLarge && !isHighQuality)
            {
                // If it's already small and lower quality, do not compress to avoid generation loss.
                return sourcePath;
            }
        }

        image.AutoOrient();
        if (preferences.Standardize)
        {
            image.Quality = 92;
            image.Settings.SetDefine("jpeg:sampling-factor", "1x1,1x1,1x1");
            image.Settings.Interlace = Interlace.NoInterlace; // better performance encoding/decoding
                                                              // COLOR FIDELITY: Ensure the ICC profile is present. Keep image profile (like Adobe RGB). 
                                                              // If no profile, define sRGB so browsers don't guess.
            if (image.GetColorProfile() == null)
            {
                image.ColorSpace = ColorSpace.sRGB;
            }
        }
        else
        {
            image.Quality = 85;
            image.Settings.SetDefine("jpeg:sampling-factor", "4:2:0");
            image.Settings.Interlace = Interlace.NoInterlace; // better performance encoding/decoding
            image.ColorSpace = ColorSpace.sRGB;
            if (preferences.ResizeOnCompress)
            {
                var size = new MagickGeometry("1920x1920>");
                image.Resize(size);
            }
        }

        outputPath = FileHelper.GetUniqueFilePath(outputPath);
        await image.WriteAsync(outputPath, MagickFormat.Jpeg);
        await exif.CopyMetadata(sourcePath, outputPath);

        return outputPath;
    }
}
