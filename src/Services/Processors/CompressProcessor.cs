using EdsMediaArchiver.Models;
using EdsMediaArchiver.Services.Converters;

namespace EdsMediaArchiver.Services.Processors;

public interface ICompressProcessor
{
    /// <summary>
    /// Converts XMP-only format files to JPG and writes dates to the new file.
    /// Deletes the original on success. Updates request.NewPath.
    /// </summary>
    Task<ProcessingResult> ProcessAsync(ArchiveRequest request);
}

public class CompressProcessor(IImageConverter imageConverter, IMetadataWriter metadataWriter) : ICompressProcessor
{
    public async Task<ProcessingResult> ProcessAsync(ArchiveRequest request)
    {
        var filePath = request.NewPath.Absolute;
        var bestDate = request.OriginDate!.Value;
        var rootPath = request.NewPath.Root;
        var relativePath = request.NewPath.Relative;

        var jpgPath = Path.ChangeExtension(filePath, ".jpg");
        if (File.Exists(jpgPath) && !string.Equals(jpgPath, filePath, StringComparison.OrdinalIgnoreCase))
            jpgPath = GetUniqueFilePath(jpgPath, "-converted");

        var success = await imageConverter.ConvertToJpgAsync(filePath, jpgPath);
        if (!success)
        {
            Console.WriteLine($"  [ERR] {relativePath} - Magick.NET conversion failed, falling back to XMP");
            await metadataWriter.WriteXmpDatesAsync(filePath, bestDate);
            SetFilesystemDates(filePath, bestDate);
            return new ProcessingResult(relativePath, bestDate, ProcessingStatus.Fixed);
        }

        // Write EXIF dates into the new JPG
        await metadataWriter.WriteExifDatesAsync(jpgPath, bestDate);
        SetFilesystemDates(jpgPath, bestDate);

        // Delete original (conversion succeeded)
        File.Delete(filePath);

        var jpgRelative = Path.GetRelativePath(rootPath, jpgPath);
        Console.WriteLine($"  [CONV] {relativePath} -> {jpgRelative} ({bestDate:yyyy-MM-dd HH:mm:ss})");
        request.NewPath = new PathInfo(rootPath, jpgPath);
        return new ProcessingResult(relativePath, bestDate, ProcessingStatus.Converted);
    }

    private static void SetFilesystemDates(string filePath, DateTimeOffset date)
    {
        File.SetCreationTime(filePath, date.LocalDateTime);
        File.SetLastWriteTime(filePath, date.LocalDateTime);
    }

    private static string GetUniqueFilePath(string path, string suffix)
    {
        if (!File.Exists(path)) return path;

        var dir = Path.GetDirectoryName(path)!;
        var baseName = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        var counter = 1;

        string candidate;
        do
        {
            candidate = Path.Combine(dir, $"{baseName}{suffix}{counter}{ext}");
            counter++;
        } while (File.Exists(candidate));

        return candidate;
    }
}
