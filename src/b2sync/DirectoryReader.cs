namespace b2sync;

public interface IDirectoryReader
{
    DirectoryContents GetDirectoryContents(string path);
}

public class DirectoryReader : IDirectoryReader
{
    private readonly HashSet<string> _excludeFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ".DS_Store"
    };

    private readonly HashSet<string> _excludeDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "@eaDir",
        "#recycle"
    };

    public DirectoryContents GetDirectoryContents(string path)
    {
        var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
            .Select(s => new FileInfo(s))
            .Where(IncludeFile)
            .ToList();

        return new DirectoryContents
        {
            Map = files.ToDictionary(f => f.FromFileInfo(path), f => f),
            Items = files
        };
    }

    private bool IncludeFile(FileInfo file)
    {
        if (_excludeFiles.Contains(file.Name)) return false;
        if (IsExcludedDirectory(file.Directory)) return false;
        return true;
    }

    private bool IsExcludedDirectory(DirectoryInfo? directoryInfo)
    {
        if (directoryInfo == null) return false;
        var isParentExcluded = IsExcludedDirectory(directoryInfo.Parent);
        return isParentExcluded || _excludeDirectories.Contains(directoryInfo.Name);
    }
}

public record DirectoryContents
{
    public required IReadOnlyDictionary<FileKey, FileInfo> Map { get; init; }
    public required IReadOnlyList<FileInfo> Items { get; init; }
}
