namespace b2sync;

public interface IDirectoryReader
{
    DirectoryContents GetDirectoryContents(string path);
}

public class DirectoryReader : IDirectoryReader
{
    public DirectoryContents GetDirectoryContents(string path)
    {
        var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
            .Select(s => new FileInfo(s))
            .ToList();

        return new DirectoryContents
        {
            Map = files.ToDictionary(f => f.FromFileInfo(path), f => f),
            Items = files
        };
    }
}

public record DirectoryContents
{
    public required IReadOnlyDictionary<FileKey, FileInfo> Map { get; init; }
    public required IReadOnlyList<FileInfo> Items { get; init; }
}
