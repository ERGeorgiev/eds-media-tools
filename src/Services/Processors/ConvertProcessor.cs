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
        try
        {
            var actualType = request.ActualFileType;
            if (request.Compress)
            {
                var compressor = compressors.FirstOrDefault(c => c.IsSupported(actualType));
                if (compressor != null)
                {
                    return await ConvertFileAsync(request, compressor);
                }
            }
        }
        catch (Exception e)
        {
            return new ProcessingResult(request.NewPath.Absolute, null, ProcessingStatus.Error, e.Message);
        }

        return null;
    }

    private static async Task<ProcessingResult> ConvertFileAsync(ArchiveRequest request, IMediaCompressor compressor)
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
        request.NewPath = new FilePathInfo(rootPath, outputPath);
        return new ProcessingResult(relativePath, null, ProcessingStatus.Converted);
    }
}
