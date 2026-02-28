namespace EdsMediaArchiver.Models;

public record PathInfo(string Root, string Absolute)
{
    public readonly string Relative = Path.GetRelativePath(Root, Absolute);
}
