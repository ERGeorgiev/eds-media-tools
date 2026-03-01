using ImageMagick;

namespace EdsMediaArchiver.Services.Writers;

public interface IMetadataWriter
{
    Task WriteImageDatesAsync(string filePath, DateTimeOffset date);
    void WriteVideoDates(string filePath, DateTimeOffset date);
    void WriteAudioDates(string filePath, DateTimeOffset date);
}

public class MetadataWriter(IXmpWriter xmpWriter) : IMetadataWriter
{
    public async Task WriteImageDatesAsync(string filePath, DateTimeOffset date)
    {
        using var image = new MagickImage();
        await image.ReadAsync(filePath);

        var exifDate = date.ToString("yyyy:MM:dd HH:mm:ss");

        // Update EXIF
        var exif = image.GetExifProfile() ?? new ExifProfile();
        exif.SetValue(ExifTag.DateTimeOriginal, exifDate);
        exif.SetValue(ExifTag.DateTimeDigitized, exifDate);
        exif.SetValue(ExifTag.DateTime, exifDate);
        image.SetProfile(exif);

        // Update XMP (Safe Merge)
        xmpWriter.UpdateImageXmpProfile(image, date);

        if (Path.GetExtension(filePath) == ".png")
        {
            // PNG Specific Attribute
            // tIME chunk is traditionally UTC
            image.SetAttribute("png:tIME", date.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }

        await image.WriteAsync(filePath);
    }

    public void WriteVideoDates(string filePath, DateTimeOffset date)
    {
        using var file = TagLib.File.Create(filePath);
        file.Tag.DateTagged = date.LocalDateTime;
        file.Tag.Year = (uint)date.Year;
        file.Save();
    }

    public void WriteAudioDates(string filePath, DateTimeOffset date)
    {
        using var file = TagLib.File.Create(filePath);
        file.Tag.DateTagged = date.LocalDateTime;
        file.Tag.Year = (uint)date.Year;
        file.Save();
    }
}
