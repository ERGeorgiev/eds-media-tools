using EdsMediaArchiver.Definitions;
using EdsMediaArchiver.Helpers;
using EdsMediaArchiver.Models;
using FFMpegCore;
using FFMpegCore.Enums;
using System.Diagnostics;

namespace EdsMediaArchiver.Services.Compressors;

public interface IVideoCompressor : IMediaCompressor { }

/// <summary>
/// Compresses video formats to MP4 (H.264 + AAC).
/// </summary>
public class VideoCompressor(IExifToolService exif, IUserPreferences preferences) : IVideoCompressor
{
    public static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        MediaType.Asf, MediaType.Avi, MediaType.Amv, MediaType.Dv, MediaType.Dvr_ms, MediaType.F4V, MediaType.Flv, MediaType.Gxf, MediaType.Lrv,
        MediaType.M2Ts, MediaType.M4V, MediaType.Mj2, MediaType.Mjpeg, MediaType.Mkv, MediaType.Mod, MediaType.Mov, MediaType.Mp4, MediaType.Mpeg,
        MediaType.Mpegts, MediaType.Mpg, MediaType.Mts, MediaType.Mvi, MediaType.Mxf, MediaType.Ogv, MediaType.QuickTime, MediaType.Rm, MediaType.Rmvb, 
        MediaType.ThreeG2, MediaType.ThreeGp, MediaType.Tod, MediaType.Ts, MediaType.Vob, MediaType.Wmv, MediaType.Wtv
        // Any other types may be adversely affected by the current compressor, like WebM/AV1 that is
        // more efficient than the compressor's output MP4/H265.
        // In terms of conversion/standardization for DateWrite, it's less of an issue for videos (few/none support exif anyway)
    };

    public bool IsSupported(string actualType) => SupportedTypes.Contains(actualType);

    public async Task<string> CompressAsync(string sourcePath, string outputDirectory, string fileType)
    {
        var outputExtension = ".mp4";
        var sourceExtension = Path.GetExtension(sourcePath);
        var outputPath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(sourcePath) + outputExtension);
        if (outputExtension.Equals(sourceExtension, StringComparison.OrdinalIgnoreCase))
        {
            if (preferences.Standardize)
            {
                return sourcePath;
            }
            // Prevent compression of already-compressed files.
            var analysis = await FFProbe.AnalyseAsync(sourcePath);
            var videoStream = analysis.VideoStreams.FirstOrDefault();
            if (videoStream != null)
            {
                bool isSmallEnough = videoStream.Width <= 1920 && videoStream.Height <= 1920;
                bool isModernCodec = videoStream.CodecName is "h264" or "hevc";
                double bitrateKbps = analysis.Format.BitRate / 1000.0;
                bool isLowBitrate = bitrateKbps <= 10000;

                if (isSmallEnough && isLowBitrate && isModernCodec)
                {
                    return sourcePath; // Already compressed
                }
            }
        }

        outputPath = FileHelper.GetUniqueFilePath(outputPath);
        if (preferences.Standardize)
        {
            // Stream copy: no re-encode, so colour signalling passes through untouched.
            await FFMpegArguments
                .FromFileInput(sourcePath)
                .OutputToFile(outputPath, overwrite: false, options =>
                {
                    options
                        .WithVideoCodec("copy")
                        .WithAudioCodec("copy")
                        .WithCustomArgument("-map_metadata 0")
                        .WithCustomArgument("-movflags +faststart");
                })
                .ProcessAsynchronously();
        }
        else
        {
            var colour = await ProbeColourAsync(sourcePath);
            var (videoFilter, colourArgs) = BuildColourPipeline(colour, preferences.ResizeOnCompress);
            await FFMpegArguments
                .FromFileInput(sourcePath)
                .OutputToFile(outputPath, overwrite: false, options =>
                {
                    options
                        .WithVideoCodec("libx264")
                        .WithConstantRateFactor(23)
                        .WithSpeedPreset(Speed.Slow)
                        .WithAudioCodec("aac")
                        .WithAudioBitrate(128)
                        .WithCustomArgument("-pix_fmt yuv420p")
                        .WithCustomArgument("-map_metadata 0")
                        .WithCustomArgument("-profile:v main")
                        .WithCustomArgument("-movflags +faststart")
                        .WithCustomArgument($"-vf \"{videoFilter}\"");
                    foreach (var arg in colourArgs)
                    {
                        options.WithCustomArgument(arg);
                    }
                })
                .ProcessAsynchronously();
        }

        await exif.CopyMetadata(sourcePath, outputPath);

        // Carry the original filesystem modification date onto the new file
        File.SetLastWriteTimeUtc(outputPath, File.GetLastWriteTimeUtc(sourcePath));
        File.SetCreationTimeUtc(outputPath, File.GetCreationTimeUtc(sourcePath));

        return outputPath;
    }

    private sealed record ColourInfo(string Range, string Space, string Primaries, string Transfer)
    {
        /// <summary>
        /// Newer phones record HDR (BT.2020 + PQ/HLG). An 8-bit H.264 SDR encode
        /// cannot carry that, so HDR sources must be tone-mapped, not range-fixed.
        /// </summary>
        public bool IsHdr =>
            Transfer is "smpte2084" or "arib-std-b67"   // PQ or HLG
            || Primaries == "bt2020"
            || Space is "bt2020nc" or "bt2020c";
    }

    /// <summary>
    /// Reads colour signalling via ffprobe directly. FFMpegCore's VideoStream
    /// does not reliably surface color_range across versions, so we don't depend
    /// on it. Returns empty strings for unspecified/unknown values.
    /// </summary>
    private static async Task<ColourInfo> ProbeColourAsync(string path)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "ffprobe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("-v");
        psi.ArgumentList.Add("error");
        psi.ArgumentList.Add("-select_streams");
        psi.ArgumentList.Add("v:0");
        psi.ArgumentList.Add("-show_entries");
        psi.ArgumentList.Add("stream=color_range,color_space,color_primaries,color_transfer");
        psi.ArgumentList.Add("-of");
        psi.ArgumentList.Add("default=noprint_wrappers=1");
        psi.ArgumentList.Add(path);

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start ffprobe.");
        var stdout = await proc.StandardOutput.ReadToEndAsync();
        await proc.WaitForExitAsync();

        return new ColourInfo(
            Read("color_range"),
            Read("color_space"),
            Read("color_primaries"),
            Read("color_transfer"));

        string Read(string key)
        {
            foreach (var line in stdout.Split('\n'))
            {
                if (line.StartsWith(key + "=", StringComparison.Ordinal))
                {
                    var value = line[(key.Length + 1)..].Trim();
                    return value is "" or "unknown" or "N/A" ? "" : value;
                }
            }
            return "";
        }
    }

    /// <summary>
    /// Builds the <c>-vf</c> chain plus the colour output arguments.
    /// SDR: preserve source range, tag to match (unknown defaults to limited/tv,
    /// which avoids the "limited data tagged full" washout — the common case).
    /// HDR: tone-map BT.2020 PQ/HLG down to BT.709 limited in linear light.
    /// </summary>
    private static (string VideoFilter, string[] ColourArgs) BuildColourPipeline(ColourInfo c, bool resize)
    {
        string scale = resize
            ? "scale=1920:1920:force_original_aspect_ratio=decrease:force_divisible_by=2"
            : "scale='trunc(iw/2)*2:trunc(ih/2)*2'";

        if (c.IsHdr)
        {
            // npl (nominal peak luminance) and the tonemap operator are aesthetic
            // choices — tune per source. This deliberately flattens HDR to SDR.
            string vf =
                $"zscale=transferin={Or(c.Transfer, "smpte2084")}:" +
                $"matrixin={Or(c.Space, "bt2020nc")}:" +
                $"primariesin={Or(c.Primaries, "bt2020")}:transfer=linear:npl=100," +
                "format=gbrpf32le,tonemap=hable:desat=0," +
                "zscale=primaries=bt709:transfer=bt709:matrix=bt709:range=tv," +
                $"{scale},format=yuv420p";

            return (vf, Bt709Args("tv"));
        }

        // SDR: preserve the source range. Identity zscale (rangein == range) does
        // NOT remap samples; it only pins metadata and blocks the implicit
        // yuvj420p->yuv420p full->limited shift. setparams keeps the frame's range
        // tag aligned with the -color_range output flag.
        string range = c.Range == "pc" ? "pc" : "tv";
        string zr = range == "pc" ? "full" : "limited";
        string space = Or(c.Space, "bt709");
        string primaries = Or(c.Primaries, "bt709");
        string transfer = Or(c.Transfer, "bt709");

        string sdrVf =
            $"zscale=rangein={zr}:range={zr},{scale},format=yuv420p,setparams=range={range}";

        var args = new[]
        {
            $"-color_range {range}",
            $"-colorspace {space}",
            $"-color_primaries {primaries}",
            $"-color_trc {transfer}",
            $"-x264-params \"range={range}:colormatrix={space}:transfer={transfer}:colorprim={primaries}\""
        };

        return (sdrVf, args);

        static string[] Bt709Args(string range) =>
        [
            $"-color_range {range}",
            "-colorspace bt709",
            "-color_primaries bt709",
            "-color_trc bt709",
            $"-x264-params \"range={range}:colormatrix=bt709:transfer=bt709:colorprim=bt709\""
        ];
    }

    private static string Or(string value, string fallback) =>
        string.IsNullOrEmpty(value) ? fallback : value;
}
