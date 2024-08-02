namespace b2sync;

public record SyncTask
{
    public required FileInfo File { get; init; }
    public string Hash { get; init; } = string.Empty;
}
