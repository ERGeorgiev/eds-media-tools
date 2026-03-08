using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace EdsMediaDateRestorer.Services;

public interface IExifToolService
{
    Task WriteDatesAsync(string filePath, DateTimeOffset date);
    Task<Dictionary<string, string>> ReadDatesAsync(string filePath);
}

public class ExifToolService : IExifToolService
{
    public async Task<Dictionary<string, string>> ReadDatesAsync(string filePath)
    {
        var args = new List<string>
    {
        "-json",
        "-G",
        "-dateFormat", "%Y-%m-%dT%H:%M:%S%z",
        "-EXIF:DateTimeOriginal",
        "-EXIF:DateTimeDigitized",
        "-EXIF:CreateDate",
        "-EXIF:ModifyDate",
        "-XMP-exif:DateTimeOriginal",
        "-XMP-exif:DateTimeDigitized",
        "-XMP-xmp:CreateDate",
        "-XMP-xmp:ModifyDate",
        "-QuickTime:CreateDate",
        "-QuickTime:ModifyDate",
        "-QuickTime:TrackCreateDate",
        "-QuickTime:TrackModifyDate",
        "-QuickTime:MediaCreateDate",
        "-QuickTime:MediaModifyDate",
        "-Keys:CreationDate",
        filePath
    };

        var output = await ExecuteAsync(args);
        var results = JsonSerializer.Deserialize<Dictionary<string, string>[]>(output);
        return results?.FirstOrDefault() ?? [];
    }

    public async Task WriteDatesAsync(string filePath, DateTimeOffset date)
    {
        var exifDate = date.ToString("yyyy:MM:dd HH:mm:ss");
        var exifOffset = date.ToString("zzz");
        var xmpDate = date.ToString("yyyy-MM-ddTHH:mm:ssK");
        var utcDate = date.UtcDateTime.ToString("yyyy:MM:dd HH:mm:ss");
        var pngDate = date.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");

        var args = new List<string>
        {
            "-f",
            "-m",
            "-overwrite_original",
            
            // EXIF dates (local time without offset)
            $"-EXIF:DateTimeOriginal={exifDate}",
            $"-EXIF:CreateDate={exifDate}",
            $"-EXIF:ModifyDate={exifDate}",

            // EXIF 2.31 timezone offset tags
            $"-EXIF:OffsetTimeOriginal={exifOffset}",
            $"-EXIF:OffsetTimeDigitized={exifOffset}",
            $"-EXIF:OffsetTime={exifOffset}",

            // XMP dates
            $"-XMP-xmp:CreateDate={xmpDate}",
            $"-XMP-xmp:ModifyDate={xmpDate}",
            $"-XMP-xmp:MetadataDate={xmpDate}",
            $"-XMP-exif:DateTimeOriginal={xmpDate}", //todo: this is exif but writes xmpDate? Which one should it be?
            $"-XMP-exif:DateTimeDigitized={xmpDate}",
            $"-XMP-tiff:DateTime={xmpDate}",

            // QuickTime dates (for MP4/MOV)
            $"-QuickTime:CreateDate={utcDate}",
            $"-QuickTime:ModifyDate={utcDate}",
            $"-QuickTime:TrackCreateDate={utcDate}",
            $"-QuickTime:TrackModifyDate={utcDate}",
            $"-QuickTime:MediaCreateDate={utcDate}",
            $"-QuickTime:MediaModifyDate={utcDate}",

            // Keys:CreationDate (local time with timezone, for Apple Photos / Google Photos compatibility)
            $"-Keys:CreationDate={xmpDate}",

            // PNG tIME chunk
            $"-PNG:ModifyDate={pngDate}",

            filePath
        };

        await ExecuteAsync(args);
    }

    private static async Task<string> ExecuteAsync(IReadOnlyList<string> args)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "exiftool",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            }
        };

        // Use -@ - to read args from stdin, with UTF-8 filename charset
        process.StartInfo.ArgumentList.Add("-charset");
        process.StartInfo.ArgumentList.Add("filename=utf8");
        process.StartInfo.ArgumentList.Add("-@");
        process.StartInfo.ArgumentList.Add("-");

        process.Start();

        // Write args to stdin as UTF-8, one per line
        await using (var writer = new StreamWriter(
            process.StandardInput.BaseStream, new UTF8Encoding(false)))
            foreach (var arg in args)
                await writer.WriteLineAsync(arg);

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"ExifTool failed (exit code {process.ExitCode}): {error.Trim()}");

        return output;
    }
}
