using EdsMediaArchiver.Definitions;
using EdsMediaArchiver.Services.Compressors;

namespace EdsMediaArchiver.Services.Processors;

public interface ICompressProcessor
{
    /// <summary>
    /// Fixes the file extension and/or compresses the file based on request flags.
    /// Returns null if no action was taken.
    /// </summary>
    Task<bool> ProcessAsync(string sourcePath);
}

public class CompressProcessor(IEnumerable<IMediaCompressor> compressors) : ICompressProcessor
{
    public async Task<bool> ProcessAsync(string sourcePath)
    {
        if (ExtensionsTypes.ExtensionToFileType.TryGetValue(Path.GetExtension(sourcePath), out var fileType) == false)
            throw new NotSupportedException($"File not supported");

        var compressor = compressors.FirstOrDefault(c => c.IsSupported(fileType)) ?? throw new NotSupportedException($"File not supported");
        if (compressor != null)
        {
            var outputDirectory = Path.GetDirectoryName(sourcePath) ?? throw new Exception("Directory cannot be empty");
            var outputPath = await compressor.CompressAsync(sourcePath, outputDirectory, fileType);

            if (string.Equals(sourcePath, outputPath, StringComparison.OrdinalIgnoreCase) == false)
            {
                if (Path.GetExtension(sourcePath).Equals(Path.GetExtension(outputPath), StringComparison.OrdinalIgnoreCase))
                {
                    // Output has same extension, so just overwrite the source file.
                    File.Move(outputPath, sourcePath, overwrite: true);
                }
                else
                {
                    // Output has diff extension, so delete the source file to replace it.
                    File.Delete(sourcePath);
                }
                return true;
            }
        }
        return false;
    }
}
