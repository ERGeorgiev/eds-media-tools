using EdsMediaArchiver.Helpers;
using EdsMediaArchiver.Models;

namespace EdsMediaArchiver.Services.Converters;

/// <summary>
/// Renames files to ensure their extension matches their actual type.
/// </summary>
public class TrueExtensionConverter : IMediaConverter
{
    public bool IsSupported(string actualType) => Constants.FileTypeToExtension.ContainsKey(actualType);

    public Task<ProcessingResult?> ConvertAsync(ArchiveRequest request)
    {
        try
        {
            if (FixExtension(request))
            {
                return Task.FromResult<ProcessingResult?>(new ProcessingResult(request.NewPath.Relative, null, ProcessingStatus.Renamed));
            }
        }
        catch { }
        return Task.FromResult<ProcessingResult?>(null);
    }

    private static bool FixExtension(ArchiveRequest request)
    {
        if (Constants.FileTypeToExtension.TryGetValue(request.ActualFileType, out var correctExt) == false)
            return false;
        if (Constants.ExtensionToFileType.TryGetValue(correctExt, out var correctExtFileType) == false)
            return false;
        var currentExt = Path.GetExtension(request.NewPath.Absolute);
        if (Constants.ExtensionToFileType.TryGetValue(currentExt, out var currentExtFileType) == false)
            return false;

        if (currentExtFileType.Equals(correctExtFileType, StringComparison.OrdinalIgnoreCase))
            return false;

        var oldPath = request.NewPath.Absolute;
        var newPath = Path.ChangeExtension(oldPath, correctExt);
        newPath = FileHelpers.GetUniqueFilePath(newPath);

        File.Move(oldPath, newPath);
        request.NewPath = new PathInfo(request.NewPath.Root, newPath);

        Console.WriteLine($"  [RENAME] {request.OriginalPath.Relative} -> {Path.GetFileName(newPath)} (actual type: {request.ActualFileType})");
        return true;
    }
}
