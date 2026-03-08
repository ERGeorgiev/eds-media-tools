using EdsMediaArchiver.Definitions;
using EdsMediaArchiver.Helpers;
using EdsMediaArchiver.Models;
using FFMpegCore;
using System.Reflection;

namespace EdsMediaArchiver.Services.Compressors;

/// <summary>
/// Compresses audio files to OGG (Vorbis).
/// </summary>
public class AudioCompressor : IMediaCompressor
{
    public static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        MediaType.Mp3, MediaType.Wav, MediaType.Flac, MediaType.Ogg, MediaType.Aac, MediaType.Wma, MediaType.M4a, MediaType.Opus,
        MediaType.Aiff, MediaType.Amr, MediaType.Ac3, MediaType.Dts, MediaType.Pcm, MediaType.M4b, MediaType.Mp2
        // As this compressor uses Opus, it's an upgrade for pretty much all other audio extensions listed here.
    };

    private readonly IUserPreferences _preferences;
    private readonly string _coverImagePath;

    public AudioCompressor(IUserPreferences preferences)
    {
        _preferences = preferences;
        _coverImagePath = ExtractCoverImage();

        static string ExtractCoverImage()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .First(n => n.EndsWith("music.jpg"));

            var tempPath = Path.Combine(Path.GetTempPath(), "music_cover.jpg");

            using var resourceStream = assembly.GetManifestResourceStream(resourceName)!;
            using var fileStream = File.Create(tempPath);
            resourceStream.CopyTo(fileStream);

            return tempPath;
        }
    }

    public bool IsSupported(string actualType) => SupportedTypes.Contains(actualType);

    public bool OutputMp4InsteadOfOgg { get; set; } = false;
    public bool ConvertOnly { get; set; } = false;

    public async Task<string> CompressAsync(string sourcePath, string outputDirectory, string fileType)
    {
        var outputExtension = _preferences.AudioToMp4 ? ".mp4" : ".ogg";
        var sourceExtension = Path.GetExtension(sourcePath);
        var outputPath = Path.Combine(outputDirectory,
            Path.GetFileNameWithoutExtension(sourcePath) + outputExtension);
        if (outputExtension.Equals(sourceExtension, StringComparison.OrdinalIgnoreCase))
            return sourcePath; // Already processed, likely small enough already.

        outputPath = FileHelper.GetUniqueFilePath(outputPath);
        if (_preferences.Standardize)
        {
            if (_preferences.AudioToMp4)
            {
                await FFMpegArguments
                    .FromFileInput(_coverImagePath, verifyExists: true, options => options
                        .WithCustomArgument("-loop 1"))
                    .AddFileInput(sourcePath)
                    .OutputToFile(outputPath, overwrite: false, options =>
                    {
                        options
                            .WithVideoCodec("libx264")
                            .WithCustomArgument("-tune stillimage")
                            .WithCustomArgument("-pix_fmt yuv420p")
                            .WithCustomArgument("-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\"")
                            .WithCustomArgument("-c:a copy") // remux the audio without re-encoding
                            .WithCustomArgument("-shortest")
                            .WithCustomArgument("-map_metadata 1"); // copy metadata from audio input (index 1)
                    })
                    .ProcessAsynchronously();
            }
            else
            {
                await FFMpegArguments
                    .FromFileInput(sourcePath)
                    .OutputToFile(outputPath, overwrite: false, options =>
                    {
                        options
                        .WithAudioCodec("copy")
                        .WithCustomArgument("-map_metadata 0"); // preserve all metadata
                    })
                    .ProcessAsynchronously();
            }
        }
        else
        {
            if (_preferences.AudioToMp4)
            {
                await FFMpegArguments
                    .FromFileInput(_coverImagePath, verifyExists: true, options => options
                        .WithCustomArgument("-loop 1"))
                    .AddFileInput(sourcePath)
                    .OutputToFile(outputPath, overwrite: false, options =>
                    {
                        options
                            .WithVideoCodec("libx264")
                            .WithCustomArgument("-tune stillimage")
                            .WithCustomArgument("-pix_fmt yuv420p")
                            .WithCustomArgument("-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\"")
                            .WithCustomArgument("-shortest")
                            .WithAudioCodec("libopus")
                            .WithAudioBitrate(128)
                            .WithCustomArgument("-vbr on")
                            .WithCustomArgument("-map_metadata 1"); // copy metadata from audio input (index 1)
                    })
                    .ProcessAsynchronously();
            }
            else
            {
                await FFMpegArguments
                    .FromFileInput(sourcePath)
                    .OutputToFile(outputPath, overwrite: false, options =>
                    {
                        options
                        .WithAudioCodec("libopus")
                        .WithAudioBitrate(128)
                        .WithCustomArgument("-frame_size 60")
                        .WithCustomArgument("-vbr on")
                        .WithCustomArgument("-vn")
                        .WithCustomArgument("-map_metadata 0");
                    })
                    .ProcessAsynchronously();
            }
        }

        return outputPath;
    }
}
