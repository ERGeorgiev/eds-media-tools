using EdsMediaArchiver.Definitions;
using EdsMediaArchiver.Helpers;
using EdsMediaArchiver.Services;
using EdsMediaArchiver.Services.Logging;
using EdsMediaArchiver.Services.Processors;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;

//ToDo: Gifs, Transparent PNGs, Re-compressing JPGs (cases where its wanted and not)

Console.WriteLine();
Console.WriteLine("================================================");
Console.WriteLine($"  Ed's Media Archiver v{Assembly.GetEntryAssembly()?.GetName().Version?.ToString(2)}");
Console.WriteLine("  Prepare your media files for storage.");
Console.WriteLine("================================================");
Console.WriteLine();

if (Debugger.IsAttached)
    args = [$"{Path.Combine(Directory.GetCurrentDirectory(), "TestData")}", $"{Path.Combine(Directory.GetCurrentDirectory(), "TestData2")}"];

// Validate input paths
if (args.Length == 0)
{
    Console.Error.WriteLine("[ERROR] No folders provided. Pass folder paths as arguments.");
    Console.Error.WriteLine("To use, just drop files or folders to be processed (recursively) on top of this .exe file.");
    Console.Write("Press any key to exit...");
    Console.ReadLine();
    return 1;
}

if (ConsoleHelper.TryGetUserPreferences(out var prefs) == false)
{
    Console.Write("Press any key to exit...");
    Console.ReadLine();
}

Console.WriteLine();
Console.Write("  Ensure you have created a backup! Proceed? (Y/n): ");
if (ConsoleHelper.AskYesNo() == false)
{
    Console.WriteLine("  Cancelled.");
    Console.Write("Press any key to exit...");
    Console.ReadLine();
    return 0;
}

// Services
var serviceProvider = ServiceProviderHelper.Create();
var mediaFileProcessor = serviceProvider.GetRequiredService<IArchiveProcessor>();
var fileRequestFactory = serviceProvider.GetRequiredService<IArchiveRequestFactory>();
var logs = serviceProvider.GetRequiredService<IProcessLogger>();

// Process each folder
foreach (var inputPath in args)
{
    Console.WriteLine("────────────────────────────────────────────────");
    Console.WriteLine($"Processing: {inputPath}");
    Console.WriteLine();

    var targetInfo = new FileInfo(inputPath);
    var exists = (int)targetInfo.Attributes != -1;
    if (exists == false)
    {
        Console.Error.WriteLine($"[ERROR] Target not found: {inputPath}");
        continue;
    }

    string dirPath;
    List<string> files;
    if (targetInfo.Attributes.HasFlag(FileAttributes.Directory)) // It's a directory
    {
        dirPath = inputPath;
        files = Directory.EnumerateFiles(inputPath, "*", SearchOption.AllDirectories).ToList();
    }
    else // It's a file
    {
        dirPath = Path.GetDirectoryName(inputPath) ?? "";
        files = [inputPath];
    }
    var unsupportedFiles = files.Where(f => MediaType.SupportedTypes.Contains(Path.GetExtension(f)) == false).ToList(); // ToDo: Decide with actual type
    var supportedFiles = files.Except(unsupportedFiles).ToList();
    Console.WriteLine($"  Found {supportedFiles.Count} supported files");
    Console.WriteLine();

    // Process files with limited parallelism
    var semaphore = new SemaphoreSlim(10);
    var tasks = supportedFiles.Select(async file =>
    {
        await semaphore.WaitAsync();
        try
        {
            var request = fileRequestFactory.Create(dirPath, file);
            request.Compress = prefs.Compress;
            request.ReizeOnCompress = prefs.ResizeOnCompress;
            request.Standardize = prefs.Standardize;
            request.SetDates = prefs.SetDates;
            await mediaFileProcessor.ProcessFileAsync(request);
            logs.PrintLogs(request.OriginalPath.Absolute);
            if (request.OriginalPath != request.NewPath)
            {
                logs.PrintLogs(request.NewPath.Absolute);
            }
        }
        finally
        {
            semaphore.Release();
        }
    });

    await Task.WhenAll(tasks);
    Console.WriteLine($"  Finished Processing: {inputPath}");
    logs.PrintSummary();
}

Console.WriteLine();
Console.WriteLine("================================================");
Console.WriteLine("  All done!");
Console.WriteLine("================================================");
Console.WriteLine();
Console.Write("Press any key to exit...");
Console.ReadLine();

return 0;
