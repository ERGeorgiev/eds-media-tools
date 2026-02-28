using EdsMediaArchiver.Models;
using EdsMediaArchiver.Services.Resolvers;

namespace EdsMediaArchiver.Services.Processors;

public interface IMediaFileProcessor
{
    Task<ProcessingResult> ProcessFileAsync(ArchiveRequest request);
}

/// <summary>
/// Orchestrates file processing by delegating to specialized processors
/// based on the user's preferences set on the request.
/// </summary>
public class MediaFileProcessor(
    IFileTypeResolver fileTypeService,
    IExtensionFixProcessor extensionFixProcessor,
    ICompressProcessor compressProcessor,
    IDateProcessor dateProcessor) : IMediaFileProcessor
{
    public async Task<ProcessingResult> ProcessFileAsync(ArchiveRequest request)
    {
        try
        {
            var actualType = fileTypeService.GetFileType(request.NewPath.Absolute);
            var wasRenamed = false;

            // Step 1: Fix extension if requested
            if (request.FixExtension)
            {
                wasRenamed = extensionFixProcessor.Process(request, actualType);
                if (wasRenamed)
                    actualType = fileTypeService.GetFileType(request.NewPath.Absolute);
            }

            // Step 2: Compress if requested and file is XMP-only
            if (request.Compress && MediaType.XmpOnlyTypes.Contains(actualType))
            {
                if (!request.OriginDate.HasValue)
                {
                    Console.WriteLine($"  [SKIP] {request.NewPath.Relative} - no valid dates found for compression");
                    return new ProcessingResult(request.NewPath.Relative, null, ProcessingStatus.Skipped, "No valid dates found");
                }

                return await compressProcessor.ProcessAsync(request);
            }

            // Step 3: Set dates if requested
            if (request.SetDates)
            {
                if (!request.OriginDate.HasValue)
                {
                    Console.WriteLine($"  [SKIP] {request.NewPath.Relative} - no valid dates found");
                    return new ProcessingResult(request.NewPath.Relative, null, ProcessingStatus.Skipped, "No valid dates found");
                }

                return await dateProcessor.ProcessAsync(request, actualType);
            }

            // Step 4: If only extension was fixed
            if (wasRenamed)
                return new ProcessingResult(request.NewPath.Relative, null, ProcessingStatus.Renamed);

            // Nothing applicable
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
