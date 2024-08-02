namespace b2sync;

public interface IFileChecksumCalculator
{
    Sha1Hash CalculateSha1(FileInfo file);
}
