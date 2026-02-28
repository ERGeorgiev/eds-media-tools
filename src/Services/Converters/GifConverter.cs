using ImageMagick;

namespace EdsMediaArchiver.Services.Converters;

/// <summary>
/// Handles GIF compression: single-frame GIFs become JPG, multi-frame GIFs become MP4.
/// </summary>
public class GifConverter(IImageConverter imageConverter, IVideoConverter videoConverter) : IMediaConverter
{
    public bool IsSupported(string actualType) =>
        actualType.Equals(MediaType.Gif, StringComparison.OrdinalIgnoreCase);

    public async Task<string> ConvertAsync(string sourcePath, string outputDirectory, string actualType)
    {
        var frameCount = GetFrameCount(sourcePath);

        if (frameCount <= 1)
            return await imageConverter.ConvertAsync(sourcePath, outputDirectory, actualType);
        else
            return await videoConverter.ConvertAsync(sourcePath, outputDirectory, actualType);
    }

    private static int GetFrameCount(string path)
    {
        using var images = new MagickImageCollection();
        images.Read(path);
        return images.Count;
    }
}
