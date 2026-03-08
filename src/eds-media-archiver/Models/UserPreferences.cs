namespace EdsMediaArchiver.Models;

public record UserPreferences
{
    public bool Compress { get; set; } = false;
    public bool ResizeOnCompress { get; set; } = false;
    public bool Standardize { get; set; } = false;
    public bool SetDates { get; set; } = false;
}
