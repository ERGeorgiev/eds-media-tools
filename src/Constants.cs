namespace EdsMediaArchiver;

/// <summary>
/// File type classifications, extension mappings, and filename date patterns.
/// </summary>
public static partial class Constants
{
    public static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".mp4", ".mov", ".heic", ".heif",
        ".bmp", ".gif", ".webp", ".avi", ".mkv", ".wmv", ".3gp",
        ".tiff", ".tif"
    };

    /// <summary>Map from detected file type to correct extension (for fixing mislabeled files).</summary>
    public static readonly Dictionary<string, string> FileTypeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [MediaType.Jpeg] = ".jpg",
        [MediaType.Png] = ".png",
        [MediaType.Heic] = ".heic",
        [MediaType.Heif] = ".heif",
        [MediaType.Webp] = ".webp",
        [MediaType.Bmp] = ".bmp",
        [MediaType.Gif] = ".gif",
        [MediaType.Tiff] = ".tiff",
        [MediaType.Mp4] = ".mp4",
        [MediaType.Mov] = ".mov",
        [MediaType.Avi] = ".avi",
        [MediaType.Mkv] = ".mkv",
        [MediaType.Wmv] = ".wmv",
        [MediaType.ThreeGp] = ".3gp",
        [MediaType.QuickTime] = ".mov"
    };

    /// <summary>Map from detected file type to correct extension (for fixing mislabeled files).</summary>
    public static readonly Dictionary<string, string> ExtensionToFileType = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = MediaType.Jpeg,
        [".jpeg"] = MediaType.Jpeg,
        [".png"] = MediaType.Png,
        [".heic"] = MediaType.Heic,
        [".heif"] = MediaType.Heif,
        [".webp"] = MediaType.Webp,
        [".bmp"] = MediaType.Bmp,
        [".gif"] = MediaType.Gif,
        [".tiff"] = MediaType.Tiff,
        [".tif"] = MediaType.Tiff,
        [".mp4"] = MediaType.Mp4,
        [".mov"] = MediaType.Mov,
        [".avi"] = MediaType.Avi,
        [".mkv"] = MediaType.Mkv,
        [".wmv"] = MediaType.Wmv,
        [".3gp"] = MediaType.ThreeGp
    };
}
