using EdsMediaArchiver.Models;
using EdsMediaArchiver.Services.Resolvers;
using MetadataExtractor;

namespace EdsMediaArchiver.Services;

public interface IArchiveRequestFactory
{
    ArchiveRequest Create(string rootPath, string filePath, UserPreferences preferences);
}

public class ArchiveRequestFactory(IFileDateResolver dateResolver, IFileTypeResolver fileTypeService) : IArchiveRequestFactory
{
    public ArchiveRequest Create(string rootPath, string filePath, UserPreferences preferences)
    {
        var fileInfo = new FileInfo(filePath);
        var fileType = fileTypeService.GetFileType(filePath);
        var metadataDirectories = ImageMetadataReader.ReadMetadata(fileInfo.FullName);
        var originDate = dateResolver.ResolveBestDate(fileInfo, metadataDirectories);
        return new ArchiveRequest(rootPath, fileInfo, fileType, metadataDirectories)
        {
            OriginDate = originDate,
            FixExtension = preferences.FixExtensions,
            Compress = preferences.Compress,
            SetDates = preferences.SetDates
        };
    }
}
