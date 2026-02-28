using EdsMediaArchiver.Models;
using EdsMediaArchiver.Services.Converters;

namespace EdsMediaArchiver.Services.Processors;

public interface IMediaFileProcessor
{
    Task<ProcessingResult> ProcessFileAsync(FileProcessingRequest request);
}

/// <summary>
/// Processes a single media file: detects type, fixes extension, resolves best date,
/// converts XMP-only formats if needed, and writes metadata + filesystem dates.
/// </summary>
public class MediaFileProcessor(IImageConverter imageConverter, IFileTypeService fileTypeService, IMetadataWriter metadataWriter) : IMediaFileProcessor
{
    public async Task<ProcessingResult> ProcessFileAsync(FileProcessingRequest request)
    {
        try
        {
            // Detect actual file type via magic bytes (not just the extension)
            var actualType = fileTypeService.GetFileType(request.OriginalPath.Absolute);

            // Fix mislabeled extension if needed
            //var filePath = FixExtension(request.OriginalPath.Absolute, actualType, request.OriginalPath.Root, ref relativePath);

            // Determine the best available date
            if (request.OriginDate.HasValue == false)
            {
                Console.WriteLine($"  [SKIP] {request.NewPath.Relative} - no valid dates found");
                return new ProcessingResult(request.NewPath.Relative, null, ProcessingStatus.Skipped, "No valid dates found");
            }

            // XMP-only formats: convert to JPG via Magick.NET
            if (MediaType.XmpOnlyTypes.Contains(actualType))
                return await ConvertAndWriteDateAsync(request.NewPath.Absolute, request.OriginDate.Value, request.NewPath.Root, request.NewPath.Relative);

            // Write date metadata based on file type
            await WriteDateForTypeAsync(request.NewPath.Absolute, actualType, request.OriginDate.Value);

            // Set filesystem Created/Modified dates
            SetFilesystemDates(request.NewPath.Absolute, request.OriginDate.Value);

            Console.WriteLine($"  [OK] {request.NewPath.Relative} -> {request.OriginDate:yyyy-MM-dd HH:mm:ss}");
            return new ProcessingResult(request.NewPath.Relative, request.OriginDate, ProcessingStatus.Fixed);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [ERR] {request.NewPath.Relative} - {ex.Message}");
            return new ProcessingResult(request.NewPath.Relative, null, ProcessingStatus.Error, ex.Message);
        }
    }

    private async Task<ProcessingResult> ConvertAndWriteDateAsync(
        string filePath, DateTimeOffset bestDate, string rootPath, string relativePath)
    {
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
        return new ProcessingResult(relativePath, bestDate, ProcessingStatus.Converted);
    }

    private static string FixExtension(string filePath, string actualType, string rootPath, ref string relativePath)
    {
        if (!Constants.FileTypeToExtension.TryGetValue(actualType, out var correctExt))
            return filePath;

        var currentExt = Path.GetExtension(filePath);
        var normCurrent = NormalizeExtension(currentExt);
        var normCorrect = NormalizeExtension(correctExt);

        if (normCurrent.Equals(normCorrect, StringComparison.OrdinalIgnoreCase))
            return filePath;

        var newPath = Path.ChangeExtension(filePath, correctExt);
        newPath = GetUniqueFilePath(newPath, "-renamed");

        File.Move(filePath, newPath);

        var newRelativePath = Path.GetRelativePath(rootPath, newPath);
        Console.WriteLine($"  [RENAME] {relativePath} -> {Path.GetFileName(newPath)} (actual type: {actualType})");
        relativePath = newRelativePath;
        return newPath;
    }

    private async Task WriteDateForTypeAsync(string filePath, string actualType, DateTimeOffset date)
    {
        if (MediaType.ExifWritableTypes.Contains(actualType))
            await metadataWriter.WriteExifDatesAsync(filePath, date);
        else if (MediaType.VideoTypes.Contains(actualType))
            metadataWriter.WriteVideoDates(filePath, date);
        else if (actualType.Equals("PNG", StringComparison.OrdinalIgnoreCase))
            await metadataWriter.WritePngDatesAsync(filePath, date);
        else
            // Unknown type — XMP as best effort
            await metadataWriter.WriteXmpDatesAsync(filePath, date);
    }

    private static void SetFilesystemDates(string filePath, DateTimeOffset date)
    {
        File.SetCreationTime(filePath, date.LocalDateTime);
        File.SetLastWriteTime(filePath, date.LocalDateTime);
    }

    private static string NormalizeExtension(string ext)
    {
        return ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ? ".jpg" : ext.ToLowerInvariant();
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
