using System.Diagnostics;
using Bytewizer.Backblaze.Client;
using Bytewizer.Backblaze.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace b2sync;

public interface IBucketCleaner
{
    Task PurgeUnfinishedLargeFiles(BucketItem bucket);
    Task PurgePriorVersions(BucketItem bucket);
}

public class BucketCleaner(IStorageClient client) : IBucketCleaner
{
    public ILogger Logger { get; init; } = NullLogger.Instance;

    public async Task PurgeUnfinishedLargeFiles(BucketItem bucket)
    {
        var sw = Stopwatch.StartNew();
        Logger.LogInformation("Purging unfinished large files from bucket {bucket}", bucket.BucketName);
        var items = await client.Files.GetEnumerableAsync(new ListUnfinishedLargeFilesRequest(bucket.BucketId));
        var itemsList = items.ToList();
        Logger.LogInformation("Found {count} unfinished large files to delete", itemsList.Count);
        foreach (var i in itemsList)
        {
            Logger.LogInformation("Deleting {name} ({id})", i.FileName, i.FileId);
            await client.Files.DeleteAsync(i.FileId, i.FileName);
        }

        sw.Stop();
        Logger.LogInformation("Finished deleting unfinished large files in {elapsed}", sw.Elapsed);
    }

    public async Task PurgePriorVersions(BucketItem bucket)
    {
        var sw = Stopwatch.StartNew();
        Logger.LogInformation("Purging previous versions of files from bucket {bucket}", bucket.BucketName);

        var items = await client.Files.GetEnumerableAsync(new ListFileVersionRequest(bucket.BucketId));
        var itemsMap =
            items
                .GroupBy(i => i.FileName)
                .ToDictionary(x => x.Key, x => x.OrderByDescending(i => i.UploadTimestamp).ToList());

        foreach (var kvp in itemsMap)
        {
            if (kvp.Value.Count > 1)
            {
                var files = kvp.Value;
                Logger.LogInformation("File {name} has {count} versions; deleting {x} of them", kvp.Key, files.Count,
                    files.Count - 1);

                foreach (var file in files.Skip(1))
                {
                    Logger.LogInformation("Deleting {name} ({id})", file.FileName, file.FileId);
                    await client.Files.DeleteAsync(file.FileId, file.FileName);
                }
            }
        }

        sw.Stop();
        Logger.LogInformation("Finished deleting previous versions of files in {elapsed}", sw.Elapsed);
    }
}
