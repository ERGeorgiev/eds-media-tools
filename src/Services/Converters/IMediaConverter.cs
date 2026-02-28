using EdsMediaArchiver.Models;

namespace EdsMediaArchiver.Services.Converters;

/// <summary>
/// A compressor that can convert specific media types to a more optimal format.
/// </summary>
public interface IMediaConverter
{
    /// <summary>Whether this compressor can handle the given detected file type.</summary>
    bool IsSupported(string actualType);

    /// <summary>
    /// Converts the source file to an optimal format.
    /// Returns the output file path, or null if compression failed.
    /// The caller is responsible for deleting the original file.
    /// </summary>
    Task<string> ConvertAsync(string sourcePath, string outputDirectory, string actualType);
}
