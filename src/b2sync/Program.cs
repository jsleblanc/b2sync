using System.Collections.Concurrent;
using System.Diagnostics;
using Bytewizer.Backblaze.Client;
using Bytewizer.Backblaze.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using FileInfo = System.IO.FileInfo;

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

        var checksumCalculator = new FileChecksumCalculator();
        var scanner = new DirectoryScanner();

        //var allFiles = scanner.FindAllFilesInPath(syncOpts.SourceDirectory);
        //var tasks = allFiles.Select(f => new SyncTask { File = f }).ToList();

        var syncTracker = new SyncStateTracker(new FileInfo("sync-989b032b.sqlite"));
        syncTracker.Initialize();
        //syncTracker.SeedTasks(tasks);

        var sw = Stopwatch.StartNew();

        Console.WriteLine("Hashing files");
        var tasks = syncTracker.GetIncompleteTasks();

        var hashedTasks = new ConcurrentBag<SyncTask>();
        Parallel.ForEach(tasks, () => new List<SyncTask>(), (task, loopState, localState) =>
        {
            var hash = checksumCalculator.CalculateSha1(task.File);
            localState.Add(task with { Hash = hash });
            return localState;
        }, (state) =>
        {
            foreach (var t in state)
                hashedTasks.Add(t);
            Console.WriteLine($"Hashed {state.Count} items");
        });

/*
        foreach (var task in tasks)
        {
            Console.Write($"{task.File}...");
            var hash = checksumCalculator.CalculateSha1(task.File);
            Console.WriteLine(hash);
            syncTracker.SetHash(task with { Hash = hash });
        }*/

        sw.Stop();



        var options = new ClientOptions();
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Bytewizer.Backblaze", LogLevel.Trace)
                .AddDebug();
        });

        var cache = new MemoryCache(new MemoryCacheOptions());

        var client = new BackblazeClient(options, loggerFactory, cache);
        await client.ConnectAsync(keyId, keySecret);

        var tmBucket = await client.Buckets.FindByNameAsync(TimeMachineBucket);
        var buckets = await client.Buckets.GetAsync();

        foreach (var bucket in buckets)
            Console.WriteLine($"ID: {bucket.BucketId} - Bucket Name: {bucket.BucketName} - Type: {bucket.BucketType}");

        var unfinishedFiles =
            await client.Files.GetEnumerableAsync(new ListUnfinishedLargeFilesRequest(tmBucket.BucketId));
/*
        foreach (var unfinishedFile in unfinishedFiles)
        {
            Console.WriteLine(unfinishedFile.FileName);
        }
*/
        Console.WriteLine($"unfinished files: {unfinishedFiles.Count()}");

        var files = await client.Files.GetEnumerableAsync(new ListFileNamesRequest(tmBucket.BucketId));
        /*
        foreach (var file in files)
        {
            Console.WriteLine(file.FileName);
        }*/

        Console.WriteLine("Hello, World!");
    }
}
