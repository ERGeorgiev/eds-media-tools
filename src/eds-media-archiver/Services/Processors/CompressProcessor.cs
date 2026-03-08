using EdsMediaArchiver.Services.Compressors;
using EdsMediaArchiver.Services.Logging;

namespace EdsMediaArchiver.Services.Processors;

public interface ICompressProcessor
{
    /// <summary>
    /// Fixes the file extension and/or compresses the file based on request flags.
    /// Returns null if no action was taken.
    /// </summary>
    Task<string> ProcessAsync(string sourcePath, string outputDirectory, string fileType, CompressorMode compressorMode);
}

public class CompressProcessor(IEnumerable<IMediaCompressor> compressors, IProcessLogger processLogger) : ICompressProcessor
{
    public async Task<string> ProcessAsync(string sourcePath, string outputDirectory, string fileType, CompressorMode compressorMode)
    {
        var compressor = compressors.FirstOrDefault(c => c.IsSupported(fileType));
        if (compressor != null)
        {
            var outputPath = await compressor.CompressAsync(sourcePath, outputDirectory, fileType, compressorMode);

            if (string.Equals(sourcePath, outputPath, StringComparison.OrdinalIgnoreCase))
            {
                // Output == Input, so SKIPPED is the only possible case.
                processLogger.Log(IProcessLogger.Operation.Convert, IProcessLogger.Result.SKIPPED, sourcePath, "");
                return outputPath;
            }
            else
            {
                File.Delete(sourcePath);
                File.Move(outputPath, sourcePath, overwrite: true);
                if (compressorMode == CompressorMode.Convert)
                {
                    processLogger.Log(IProcessLogger.Operation.Convert, IProcessLogger.Result.SUCCESS, sourcePath, $"{outputPath,-60}");
                }
                else
                {
                    processLogger.Log(IProcessLogger.Operation.Compress, IProcessLogger.Result.SUCCESS, sourcePath, $"{outputPath,-60}");
                }
                return outputPath;
            }
        }
        return sourcePath;
    }
}
