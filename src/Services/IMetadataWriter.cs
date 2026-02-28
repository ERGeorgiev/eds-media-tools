
namespace EdsMediaArchiver.Services
{
    public interface IMetadataWriter
    {
        Task WriteExifDatesAsync(string filePath, DateTimeOffset date);
        Task WritePngDatesAsync(string filePath, DateTimeOffset date);
        void WriteVideoDates(string filePath, DateTimeOffset date);
        Task WriteXmpDatesAsync(string filePath, DateTimeOffset date);
    }
}