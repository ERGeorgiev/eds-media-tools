using EdsMediaArchiver.Services;
using MetadataExtractor;

namespace EdsMediaArchiver.Models;

public class FileProcessingRequest(FileInfo fileInfo)
{
    public static FileProcessingRequest Create(DateResolver dateResolver, FileInfo fileInfo)
    {
        var originDate = dateResolver.ResolveBestDate(fileInfo.FullName);
        var req = new FileProcessingRequest(fileInfo)
        {
            OriginDate = originDate
        };
        return req;
    }

    public string OriginalFilePath { get; private set; } = fileInfo.FullName;
    public FileInfo FileInfo { get; private set; } = fileInfo;
    public IReadOnlyList<MetadataExtractor.Directory> MetadataDirectory = ImageMetadataReader.ReadMetadata(fileInfo.FullName);

    public DateTimeOffset? OriginDate { get; private set; }
    public string? ShouldRenameTo { get; private set; }
}
