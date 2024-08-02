using Bytewizer.Backblaze.Client;
using Bytewizer.Backblaze.Models;

namespace b2sync;

public record BucketReaderOptions
{
    public required string TargetBucket { get; init; }
    public required string TargetPath { get; init; }
}

public interface IBucketReader
{
    Task<BucketContents> GetBucketContents(BucketReaderOptions options);
}

public class BucketReader(IStorageClient client) : IBucketReader
{
    public async Task<BucketContents> GetBucketContents(BucketReaderOptions options)
    {
        var bucket = await client.Buckets.FindByNameAsync(options.TargetBucket);
        if (bucket == null)
            throw new InvalidOperationException($"could not find bucket named \"{options.TargetBucket}\"");

        var files = await client.Files.GetEnumerableAsync(new ListFileNamesRequest(bucket.BucketId));
        var filesList = files.ToList();
        return new BucketContents
        {
            Map = filesList.ToDictionary(f => f.FromFileItem(options.TargetPath), f => f),
            Items = filesList.ToList()
        };
    }
}

public record BucketContents
{
    public required IReadOnlyDictionary<FileKey, FileItem> Map { get; init; }
    public required IReadOnlyList<FileItem> Items { get; init; }
}
