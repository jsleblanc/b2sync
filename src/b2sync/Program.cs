using Bytewizer.Backblaze.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace b2sync;

class Program
{


    static async Task Main(string[] args)
    {
        var syncOpts = new SyncOptions
        {
            ApplicationKey = keySecret,
            KeyId = keyId,
            SourceDirectory = "/Volumes/Time Machine",
            TargetBucket = TimeMachineBucket,
            TargetPath = "TimeMachineBackups/"
        };

        var options = new ClientOptions();
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Bytewizer.Backblaze", LogLevel.Trace)
                .AddDebug();
        });

        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = new BackblazeClient(options, loggerFactory, cache);
        await client.ConnectAsync(syncOpts.KeyId, syncOpts.ApplicationKey);



        var checksumCalculator = new FileChecksumCalculator();

        var directoryReader = new DirectoryReader();
        var bucketReader = new BucketReader(client);
        var bucketCleaner = new BucketCleaner(client);


        var tool = new SyncTool(client, checksumCalculator, directoryReader, bucketReader, bucketCleaner);
        await tool.Sync(syncOpts, CancellationToken.None);

        /*
        var directoryContents = directoryReader.GetDirectoryContents(syncOpts.SourceDirectory);
        var bucketContents = await bucketReader.GetBucketContents(new BucketReaderOptions
        {
            TargetBucket = syncOpts.TargetBucket,
            TargetPath = syncOpts.TargetPath
        });

        //added
        var addedKeys = directoryContents.Map.Keys.Except(bucketContents.Map.Keys).ToList();

        //removed
        var removedKeys = bucketContents.Map.Keys.Except(directoryContents.Map.Keys).ToList();

        //existing
        var existingKeys = directoryContents.Map.Keys.Intersect(bucketContents.Map.Keys).ToList();

        Console.WriteLine($"Added keys {addedKeys.Count}");
        Console.WriteLine($"Removed keys {removedKeys.Count}");
        Console.WriteLine($"Existing keys {existingKeys.Count}");

        var tmBucket = await client.Buckets.FindByNameAsync(TimeMachineBucket);
        await bucketCleaner.PurgeUnfinishedLargeFiles(tmBucket);
*/
        Console.WriteLine("Hello, World!");
    }
}
