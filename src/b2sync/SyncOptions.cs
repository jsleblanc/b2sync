namespace b2sync;

public record SyncOptions
{
    public required string SourceDirectory { get; init; }
    public required string TargetBucket { get; init; }
    public required string TargetPath { get; init; }
    public required string KeyId { get; init; }
    public required string ApplicationKey { get; init; }
}
