namespace b2sync;

public class DirectoryScanner : IDirectoryScanner
{
    public IReadOnlyList<FileInfo> FindAllFilesInPath(string path) =>
        Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
            .Select(s => new FileInfo(s))
            .ToList();
}
