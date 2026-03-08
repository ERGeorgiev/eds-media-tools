namespace EdsMediaArchiver.Models;

public record FilePathInfo(string Root, string Absolute)
{
    public readonly string Relative = Path.GetRelativePath(Root, Absolute);
    public readonly string Directory = Path.GetDirectoryName(Absolute)!;
    public readonly string FileNameWithoutExtension = Path.GetFileNameWithoutExtension(Absolute);
    public readonly string Extension = Path.GetExtension(Absolute);
}
