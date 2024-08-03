using Bytewizer.Backblaze.Models;
using FileInfo = System.IO.FileInfo;

namespace b2sync;

public record FileKey(string Key) { }

public static class FileKeyExtensions
{
    public static FileKey FromFileInfo(this FileInfo fileInfo, string path)
        => new(Path.GetRelativePath(path, fileInfo.FullName));

    public static FileKey FromFileItem(this FileItem fileItem, string path) =>
        new(Path.GetRelativePath(path, fileItem.FileName));
}
