using EdsMediaArchiver.Definitions;
using EdsMediaArchiver.Helpers;
using EdsMediaArchiver.Models;
using ImageMagick;

namespace EdsMediaArchiver.Services.Compressors;

public interface IImageCompressor : IMediaCompressor { }

/// <summary>
/// Re-encodes raster images to JPEG via mozjpeg for better quality-per-byte than stock libjpeg-turbo.
/// Magick.NET handles decode / orient / resize / colour; the JPEG encode itself is delegated to
/// <see cref="IJpegEncoder"/> so the encoder is swappable and the lossy step happens exactly once.
/// </summary>
public class ImageCompressor(
    IJpegEncoder encoder,
    IExifToolService exif,
    IUserPreferences preferences) : IImageCompressor
{
    public static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        MediaType.Jpeg, MediaType.Png, MediaType.Bmp
        // Other types (HEIC/HEIF etc.) may already be more efficient than the JPEG output.
    };

    public bool IsSupported(string actualType) => SupportedTypes.Contains(actualType);

    public async Task<string> CompressAsync(string sourcePath, string outputDirectory, string fileType)
    {
        const string outputExtension = ".jpg";
        var sourceExtension = Path.GetExtension(sourcePath);
        var isAlreadyJpeg = string.Equals(sourceExtension, outputExtension, StringComparison.OrdinalIgnoreCase);

        // Standardize on a file that's already JPEG: container is already right and we don't resize,
        // so a lossy round-trip would only cost quality. Leave it untouched.
        if (isAlreadyJpeg && preferences.Standardize)
            return sourcePath;

        if (HasHdrGainMap(sourcePath))
        {
            if (preferences.ResizeOnCompress)
            {
                Console.WriteLine($"    [WARNING] File '{sourcePath}' has gain map and it will be flattened to SDR!");
            }
            else
            {
                Console.WriteLine($"    [SKIP] File '{sourcePath}' has gain map and it is not supported with this software (use ResizeOnCompress to flatten it)");
                return sourcePath;
            }
        }

        using var image = new MagickImage();
        await image.ReadAsync(sourcePath);

        // libjpeg's quality estimate — only meaningful for JPEG sources.
        int sourceQuality = isAlreadyJpeg ? (int)image.Quality : 0;

        // Non-standardize on an already-JPEG: only re-encode when there's a real reason to.
        if (isAlreadyJpeg && !preferences.Standardize)
        {
            bool tooLarge = preferences.ResizeOnCompress && (image.Width > 1920 || image.Height > 1920);
            bool worthShrinking = sourceQuality > 85; // headroom for mozjpeg to shrink near-invisibly
            if (!tooLarge && !worthShrinking)
                return sourcePath; // already small and modest quality — re-encoding would just degrade it.
        }

        image.AutoOrient();

        // True grayscale sources: keep them grey through the whole pipeline.
        // Promoting to sRGB triples the channel count for zero visual gain and a bigger file.
        bool isGrayscale = image.ColorSpace == ColorSpace.Gray;

        JpegEncodeOptions options;
        if (preferences.Standardize)
        {
            // Max fidelity: high quality, full-resolution colour, no resize.
            // Pixels keep their original colour space; the ICC profile is re-attached via ExifTool below.
            options = new JpegEncodeOptions
            {
                Quality = 92,
                Subsampling = ChromaSubsampling.Chroma444 // 4:4:4
            };
        }
        else
        {
            // Smaller files at near-invisible loss. Normalize to sRGB and allow resize.
            // It can be lossy in colour, but should be good enough for every day images, and most of them are in SRGB already anyway.
            // Only colour images need normalizing to sRGB; grey has no chroma to manage.
            if (image.ColorSpace != ColorSpace.sRGB && isGrayscale == false)
                image.TransformColorSpace(ColorProfiles.SRGB);

            if (preferences.ResizeOnCompress)
                image.Resize(new MagickGeometry("1920x1920>"));

            // Never target a quality above the source's: that inflates the file AND degrades quality
            // (the source's artefacts are already baked in). mozjpeg still shrinks it via trellis.
            int target = sourceQuality > 0 ? Math.Min(85, sourceQuality) : 85;

            options = new JpegEncodeOptions
            {
                Quality = target,
                Subsampling = ChromaSubsampling.Chroma420 // 4:2:0
            };
        }

        // Lossless intermediate: 8-bit P5 PGM for grayscale, 8-bit P6 PPM for colour.
        // cjpeg auto-detects the PNM subtype and produces a 1-channel JPEG from P5.
        // This is the only point pixels leave Magick, so the lossy encode happens exactly once.
        image.Depth = 8;
        var format = isGrayscale ? MagickFormat.Pgm : MagickFormat.Ppm;
        var ppm = image.ToByteArray(format);

        var outputPath = FileHelper.GetUniqueFilePath(
            Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(sourcePath) + outputExtension));

        await encoder.EncodeAsync(ppm, options, outputPath);
        await exif.CopyMetadata(sourcePath, outputPath, pixelsAlreadyOriented: true);

        // Carry the original filesystem timestamps onto the new file.
        File.SetLastWriteTimeUtc(outputPath, File.GetLastWriteTimeUtc(sourcePath));
        File.SetCreationTimeUtc(outputPath, File.GetCreationTimeUtc(sourcePath));

        return outputPath;
    }

    private static bool HasHdrGainMap(string path) =>
        File.ReadAllText(path, System.Text.Encoding.Latin1).Contains("urn:iso:std:iso:ts:21496");
}