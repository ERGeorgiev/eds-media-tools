using ImageMagick;

namespace EdsMediaArchiver.Services.Converters;

/// <summary>
/// Converts XMP-only image formats (WebP, BMP, TIFF) to JPG.
/// </summary>
public class ImageConverter : IMediaConverter
{
    public bool IsSupported(string actualType) => MediaType.CompressibleImageTypes.Contains(actualType);

    public async Task<string> ConvertAsync(string sourcePath, string outputDirectory, string actualType)
    {
        var outputPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(sourcePath) + ".jpg");
        outputPath = GetUniqueFilePath(outputPath);

        using var image = new MagickImage();
        await image.ReadAsync(sourcePath);

        // 1. Conversion = Max Quality
        image.Quality = 100;

        // 2. Disable Chroma Subsampling (Full Color Detail 4:4:4)
        image.Settings.SetDefine("jpeg:sampling-factor", "1x1,1x1,1x1");

        // 3. COLOR FIDELITY: Ensure the ICC profile is present. Keep image profile (like Adobe RGB). 
        // If no profile, define sRGB so browsers don't guess.
        var profile = image.GetColorProfile();
        if (profile == null)
        {
            image.ColorSpace = ColorSpace.sRGB;
        }

        await image.WriteAsync(outputPath, MagickFormat.Jpeg);
        return outputPath;
    }

    private static string GetUniqueFilePath(string path)
    {
        if (!File.Exists(path)) return path;

        var dir = Path.GetDirectoryName(path)!;
        var baseName = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        var counter = 1;

        string candidate;
        do
        {
            candidate = Path.Combine(dir, $"{baseName}{counter}{ext}");
            counter++;
        } while (File.Exists(candidate));

        return candidate;
    }
}
