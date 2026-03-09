using EdsMediaDateRestorer.Services.FileDateReaders;

namespace EdsMediaDateRestorer.Services.Resolvers;

// ToDo: Read metadata OffsetTime to get "+05:00" and take it into consideration

public interface IFileDateResolver
{
    Task<DateTimeOffset?> ResolveBestDate(string filePath);
}

/// <summary>
/// Determines the best date for a media file.
/// </summary>
public partial class FileDateResolver(
    IOriginalDateReader originalDateReader,
    IFilenameDateReader filenameDateReader,
    IExifToolService exif,
    IOldestDateReader oldestDateReader) : IFileDateResolver
{
    private readonly IEnumerable<IFileDateReader> _dateReaders = [originalDateReader, filenameDateReader, oldestDateReader];

    public async Task<DateTimeOffset?> ResolveBestDate(string filePath)
    {
        var dateTags = await exif.ReadDatesAsync(filePath);
        foreach (var reader in _dateReaders)
        {
            var date = reader.Read(filePath, dateTags);
            if (date.HasValue)
                return date;
        }
        return null;
    }
}
