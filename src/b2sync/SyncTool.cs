using System.Diagnostics;
using Bytewizer.Backblaze.Client;
using Bytewizer.Backblaze.Models;
using Bytewizer.Backblaze.Progress;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

    private readonly Queue<UploadAttempt> _failedUploads = new();
    private const int MaxUploadAttempts = 10;
    public ILogger Logger { get; init; } = NullLogger.Instance;
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
        var sw = Stopwatch.StartNew();
        Logger.LogInformation("Retrieving bucket {bucket}", options.TargetBucket);
        var bucket = await _client.Buckets.FindByNameAsync(options.TargetBucket);
        if (bucket == null) throw new InvalidOperationException($"could not retrieve bucket {options.TargetBucket}");

        Logger.LogInformation("Scanning directory {dir} for files", options.SourceDirectory);
        var directoryContents = _directoryReader.GetDirectoryContents(options.SourceDirectory);
        Logger.LogInformation("Found {files} files to sync ({elapsed})", directoryContents.Items.Count, sw.Elapsed);

        Logger.LogInformation("Scanning bucket {bucket} for files", options.TargetBucket);
        var bucketContents = await _bucketReader.GetBucketContents(new BucketReaderOptions
        {
            TargetBucket = options.TargetBucket,
            TargetPath = options.TargetPath
        });
        Logger.LogInformation("Found {files} in bucket ({elapsed})", bucketContents.Items.Count, sw.Elapsed);

        await _bucketCleaner.PurgeUnfinishedLargeFiles(bucket);
        await _bucketCleaner.PurgePriorVersions(bucket);

        Logger.LogInformation("Uploading new files");
        var swAdded = Stopwatch.StartNew();
        await UploadAddedFiles(directoryContents, bucketContents, bucket, options, token);
        swAdded.Stop();
        Logger.LogInformation("Finished uploading new files in {elapsed}", swAdded.Elapsed);

        Logger.LogInformation("Deleting removed files");
        var swRemoved = Stopwatch.StartNew();
        await DeleteRemovedFiles(directoryContents, bucketContents);
        swRemoved.Stop();
        Logger.LogInformation("Finished removing files in {elapsed}", swRemoved.Elapsed);

        Logger.LogInformation("Uploading existing files");
        var swExisting = Stopwatch.StartNew();
        await UploadChangedFiles(directoryContents, bucketContents, bucket, options, token);
        swExisting.Stop();
        Logger.LogInformation("Finished existing files in {elapsed}", swExisting.Elapsed);

        await _bucketCleaner.PurgeUnfinishedLargeFiles(bucket);

        Logger.LogInformation("Failed uploads {count} to retry", _failedUploads.Count);
        var swRetries = Stopwatch.StartNew();
        await RetryFailedUploads(options, bucket, token);
        swRetries.Stop();
        Logger.LogInformation("Completed retry uploads in {elapsed}", swRetries.Elapsed);

        sw.Stop();
        Logger.LogInformation("Sync completed in {elapsed}", sw.Elapsed);
    }

    private async Task UploadChangedFiles(
        DirectoryContents directoryContents,
        BucketContents bucketContents,
        BucketItem bucket,
        SyncOptions options,
        CancellationToken token)
    {
        var existingKeys = directoryContents.Map.Keys.Intersect(bucketContents.Map.Keys).ToList();

        Logger.LogInformation("Found {c} existing files to compare and sync", existingKeys.Count);
        foreach (var key in existingKeys)
        {
            var fileInSource = directoryContents.Map[key];
            var fileInBucket = bucketContents.Map[key];

            var bucketFileHash = fileInBucket.ToHash();
            var directoryFileHash = _checksumCalculator.CalculateSha1(fileInSource);

            if (directoryFileHash != bucketFileHash)
            {
                await UploadFile(options, new UploadAttempt(fileInSource), bucket, token);
            }
            else
            {
                Logger.LogInformation("{fs} and {fd} are identical, skipping.", fileInSource, fileInBucket.FileName);
            }
        }
    }

    private async Task DeleteRemovedFiles(
        DirectoryContents directoryContents,
        BucketContents bucketContents)
    {
        var removedKeys = bucketContents.Map.Keys.Except(directoryContents.Map.Keys).ToList();
        Logger.LogInformation("Removed files to delete: {c}", removedKeys.Count);
        foreach (var key in removedKeys)
        {
            var fileInBucket = bucketContents.Map[key];
            Logger.LogInformation("Deleting {file}", fileInBucket.FileName);
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
        Logger.LogInformation("Added files to upload: {c}", addedKeys.Count);
        foreach (var key in addedKeys)
        {
            var fileInSource = directoryContents.Map[key];
            await UploadFile(options, new UploadAttempt(fileInSource), bucket, token);
        }
    }

    private static string CalculateTargetFileName(SyncOptions options, FileInfo fileInfo)
    {
        var relativePath = Path.GetRelativePath(options.SourceDirectory, fileInfo.FullName);
        var targetPath = Path.Combine(options.TargetPath, relativePath);
        var uri = new UriBuilder
        {
            Scheme = Uri.UriSchemeFile,
            Path = targetPath,
            Host = string.Empty
        };

        var bucketPath = uri.Uri.ToString()
            .Replace("file://", string.Empty)
            .Replace(options.TargetPath, options.TargetPath, StringComparison.OrdinalIgnoreCase);

        return bucketPath;
    }

    private async Task UploadFile(SyncOptions options, UploadAttempt attempt, BucketItem bucket, CancellationToken token)
    {
        var fileInfo = attempt.FileInfo;
        var bucketPath = CalculateTargetFileName(options, fileInfo);

        try
        {
            Logger.LogInformation("Uploading {fs} to {fd}", fileInfo.FullName, bucketPath);
            await using var fs = new FileStream(fileInfo.FullName, FileMode.Open);
            await using var bs = new BufferedStream(fs);
            await _client.UploadAsync(
                new UploadFileByBucketIdRequest(bucket.BucketId, bucketPath),
                bs,
                _progressBar,
                token);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to upload file {f}; queueing for retry later", fileInfo);
            _failedUploads.Enqueue(attempt with { AttemptCount = attempt.AttemptCount + 1 });
        }
    }

    private async Task RetryFailedUploads(SyncOptions options, BucketItem bucket, CancellationToken token)
    {
        while (_failedUploads.TryDequeue(out var item))
        {
            if (item.AttemptCount < MaxUploadAttempts)
            {
                Logger.LogInformation("Retrying upload of {file} attempts so far: {attempts}", item.FileInfo.FullName,
                    item.AttemptCount);
                await UploadFile(options, item, bucket, token);
            }
            else
            {
                Logger.LogWarning("Maximum upload attempts reached for {item}, giving up", item.FileInfo.FullName);
            }
        }
    }

    private record UploadAttempt(FileInfo FileInfo, int AttemptCount = 0);
}

