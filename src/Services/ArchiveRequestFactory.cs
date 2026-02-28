using EdsMediaArchiver.Models;
using EdsMediaArchiver.Services.Resolvers;
using MetadataExtractor;

namespace EdsMediaArchiver.Services;

public interface IArchiveRequestFactory
{
    ArchiveRequest Create(string rootPath, string filePath);
}

public class ArchiveRequestFactory(IFileDateResolver dateResolver, IFileTypeResolver fileTypeService) : IArchiveRequestFactory
{
    public ArchiveRequest Create(string rootPath, string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var fileType = fileTypeService.GetFileType(rootPath);
        var metadataDirectories = ImageMetadataReader.ReadMetadata(fileInfo.FullName);
        var originDate = dateResolver.ResolveBestDate(fileInfo, metadataDirectories);
        var req = new ArchiveRequest(rootPath, fileInfo, fileType, metadataDirectories)
        {
            OriginDate = originDate
        };
        return req;
    }
}
