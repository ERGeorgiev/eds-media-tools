using EdsMediaArchiver.Definitions;
using EdsMediaArchiver.Services.Resolvers;

namespace EdsMediaArchiver.Services.Writers;

public interface IFileDateWriter
{
    /// </summary>
    Task<DateTimeOffset?> WriteDateToFileAsync(string filePath, string fileType, DateTimeOffset date);
}

public class FileDateWriter(
    IMetadataWriter metadataWriter, 
    IFileTypeResolver fileTypeResolver) : IFileDateWriter
{
    public async Task<DateTimeOffset?> WriteDateToFileAsync(string filePath, string fileType, DateTimeOffset date)
    {
        var currentType = fileTypeResolver.GetActualFileType(filePath);

        await WriteDateForTypeAsync(filePath, currentType, date);

        return date;
    }

    private async Task WriteDateForTypeAsync(string filePath, string actualType, DateTimeOffset date)
    {
        if (MediaType.SupportedImageTypes.Contains(actualType))
            await metadataWriter.WriteImageDatesAsync(filePath, date);
        else if (MediaType.SupportedVideoTypes.Contains(actualType))
            metadataWriter.WriteVideoDates(filePath, date);
        else if (MediaType.SupportedAudioTypes.Contains(actualType))
            metadataWriter.WriteAudioDates(filePath, date);

        File.SetCreationTime(filePath, date.LocalDateTime);
        File.SetLastWriteTime(filePath, date.LocalDateTime);
    }
}
