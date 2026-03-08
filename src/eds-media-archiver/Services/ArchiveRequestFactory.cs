using EdsMediaArchiver.Models;
using EdsMediaArchiver.Services.Resolvers;

namespace EdsMediaArchiver.Services;

public interface IArchiveRequestFactory
{
    ArchiveRequest Create(string rootPath, string filePath);
}

public class ArchiveRequestFactory(IFileTypeResolver fileTypeService) : IArchiveRequestFactory
{
    public ArchiveRequest Create(string rootPath, string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var actualFileType = fileTypeService.GetActualFileType(filePath);
        return new ArchiveRequest(rootPath, fileInfo, actualFileType);
    }
}
