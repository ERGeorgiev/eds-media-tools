using EdsMediaDateRestorer.Services;
using EdsMediaDateRestorer.Services.FileDateReaders;
using EdsMediaDateRestorer.Services.Resolvers;
using EdsMediaDateRestorer.Services.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace EdsMediaDateRestorer.Helpers;

internal static class ServiceProviderHelper
{
    public static IServiceProvider Create()
    {
        var serviceProvider = new ServiceCollection()
            // Readers
            .AddSingleton<IOriginalDateReader, OriginalDateReader>()
            .AddSingleton<IFilenameDateReader, FilenameDateReader>()
            .AddSingleton<IOldestDateReader, OldestDateReader>()

            // Resolvers
            .AddSingleton<IFileDateResolver, FileDateResolver>()

            // Validators
            .AddSingleton<IDateValidator, DateValidator>()

            // Other
            .AddSingleton<IExifToolService, ExifToolService>()

            .BuildServiceProvider();

        return serviceProvider;
    }
}
