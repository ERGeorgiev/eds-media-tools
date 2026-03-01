using EdsMediaArchiver.Services.FileDateReaders;
using MetadataExtractor;

namespace EdsMediaArchiver.Services.Resolvers;

// ToDo: Read mmetadata OffsetTime to get "+05:00" and take it into consideration

public interface IFileDateResolver
{
    DateTimeOffset? ResolveBestDate(string filePath);
}

/// <summary>
/// Determines the best date for a media file.
/// </summary>
public partial class FileDateResolver(
    IOriginalDateReader originalDateReader,
    IFilenameDateReader filenameDateReader,
    IOldestDateReader oldestDateReader) : IFileDateResolver
{
    private readonly IEnumerable<IFileDateReader> _dateReaders = [originalDateReader, filenameDateReader, oldestDateReader];

    public DateTimeOffset? ResolveBestDate(string filePath)
    {
        var metadataDirectories = ImageMetadataReader.ReadMetadata(filePath);
        foreach (var reader in _dateReaders)
        {
            var date = reader.Read(filePath, metadataDirectories);
            if (date.HasValue)
                return date;
        }
        return null;
    }
}
