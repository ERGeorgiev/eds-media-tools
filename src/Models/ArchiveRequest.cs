namespace EdsMediaArchiver.Models;

public class ArchiveRequest(string rootPath, FileInfo fileInfo, string actualFileType, IReadOnlyList<MetadataExtractor.Directory> directories)
{
    public FilePathInfo OriginalPath { get; } = new(rootPath, fileInfo.FullName);
    public FilePathInfo NewPath { get; set; } = new(rootPath, fileInfo.FullName);
    public FileInfo FileInfo { get; } = fileInfo;
    public string ActualFileType { get; } = actualFileType;
    public IReadOnlyList<MetadataExtractor.Directory> MetadataDirectories { get; } = directories;

    public DateTimeOffset? OriginDate { get; set; }
    public bool Compress { get; set; }
    public bool SetDates { get; set; }
    public bool ConvertIfUnreliableForDates { get; set; }
}
