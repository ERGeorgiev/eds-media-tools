using EdsMediaDateRestorer.Helpers;
using EdsMediaDateRestorer.Services;
using EdsMediaDateRestorer.Services.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

// ToDo: Remove MetadataExtractor in favour of ExifTool

Console.WriteLine();
Console.WriteLine($"  Ed's Media Date Restorer v{Assembly.GetEntryAssembly()?.GetName().Version?.ToString(2)}");
Console.WriteLine($"    Restore and reliably set the original date of your media files.");
Console.WriteLine();

// Validate Input
if (args.Length == 0)
{
    if (Debugger.IsAttached)
    {
        var solutionDir = new DirectoryInfo(Directory.GetCurrentDirectory()).Parent!.Parent!.Parent!.FullName;
        args = [$"{Path.Combine(solutionDir, "TestData")}"];
    }
    else
    {
        Console.Error.WriteLine("[ERROR] No input provided");
        Console.WriteLine("To use, just drop files or folders to be processed (recursively) on top of this .exe file.");
        Console.Write("Press any key to exit...");
        Console.ReadLine();
        return 1;
    }
}

Console.WriteLine($"  Input:");
Console.WriteLine($"    {string.Join(Environment.NewLine + "    ", args)}");
Console.WriteLine();

Console.Write("  Ensure you have created a backup! Proceed? (Y/n): ");
ConsoleHelper.FlushInput();
if (ConsoleHelper.AskYesNo() == false)
{
    Console.WriteLine("  Cancelled.");
    Console.Write("Press any key to exit...");
    Console.ReadLine();
    return 0;
}
Console.WriteLine();

// Services
var serviceProvider = ServiceProviderHelper.Create();
var dateResolver = serviceProvider.GetRequiredService<IFileDateResolver>();
var exif = serviceProvider.GetRequiredService<IExifToolService>();

// Process each folder
ConcurrentBag<string> errors = [];
foreach (var inputPath in args)
{
    Console.WriteLine();
    Console.WriteLine($"  Processing: {inputPath}");

    var isDir = Directory.Exists(inputPath);
    var isFile = File.Exists(inputPath);
    if (isDir == false && isFile == false)
    {
        Console.Error.WriteLine($"    [ERROR] Target not found: {inputPath}");
        continue;
    }

    string dirPath;
    List<string> files;
    if (isDir) // It's a directory
    {
        dirPath = inputPath;
        files = Directory.EnumerateFiles(inputPath, "*", SearchOption.AllDirectories).ToList();
    }
    else // It's a file
    {
        dirPath = Path.GetDirectoryName(inputPath) ?? "";
        files = [inputPath];
    }
    Console.WriteLine($"    Found {files.Count} files");
    
    await Parallel.ForEachAsync(files, async (filePath, ct) =>
    {
        await Task.Run(async () => {
            var fileRelativePath = Path.GetRelativePath(dirPath, filePath);
            try
            {
                DateTimeOffset? setDate = await dateResolver.ResolveBestDate(filePath) ?? throw new Exception("Unable to resolve DateTime");
                try
                {
                    await exif.WriteDatesAsync(filePath, setDate.Value);
                    Console.WriteLine($"    Set Date '{setDate.Value.Date}' for file '{fileRelativePath}'");
                }
                finally
                {
                    File.SetCreationTime(filePath, setDate.Value.DateTime);
                    File.SetLastWriteTime(filePath, setDate.Value.DateTime);
                }
            }
            catch (Exception e)
            {
                var msg = $"    [ERROR] File '{Path.GetRelativePath(dirPath, filePath)}': {e.Message}";
                errors.Add(msg);
                Console.Error.WriteLine(msg);
            }
        }, ct);
    });

    Console.WriteLine($"    Finished Processing: {inputPath}");
    Console.WriteLine();
}

Console.WriteLine("  All done!");
Console.WriteLine();
Console.WriteLine($"  Errors:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
Console.WriteLine();
Console.Write("Press Enter to exit...");
ConsoleHelper.FlushInput();
Console.ReadLine();

return 0;
