namespace EdsMediaArchiver.Models;

public interface IUserPreferences
{
    bool AudioToMp4 { get; }
    bool ResizeOnCompress { get; }
    bool Standardize { get; }
}

public record UserPreferences : IUserPreferences
{
    public bool ResizeOnCompress { get; set; } = false;
    public bool Standardize { get; set; } = false;
    public bool AudioToMp4 { get; set; } = false;
}
