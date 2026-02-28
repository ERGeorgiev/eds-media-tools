using EdsMediaArchiver.Services.Validators;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using MetadataExtractor.Formats.Xmp;
using System.Globalization;

namespace EdsMediaArchiver.Services.FileDateReaders;

public interface IOldestDateReader : IFileDateReader { }

public partial class OldestDateReader(IDateValidator dateValidator) : IOldestDateReader
{
    /// <summary>
    /// Reads other trusted date tags: EXIF CreateDate/ModifyDate,
    /// QuickTime movie/track dates, and XMP dates.
    /// </summary>
    public DateTimeOffset? Read(FileInfo fileInfo, IEnumerable<MetadataExtractor.Directory> fileDirectories)
    {
        var dates = new List<DateTimeOffset>();
        try
        {
            // EXIF dates
            foreach (var dir in fileDirectories.OfType<ExifSubIfdDirectory>())
            {
                var date = TryGetDate(dir, ExifDirectoryBase.TagDateTimeDigitized);
                if (dateValidator.IsValid(date)) dates.Add(date!.Value);
            }
        }
        catch { }

        try
        {
            foreach (var dir in fileDirectories.OfType<ExifIfd0Directory>())
            {
                var date = TryGetDate(dir, ExifIfd0Directory.TagDateTime);
                if (dateValidator.IsValid(date)) dates.Add(date!.Value);
            }
        }
        catch { }

        try
        {
            // QuickTime movie header
            foreach (var dir in fileDirectories.OfType<QuickTimeMovieHeaderDirectory>())
            {
                var date = TryGetDate(dir, QuickTimeMovieHeaderDirectory.TagCreated);
                if (dateValidator.IsValid(date)) dates.Add(date!.Value);
                date = TryGetDate(dir, QuickTimeMovieHeaderDirectory.TagModified);
                if (dateValidator.IsValid(date)) dates.Add(date!.Value);
            }
        }
        catch { }

        try
        {
            // QuickTime track header
            foreach (var dir in fileDirectories.OfType<QuickTimeTrackHeaderDirectory>())
            {
                var date = TryGetDate(dir, QuickTimeTrackHeaderDirectory.TagCreated);
                if (dateValidator.IsValid(date)) dates.Add(date!.Value);
                date = TryGetDate(dir, QuickTimeTrackHeaderDirectory.TagModified);
                if (dateValidator.IsValid(date)) dates.Add(date!.Value);
            }
        }
        catch { }

        try
        {
            // XMP dates
            foreach (var dir in fileDirectories.OfType<XmpDirectory>())
            {
                var date = GetXmpDate(dir, "http://ns.adobe.com/xap/1.0/", "CreateDate");
                if (dateValidator.IsValid(date)) dates.Add(date!.Value);
                date = GetXmpDate(dir, "http://ns.adobe.com/xap/1.0/", "ModifyDate");
                if (dateValidator.IsValid(date)) dates.Add(date!.Value);
                date = GetXmpDate(dir, "http://ns.adobe.com/exif/1.0/", "DateTimeDigitized");
                if (dateValidator.IsValid(date)) dates.Add(date!.Value);
            }
        }
        catch { }

        try
        {
            if (dateValidator.IsValid(fileInfo.CreationTime)) dates.Add(fileInfo.CreationTime);
            if (dateValidator.IsValid(fileInfo.LastWriteTime)) dates.Add(fileInfo.LastWriteTime);
        }
        catch { }

        return dates.Count > 0 ? dates.Min() : null;
    }

    private DateTimeOffset? TryGetDate(MetadataExtractor.Directory dir, int tag)
    {
        if (dir.TryGetDateTime(tag, out var dt) && dateValidator.IsValid(dt))
        {
            return dt;
        }
        return null;
    }

    private static DateTimeOffset? GetXmpDate(XmpDirectory xmpDir, string ns, string property)
    {
        try
        {
            var value = xmpDir.XmpMeta?.GetPropertyString(ns, property);
            if (string.IsNullOrWhiteSpace(value)) return null;

            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out var parsed))
                return parsed;
        }
        catch { }
        return null;
    }
}
