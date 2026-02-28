using EdsMediaArchiver.Models;
using EdsMediaArchiver.Services.Compressors;

namespace EdsMediaArchiver.Services.Processors;

public interface IConvertProcessor
{
    /// <summary>
    /// Fixes the file extension and/or compresses the file based on request flags.
    /// Returns null if no action was taken.
    /// </summary>
    Task<ProcessingResult?> ProcessAsync(ArchiveRequest request);
}

public class ConvertProcessor(IEnumerable<IMediaCompressor> compressors) : IConvertProcessor
{
    public async Task<ProcessingResult?> ProcessAsync(ArchiveRequest request)
    {
        var actualType = request.ActualFileType;
        bool wasRenamed = false;

        if (request.Compress)
        {
            var compressor = compressors.FirstOrDefault(c => c.IsSupported(actualType));
            if (compressor != null)
            {
                var result = await CompressFileAsync(request, compressor);
                if (result != null)
                    return result;
            }
        }

        // 3. If only extension was fixed, report that
        if (wasRenamed)
            return new ProcessingResult(request.NewPath.Relative, null, ProcessingStatus.Renamed);

        return null;
    }

    private static async Task<ProcessingResult?> CompressFileAsync(ArchiveRequest request, IMediaCompressor compressor)
    {
        var filePath = request.NewPath.Absolute;
        var rootPath = request.NewPath.Root;
        var relativePath = request.NewPath.Relative;
        var outputDir = Path.GetDirectoryName(filePath)!;

        var outputPath = await compressor.CompressAsync(filePath, outputDir);
        if (outputPath == null)
        {
            Console.WriteLine($"  [ERR] {relativePath} - compression failed");
            return new ProcessingResult(relativePath, null, ProcessingStatus.Error, "Compression failed");
        }

        // Delete the original file (compressor creates a new one)
        if (!string.Equals(filePath, outputPath, StringComparison.OrdinalIgnoreCase))
            File.Delete(filePath);

        var outputRelative = Path.GetRelativePath(rootPath, outputPath);
        Console.WriteLine($"  [CONV] {relativePath} -> {outputRelative}");
        request.NewPath = new PathInfo(rootPath, outputPath);
        return new ProcessingResult(relativePath, null, ProcessingStatus.Converted);
    }
}
