namespace EdsMediaArchiver.Models;

public class ArchiveRequest(string rootPath, FileInfo fileInfo, string fileType, IReadOnlyList<MetadataExtractor.Directory> directories)
{
    public PathInfo OriginalPath { get; } = new(rootPath, fileInfo.FullName);
    public PathInfo NewPath { get; set; } = new(rootPath, fileInfo.FullName);
    public FileInfo FileInfo { get; } = fileInfo;
    public string FileType { get; } = fileType;
    public IReadOnlyList<MetadataExtractor.Directory> MetadataDirectories { get; } = directories;

    public DateTimeOffset? OriginDate { get; set; }
    public bool Compress { get; set; } // ToDo: Info that it enabled setting EXIF for unsupported file types
    public bool FixExtension { get; set; }
    public bool SetDates { get; set; } // ToDo: Maybe part of COmpress as technically when compressing dates have to be set? Or maybe not as Compress can auto set them?

    // ToDo: Ask about the bools if the user wants those to be done. Each to be able to be done separately
}
