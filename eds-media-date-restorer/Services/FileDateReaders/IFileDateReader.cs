using System.Text.Json;

namespace EdsMediaDateRestorer.Services.FileDateReaders;

public interface IFileDateReader
{
    DateTimeOffset? Read(string filePath, Dictionary<string, string> tags);
}
