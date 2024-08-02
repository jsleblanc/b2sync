namespace b2sync;

public interface IDirectoryScanner
{
    IReadOnlyList<FileInfo> FindAllFilesInPath(string path);
}
