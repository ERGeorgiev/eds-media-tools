using EdsMediaArchiver.Services;
using MetadataExtractor;

namespace EdsMediaArchiver.Models;

public interface IFileProcessingRequestFactory
{
    FileProcessingRequest Create(string rootPath, string filePath);
}

public class FileProcessingRequestFactory(IDateResolver dateResolver, IFileTypeService fileTypeService) : IFileProcessingRequestFactory
{
    public FileProcessingRequest Create(string rootPath, string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var fileType = fileTypeService.GetFileType(rootPath);
        var metadataDirectories = ImageMetadataReader.ReadMetadata(fileInfo.FullName);
        var originDate = dateResolver.ResolveBestDate(fileInfo, metadataDirectories);
        var req = new FileProcessingRequest(rootPath, fileInfo, fileType, metadataDirectories)
        {
            OriginDate = originDate
        };
        return req;
    }
}
