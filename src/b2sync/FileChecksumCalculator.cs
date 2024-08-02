using System.Security.Cryptography;
using System.Text;

namespace b2sync;

public class FileChecksumCalculator : IFileChecksumCalculator
{
    public Sha1Hash CalculateSha1(FileInfo file)
    {
        using var fs = new FileStream(file.FullName, FileMode.Open);
        using var bs = new BufferedStream(fs);
        using var sha1 = SHA1.Create();

        var hash = sha1.ComputeHash(bs);
        var formatted = new StringBuilder(2 * hash.Length);
        foreach (var b in hash)
        {
            formatted.Append($"{b:X2}");
        }

        return new Sha1Hash(formatted.ToString());
    }
}
