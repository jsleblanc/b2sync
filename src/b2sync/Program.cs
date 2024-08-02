using Bytewizer.Backblaze.Client;
using Bytewizer.Backblaze.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace b2sync;

class Program
{
    private const string KeyId = "KEY_ID";
    private const string ApplicationKey = "APPLICATION_KEY";
    private const string SourceDir = "SOURCE_DIR";
    private const string TargetBucket = "TARGET_BUCKET";
    private const string TargetPath = "TARGET_PATH";

    static async Task Main(string[] args)
    {
        DotNetEnv.Env.TraversePath().Load();

        var syncOpts = new SyncOptions
        {
            ApplicationKey = GetEnvironmentVariableOrThrow(ApplicationKey),
            KeyId = GetEnvironmentVariableOrThrow(KeyId),
            SourceDirectory = GetEnvironmentVariableOrThrow(SourceDir),
            TargetBucket = GetEnvironmentVariableOrThrow(TargetBucket),
            TargetPath = GetEnvironmentVariableOrThrow(TargetPath)
        };

        var options = new ClientOptions
        {
            KeyId = syncOpts.KeyId,
            ApplicationKey = syncOpts.ApplicationKey,
            UploadMaxParallel = 10,
            UploadPartSize = 256 * FileSize.MegaByte,
            UploadCutoffSize = 512 * FileSize.MegaByte
        };
        options.Validate();

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                //.AddFilter("Bytewizer.Backblaze", LogLevel.Trace)
                .AddFile("SyncLog-{Date}.txt",
                    outputTemplate:
                    "{Timestamp:o} {RequestId,13} [{Level:u3}] {Message} ({EventId:x8}){Properties}{NewLine}{Exception}")
                .AddDebug()
                .AddSimpleConsole()
                .SetMinimumLevel(LogLevel.Information);
        });

        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = new BackblazeClient(options, loggerFactory, cache);
        await client.ConnectAsync();

        var checksumCalculator = new FileChecksumCalculator
        {
            Logger = loggerFactory.CreateLogger<FileChecksumCalculator>()
        };
        var directoryReader = new DirectoryReader();
        var bucketReader = new BucketReader(client);
        var bucketCleaner = new BucketCleaner(client)
        {
            Logger = loggerFactory.CreateLogger<BucketCleaner>()
        };

        var tool = new SyncTool(client, checksumCalculator, directoryReader, bucketReader, bucketCleaner)
        {
            Logger = loggerFactory.CreateLogger<SyncTool>()
        };
        await tool.Sync(syncOpts, CancellationToken.None);
    }

    private static string GetEnvironmentVariableOrThrow(string name)
    {
        var env = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(env))
            throw new InvalidOperationException($"could not find environment variable {name}");
        return env;
    }
}
