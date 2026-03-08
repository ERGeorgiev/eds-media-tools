using EdsMediaArchiver.Models;
using EdsMediaArchiver.Services;
using EdsMediaArchiver.Services.Compressors;
using EdsMediaArchiver.Services.Processors;
using EdsMediaArchiver.Services.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace EdsMediaArchiver.Helpers;

internal static class ServiceProviderHelper
{
    public static IServiceProvider Create(UserPreferences userPreferences)
    {
        var serviceProvider = new ServiceCollection()
            // Prefs
            .AddSingleton<IUserPreferences>(userPreferences)

            // Compressors
            .AddSingleton<IMediaCompressor, MixedFormatCompressor>()
            .AddSingleton<ImageCompressor>()
            .AddSingleton<IMediaCompressor, ImageCompressor>(x => x.GetRequiredService<ImageCompressor>())
            .AddSingleton<IImageCompressor, ImageCompressor>(x => x.GetRequiredService<ImageCompressor>())
            .AddSingleton<VideoCompressor>()
            .AddSingleton<IMediaCompressor, VideoCompressor>(x => x.GetRequiredService<VideoCompressor>())
            .AddSingleton<IVideoCompressor, VideoCompressor>(x => x.GetRequiredService<VideoCompressor>())
            .AddSingleton<IMediaCompressor, AudioCompressor>()

            // Processors
            .AddSingleton<ICompressProcessor, CompressProcessor>()

            // Resolvers
            .AddSingleton<IFileTypeResolver, FileTypeResolver>()
            .AddSingleton<IFileExtensionResolver, FileExtensionResolver>()

            // Other
            .AddSingleton<IArchiveRequestFactory, ArchiveRequestFactory>()
            .AddSingleton<IExifToolService, ExifToolService>()

            .BuildServiceProvider();

        return serviceProvider;
    }
}
