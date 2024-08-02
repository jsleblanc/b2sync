using Bytewizer.Backblaze.Client;
using Bytewizer.Backblaze.Models;
using Bytewizer.Backblaze.Progress;
using FileInfo = System.IO.FileInfo;

namespace b2sync;

public class SyncTool
{
    private readonly IStorageClient _client;
    private readonly IFileChecksumCalculator _checksumCalculator;
    private readonly IDirectoryReader _directoryReader;
    private readonly IBucketReader _bucketReader;
    private readonly IBucketCleaner _bucketCleaner;
    private readonly ProgressBar _progressBar = new();

    public SyncTool(
        IStorageClient client,
        IFileChecksumCalculator checksumCalculator,
        IDirectoryReader directoryReader,
        IBucketReader bucketReader,
        IBucketCleaner bucketCleaner)
    {
        _client = client;
        _checksumCalculator = checksumCalculator;
        _directoryReader = directoryReader;
        _bucketReader = bucketReader;
        _bucketCleaner = bucketCleaner;
    }

    public async Task Sync(SyncOptions options, CancellationToken token)
    {
        var bucket = await _client.Buckets.FindByNameAsync(options.TargetBucket);

        var directoryContents = _directoryReader.GetDirectoryContents(options.SourceDirectory);
        var bucketContents = await _bucketReader.GetBucketContents(new BucketReaderOptions
        {
            TargetBucket = options.TargetBucket,
            TargetPath = options.TargetPath
        });

        await UploadAddedFiles(directoryContents, bucketContents, bucket, options, token);
        await DeleteRemovedFiles(directoryContents, bucketContents);
        await UploadChangedFiles(directoryContents, bucketContents, bucket, options, token);

        await _bucketCleaner.PurgeUnfinishedLargeFiles(bucket);
    }

    private async Task UploadChangedFiles(
        DirectoryContents directoryContents,
        BucketContents bucketContents,
        BucketItem bucket,
        SyncOptions options,
        CancellationToken token)
    {
        var existingKeys = directoryContents.Map.Keys.Intersect(bucketContents.Map.Keys).ToList();

        foreach (var key in existingKeys)
        {
            var fileInSource = directoryContents.Map[key];
            var fileInBucket = bucketContents.Map[key];

            var bucketFileHash = fileInBucket.ToHash();
            var directoryFileHash = _checksumCalculator.CalculateSha1(fileInSource);

            Console.WriteLine($"{fileInSource}:{directoryFileHash} - {fileInBucket.FileName}:{bucketFileHash}");
            if (directoryFileHash != bucketFileHash)
            {
                await UploadFile(options, fileInSource, bucket, token);
            }
            else
            {
                Console.WriteLine($"{fileInSource} and {fileInBucket.FileName} are identical, skipping.");
            }
        }
    }

    private async Task DeleteRemovedFiles(
        DirectoryContents directoryContents,
        BucketContents bucketContents)
    {
        var removedKeys = bucketContents.Map.Keys.Except(directoryContents.Map.Keys).ToList();
        foreach (var key in removedKeys)
        {
            var fileInBucket = bucketContents.Map[key];
            Console.WriteLine($"Deleting {fileInBucket.FileName} from bucket");
            await _client.Files.DeleteAsync(fileInBucket.FileId, fileInBucket.FileName);
        }
    }

    private async Task UploadAddedFiles(
        DirectoryContents directoryContents,
        BucketContents bucketContents,
        BucketItem bucket,
        SyncOptions options,
        CancellationToken token)
    {
        var addedKeys = directoryContents.Map.Keys.Except(bucketContents.Map.Keys).ToList();
        foreach (var key in addedKeys)
        {
            var fileInSource = directoryContents.Map[key];
            Console.WriteLine($"Uploading new file {fileInSource}");
            await UploadFile(options, fileInSource, bucket, token);
        }
    }

    private async Task UploadFile(SyncOptions options, FileInfo fileInfo, BucketItem bucket, CancellationToken token)
    {
        var relativePath = Path.GetRelativePath(options.SourceDirectory, fileInfo.FullName);
        var targetPath = Path.Combine(options.TargetPath, relativePath);

        Console.WriteLine($"Uploading {fileInfo.FullName} to {targetPath}");
        await using var fs = new FileStream(fileInfo.FullName, FileMode.Open);
        await using var bs = new BufferedStream(fs);
        await _client.UploadAsync(
            new UploadFileByBucketIdRequest(bucket.BucketId, targetPath),
            bs,
            _progressBar,
            token);
    }
}

