using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace b2sync;

public class FileChecksumCalculator : IFileChecksumCalculator
{
    public ILogger Logger { get; init; } = NullLogger.Instance;

    public Sha1Hash CalculateSha1(FileInfo file)
    {
        using var fs = new FileStream(file.FullName, FileMode.Open);
        using var bs = new BufferedStream(fs);
        using var sha1 = SHA1.Create();

        var sw = Stopwatch.StartNew();
        var hash = sha1.ComputeHash(bs);
        sw.Stop();
        Logger.LogDebug("Calculated SHA1 of {file} in {elapsed}", file.FullName, sw.Elapsed);

        var formatted = new StringBuilder(2 * hash.Length);
        foreach (var b in hash)
        {
            formatted.Append($"{b:X2}");
        }

        return new Sha1Hash(formatted.ToString());
    }
}
