using EdsMediaArchiver.Services.Converters;
using EdsMediaArchiver.Services.Logging;

namespace EdsMediaArchiver.Services.Processors;

public interface IConvertProcessor
{
    /// <summary>
    /// Fixes the file extension and/or compresses the file based on request flags.
    /// Returns null if no action was taken.
    /// </summary>
    Task<string> ProcessAsync(string sourcePath, string outputDirectory, string actualType);
}

public class ConvertProcessor(IEnumerable<IMediaConverter> converters, IProcessLogger processLogger) : IConvertProcessor
{
    public async Task<string> ProcessAsync(string sourcePath, string outputDirectory, string actualType)
    {
        var converter = converters.FirstOrDefault(c => c.IsSupported(actualType));
        if (converter != null)
        {
            var outputPath = await converter.ConvertAsync(sourcePath, outputDirectory, actualType);

            // Delete the original file (converter creates a new one)
            if (string.Equals(sourcePath, outputPath, StringComparison.OrdinalIgnoreCase))
            {
                processLogger.Log(IProcessLogger.Operation.Convert, IProcessLogger.Result.Skip, sourcePath, $"");
                return outputPath;
            }

            File.Delete(sourcePath);
            processLogger.Log(IProcessLogger.Operation.Convert, IProcessLogger.Result.Success, sourcePath, $"Converted To: {outputPath}");
            return outputPath;
        }
        return sourcePath;
    }
}
