namespace b2sync;

public interface IFileChecksumCalculator
{
    string CalculateSha1(FileInfo file);
}
