using System.Diagnostics;
using Bytewizer.Backblaze.Client;
using Bytewizer.Backblaze.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace b2sync;

public interface IBucketCleaner
{
    Task PurgeUnfinishedLargeFiles(BucketItem bucket);
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
}
