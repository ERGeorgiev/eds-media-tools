using EdsMediaArchiver.Definitions;
using EdsMediaArchiver.Helpers;
using TagLib;

namespace EdsMediaArchiver.Services.Resolvers;

public interface IExtensionRestorer
{
    bool IsSupported(string actualType);
    Task<string> RestoreExtension(string sourcePath, string outputDirectory, string actualType);
}

/// <summary>
/// Renames files to ensure their extension matches their actual type.
/// </summary>
public class ExtensionResolver : IExtensionRestorer
{
    public bool IsSupported(string actualType) => ExtensionsTypes.FileTypeToExtension.ContainsKey(actualType);

    public Task<string> RestoreExtension(string sourcePath, string outputDirectory, string actualType)
    {
        if (ExtensionsTypes.FileTypeToExtension.TryGetValue(actualType, out var correctExt) == false)
            throw new UnsupportedFormatException($"File '{sourcePath}' with type '{actualType}' is not supported.");
        if (ExtensionsTypes.ExtensionToFileType.TryGetValue(correctExt, out var correctExtFileType) == false)
            throw new UnsupportedFormatException($"File '{sourcePath}' with type '{actualType}' is not supported.");
        var currentExt = Path.GetExtension(sourcePath);
        if (ExtensionsTypes.ExtensionToFileType.TryGetValue(currentExt, out var currentExtFileType) == false)
            throw new UnsupportedFormatException($"File '{sourcePath}' with type '{actualType}' is not supported.");

        if (currentExtFileType.Equals(correctExtFileType, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(sourcePath);

        var oldPath = sourcePath;
        var newPath = Path.ChangeExtension(oldPath, correctExt);
        newPath = FileHelper.GetUniqueFilePath(newPath);

        System.IO.File.Move(oldPath, newPath);

        Console.WriteLine($"  [RENAME] {oldPath} -> {Path.GetFileName(newPath)} (actual type: {actualType})");
        return Task.FromResult(newPath);
    }
}
