using EdsMediaArchiver;
using EdsMediaArchiver.Models;
using EdsMediaArchiver.Services;
using MetadataExtractor.Util;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;

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

Console.WriteLine("  This tool will:");
Console.WriteLine("    - Detect actual file types and fix wrong extensions");
Console.WriteLine("    - Convert media that doesn't support EXIF date metadata (WebP, BMP, GIF, TIFF, MTS, WAV) to JPG/MP4");
Console.WriteLine("    - Write date metadata and set filesystem dates");
Console.WriteLine("    - DELETE input files as it's creating new ones");

Console.WriteLine();
Console.Write("  Proceed? (Y/n) ");
var confirm = Console.ReadLine();
if (confirm?.TrimStart().StartsWith("Y", StringComparison.OrdinalIgnoreCase) == false)
{
    Console.WriteLine("  Cancelled.");
    Console.Write("Press any key to exit...");
    Console.ReadLine();
    return 0;
}

// 1. Setup the container
var serviceProvider = new ServiceCollection()
    .AddSingleton<IDateValidator, DateValidator>()
    .AddSingleton<IMetadataWriter, MetadataWriter>()
    .BuildServiceProvider();

// Services
var magick = new ImageMagickService();
var metadataService = new MetadataWriter();
var dateResolver = new DateResolver(metadataService);
var mediaFileProcessor = new MediaFileProcessor(magick, metadataService, dateResolver);

// 1. Get all files
// 2. Rename mistyped files
// 3. Convert unsupported
// 4. Compress
// 5. Date

// Process each folder
foreach (var inputPath in args)
{
    Console.WriteLine("────────────────────────────────────────────────");
    Console.WriteLine($"Processing: {inputPath}");
    Console.WriteLine();

    FileSystemInfo targetInfo = new FileInfo(inputPath);
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
    var unsupportedFiles = files.Where(f => Constants.SupportedExtensions.Contains(Path.GetExtension(f)) == false).ToList();
    var supportedFiles = files.Except(unsupportedFiles).ToList();
    Console.WriteLine($"  Found {supportedFiles.Count} supported files");
    Console.WriteLine($"  Found {unsupportedFiles.Count} unsupported files");
    Console.WriteLine();

    // Process files with limited parallelism
    var semaphore = new SemaphoreSlim(10);
    var tasks = supportedFiles.Select(async file =>
    {
        await semaphore.WaitAsync();
        try
        {
            return await mediaFileProcessor.ProcessFileAsync(dirPath, file);
        }
        finally
        {
            semaphore.Release();
        }
    });

    var results = await Task.WhenAll(tasks);
    Console.WriteLine($"  Finished Processing: {inputPath}");

    // Summary
    Console.WriteLine("────────────────────────────────────────────────");
    Console.WriteLine($"Summary");
    var fixedFiles = results.Where(r => r.Status == ProcessingStatus.Fixed).ToList();
    var convertedFiles = results.Where(r => r.Status == ProcessingStatus.Converted).ToList();
    var skippedFiles = results.Where(r => r.Status == ProcessingStatus.Skipped).ToList();
    var errorFiles = results.Where(r => r.Status == ProcessingStatus.Error).ToList();

    if (fixedFiles.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("  Date Fixes:");
        foreach (var r in fixedFiles)
            Console.WriteLine($"    {r.RelativePath,-60} {r.DateAssigned:yyyy-MM-dd HH:mm:ss}");
    }

    if (unsupportedFiles.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("  Unsupported:");
        foreach (var r in unsupportedFiles)
            Console.WriteLine($"    {Path.GetRelativePath(dirPath, r),-60}");
    }

    if (convertedFiles.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("  Converted to JPG:"); // todo: to converted any to any
        foreach (var r in convertedFiles)
            Console.WriteLine($"    {r.RelativePath,-60} {r.DateAssigned:yyyy-MM-dd HH:mm:ss}");
    }

    if (skippedFiles.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("  Skipped:");
        foreach (var r in skippedFiles)
            Console.WriteLine($"    {r.RelativePath,-60} {r.ErrorMessage}");
    }

    if (errorFiles.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("  Errors:");
        foreach (var r in errorFiles)
            Console.WriteLine($"    {r.RelativePath,-60} {r.ErrorMessage}");
    }

    Console.WriteLine();
    Console.WriteLine("  Results:");
    Console.WriteLine($"    Processed:  {fixedFiles.Count}");
    Console.WriteLine($"    Converted:  {convertedFiles.Count}");
    Console.WriteLine($"    Skipped:    {skippedFiles.Count}");
    Console.WriteLine($"    Errors:     {errorFiles.Count}");
}

Console.WriteLine();
Console.WriteLine("================================================");
Console.WriteLine("  All done!");
Console.WriteLine("================================================");
Console.WriteLine();
Console.Write("Press any key to exit...");
Console.ReadLine();

return 0;
