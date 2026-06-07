using EdsMediaArchiver.Definitions;
using EdsMediaArchiver.Helpers;
using TagLib;
using File = System.IO.File;

namespace EdsMediaArchiver.Services.Resolvers;

public interface IFileExtensionResolver
{
    bool IsSupported(string actualType);
    Task<string> RestoreExtension(string sourcePath);
}

/// <summary>
/// Renames files to ensure their extension matches their actual type.
/// </summary>
public class FileExtensionResolver(IFileTypeResolver fileTypeResolver) : IFileExtensionResolver
{
    public bool IsSupported(string actualType) => ExtensionsTypes.FileTypeToExtension.ContainsKey(actualType);

    public Task<string> RestoreExtension(string sourcePath)
    {
        var actualType = fileTypeResolver.GetActualFileType(sourcePath);
        if (ExtensionsTypes.FileTypeToExtension.TryGetValue(actualType, out var correctExt) == false)
            throw new UnsupportedFormatException($"Filetype '{actualType}' is not supported.");
        if (ExtensionsTypes.ExtensionToFileType.TryGetValue(correctExt, out var correctExtFileType) == false)
            throw new UnsupportedFormatException($"Filetype '{actualType}' is not supported.");
        var currentExt = Path.GetExtension(sourcePath);
        if (ExtensionsTypes.ExtensionToFileType.TryGetValue(currentExt, out var currentExtFileType) == false)
            throw new UnsupportedFormatException($"Filetype '{actualType}' is not supported.");

        if (currentExtFileType.Equals(correctExtFileType, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(sourcePath);

        var oldPath = sourcePath;
        var lastWriteTime = File.GetLastWriteTimeUtc(oldPath);
        var creationTime = File.GetCreationTimeUtc(oldPath);
        var newPath = Path.ChangeExtension(oldPath, correctExt);
        newPath = FileHelper.GetUniqueFilePath(newPath);

        File.Move(oldPath, newPath);

        // Carry the original filesystem modification date onto the new file
        File.SetLastWriteTimeUtc(newPath, lastWriteTime);
        File.SetCreationTimeUtc(newPath, creationTime);

        return Task.FromResult(newPath);
    }
}
