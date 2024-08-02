using Bytewizer.Backblaze.Extensions;
using Bytewizer.Backblaze.Models;

namespace b2sync;

public sealed record Sha1Hash(string Value)
{
    public bool Equals(Sha1Hash? other) =>
        string.Equals(Value, other?.Value, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;
}

public static class Sha1HashExtensions
{
    public static Sha1Hash ToHash(this FileItem item) =>
        item.ContentSha1 == "none"
            ? new Sha1Hash(item.FileInfo.GetLargeFileSha1())
            : new Sha1Hash(item.ContentSha1);
}
