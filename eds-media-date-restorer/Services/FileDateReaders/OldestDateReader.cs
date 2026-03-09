using EdsMediaDateRestorer.Services.Validators;
using System.Globalization;

namespace EdsMediaDateRestorer.Services.FileDateReaders;

public interface IOldestDateReader : IFileDateReader { }

public partial class OldestDateReader(IDateValidator dateValidator) : IOldestDateReader
{
    private static readonly string[] Tags =
    [
        "EXIF:DateTimeDigitized",
        "EXIF:CreateDate",
        "EXIF:ModifyDate",
        "XMP-exif:DateTimeDigitized",
        "XMP-xmp:CreateDate",
        "XMP-xmp:ModifyDate",
        "QuickTime:CreateDate",
        "QuickTime:ModifyDate",
        "QuickTime:TrackCreateDate",
        "QuickTime:TrackModifyDate",
        "QuickTime:MediaCreateDate",
        "QuickTime:MediaModifyDate",
        "Keys:CreationDate",
    ];

    private static readonly DateTimeOffset QuickTimeEpoch =
        new(1904, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public DateTimeOffset? Read(string filePath, Dictionary<string, string> tags)
    {
        var dates = new List<DateTimeOffset>();

        foreach (var tag in Tags)
        {
            if (!tags.TryGetValue(tag, out var value))
                continue;

            if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out var parsed))
                continue;

            if (!dateValidator.IsValid(parsed))
                continue;

            // QuickTime stores dates relative to 1904-01-01 — skip if unset
            if (tag.StartsWith("QuickTime:") && parsed <= QuickTimeEpoch)
                continue;

            dates.Add(parsed);
        }

        // File system dates as fallback
        try
        {
            var fileInfo = new FileInfo(filePath);

            if (dateValidator.IsValid(fileInfo.CreationTime))
                dates.Add(fileInfo.CreationTime);

            if (dateValidator.IsValid(fileInfo.LastWriteTime))
                dates.Add(fileInfo.LastWriteTime);
        }
        catch { }

        return dates.Count > 0 ? dates.Min() : null;
    }
}
