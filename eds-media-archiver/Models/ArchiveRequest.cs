namespace EdsMediaArchiver.Models;

public class ArchiveRequest(string rootPath, FileInfo fileInfo, string actualFileType)
{
    public FilePathInfo OriginalPath { get; } = new(rootPath, fileInfo.FullName);
    public FilePathInfo NewPath { get; set; } = new(rootPath, fileInfo.FullName);
    public FileInfo FileInfo { get; } = fileInfo;
    public string ActualFileType { get; } = actualFileType;

    public bool Compress { get; set; }
    public bool ReizeOnCompress { get; set; }
    public bool Standardize { get; set; }
    public bool SetDates { get; set; }
}
