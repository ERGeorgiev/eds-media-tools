using EdsMediaArchiver.Models;
using EdsMediaArchiver.Services.Compressors;

namespace EdsMediaArchiver.Services.Processors;

public interface ICompressProcessor
{
    /// <summary>
    /// Fixes the file extension and/or compresses the file based on request flags.
    /// Returns null if no action was taken.
    /// </summary>
    Task<ProcessingResult?> ProcessAsync(ArchiveRequest request);
}

public class CompressProcessor(IEnumerable<IMediaCompressor> compressors) : ICompressProcessor
{
    public async Task<ProcessingResult?> ProcessAsync(ArchiveRequest request)
    {
        try
        {
            var compressor = compressors.FirstOrDefault(c => c.IsSupported(request.ActualFileType));
            if (compressor != null)
            {
                return await CompressFileAsync(request, compressor);
            }
        }
        catch (Exception e)
        {
            return new ProcessingResult(request.NewPath.Absolute, null, ProcessingStatus.Error, e.Message);
        }

        return null;
    }

    private static async Task<ProcessingResult> CompressFileAsync(ArchiveRequest request, IMediaCompressor compressor)
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
