namespace EdsMediaArchiver.Services.FileDateReaders;

public interface IFileDateReader
{
    DateTimeOffset? Read(FileInfo fileInfo, IEnumerable<MetadataExtractor.Directory> fileDirectories);
}
