using System.Diagnostics;
using System.Text;

namespace EdsMediaArchiver.Services;

public interface IExifToolService
{
    Task CopyMetadata(string sourceFilePath, string destinationFilePath,
        bool pixelsAlreadyOriented = false, CancellationToken cancellationToken = default);
}

public class ExifToolService : IExifToolService
{
    public async Task CopyMetadata(string sourceFilePath, string destinationFilePath,
        bool pixelsAlreadyOriented = false, CancellationToken cancellationToken = default)
    {
        var args = new List<string>
        {
            "-f",
            "-m",
            "-overwrite_original",
            "-tagsFromFile",
            sourceFilePath,
            "-all:all<all:all",
            "-icc_profile",            // ICC is a separate block; -all:all does NOT carry it (verified)
        };
        // Pixels were baked upright upstream (AutoOrient). Without this, the source's Orientation
        // tag would re-rotate the already-rotated image. '#' forces the raw numeric value 1 = normal.
        if (pixelsAlreadyOriented)
            args.Add("-Orientation#=1");

        args.Add($"-XPComment=EdsMediaArchiver");
        args.Add(destinationFilePath);
        await ExecuteAsync(args, cancellationToken);
    }

    private static async Task<string> ExecuteAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
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
        try
        {
            // Drain stdout/stderr before writing stdin so a large arg list can never deadlock
            // against a full pipe buffer.
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            // Write args to stdin as UTF-8, one per line
            await using (var writer = new StreamWriter(
                process.StandardInput.BaseStream, new UTF8Encoding(false)))
                foreach (var arg in args)
                    await writer.WriteLineAsync(arg);

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
                throw new InvalidOperationException(
                    $"ExifTool failed (exit code {process.ExitCode}): {error.Trim()}");

            // exiftool exits 0 while still printing non-fatal "Warning:" lines (a tag it could not
            // write, a truncated block, etc.). For an archiver that is silent data loss, so surface it.
            if (!string.IsNullOrWhiteSpace(error))
                Console.WriteLine("ExifTool succeeded with warnings: {0}", error.Trim());

            return output;
        }
        catch
        {
            TryKill(process);
            throw;
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best effort: the process may have exited between the check and the kill.
        }
    }
}