using EdsMediaDateRestorer.Services.Validators;
using System.Globalization;

namespace EdsMediaDateRestorer.Services.FileDateReaders;

public interface IOriginalDateReader : IFileDateReader { }

public class OriginalDateReader(IDateValidator dateValidator) : IOriginalDateReader
{
    private static readonly string[] Tags =
    [
        "EXIF:DateTimeOriginal",
        "XMP-exif:DateTimeOriginal",
    ];

    public DateTimeOffset? Read(string filePath, Dictionary<string, string> tags)
    {
        foreach (var tag in Tags)
        {
            if (tags.TryGetValue(tag, out var value)
                && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out var parsed)
                && dateValidator.IsValid(parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}
