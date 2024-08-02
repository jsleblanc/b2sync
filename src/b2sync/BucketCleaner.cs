using Bytewizer.Backblaze.Client;
using Bytewizer.Backblaze.Models;

namespace b2sync;

public interface IBucketCleaner
{
    Task PurgeUnfinishedLargeFiles(BucketItem bucket);
}

public class BucketCleaner(IStorageClient client) : IBucketCleaner
{
    public async Task PurgeUnfinishedLargeFiles(BucketItem bucket)
    {
        var items = await client.Files.GetEnumerableAsync(new ListUnfinishedLargeFilesRequest(bucket.BucketId));
        var itemsList = items.ToList();
        Console.WriteLine($"Found {itemsList.Count} unfinished large files to delete");
        foreach (var i in itemsList)
        {
            Console.WriteLine($"Deleting file id {i.FileId} name {i.FileName}");
            await client.Files.DeleteAsync(i.FileId, i.FileName);
        }
    }
}
