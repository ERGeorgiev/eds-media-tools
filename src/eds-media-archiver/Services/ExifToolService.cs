using System.Diagnostics;
using System.Text;

namespace EdsMediaArchiver.Services;

public interface IExifToolService
{
    Task CopyMetadata(string sourceFilePath, string destinationFilePath);
}

public class ExifToolService : IExifToolService
{
    public async Task CopyMetadata(string sourceFilePath, string destinationFilePath)
    {
        var args = new List<string>
        {
            "-f",
            "-m",
            "-overwrite_original",
            "-tagsFromFile",
            sourceFilePath,
            "-all:all<all:all",
            destinationFilePath
        };

        await ExecuteAsync(args);
    }
    private static async Task<string> ExecuteAsync(IReadOnlyList<string> args)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "exiftool",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            }
        };

        // Use -@ - to read args from stdin, with UTF-8 filename charset
        process.StartInfo.ArgumentList.Add("-charset");
        process.StartInfo.ArgumentList.Add("filename=utf8");
        process.StartInfo.ArgumentList.Add("-@");
        process.StartInfo.ArgumentList.Add("-");

        process.Start();

        // Write args to stdin as UTF-8, one per line
        await using (var writer = new StreamWriter(
            process.StandardInput.BaseStream, new UTF8Encoding(false)))
            foreach (var arg in args)
                await writer.WriteLineAsync(arg);

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"ExifTool failed (exit code {process.ExitCode}): {error.Trim()}");

        return output;
    }
}
