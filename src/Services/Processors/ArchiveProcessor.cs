using EdsMediaArchiver.Models;

namespace EdsMediaArchiver.Services.Processors;

public interface IArchiveProcessor
{
    Task<ProcessingResult> ProcessFileAsync(ArchiveRequest request);
}

/// <summary>
/// Orchestrates file processing by delegating to specialized processors
/// based on the user's preferences set on the request.
/// </summary>
public class ArchiveProcessor(
    ICompressProcessor compressProcessor,
    IDateProcessor dateProcessor) : IArchiveProcessor
{
    public async Task<ProcessingResult> ProcessFileAsync(ArchiveRequest request)
    {
        try
        {
            ProcessingResult? compressResult = null;

            // 1. Compress
            if (request.Compress)
                compressResult = await compressProcessor.ProcessAsync(request);

            // 2. Date
            if (request.SetDates)
            {
                var dateResult = await dateProcessor.ProcessAsync(request);

                // If compressed and dates set, prefer Converted status but include the date
                if (compressResult?.Status == ProcessingStatus.Converted)
                    return new ProcessingResult(compressResult.RelativePath, dateResult.DateAssigned, ProcessingStatus.Converted);

                return dateResult;
            }

            // Return compress/rename result if no date processing
            if (compressResult != null)
                return compressResult;

            Console.WriteLine($"  [SKIP] {request.NewPath.Relative} - no applicable processing");
            return new ProcessingResult(request.NewPath.Relative, null, ProcessingStatus.Skipped, "No applicable processing");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [ERR] {request.NewPath.Relative} - {ex.Message}");
            return new ProcessingResult(request.NewPath.Relative, null, ProcessingStatus.Error, ex.Message);
        }
    }
}
