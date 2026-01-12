using BenchmarkDotNet.Attributes;
using AppleIIDiskReader;

namespace AppleIIDiskReader.Benchmarks;

/// <summary>
/// Benchmarks for reading and enumerating Apple II disk images.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class AppleIIDiskBenchmarks
{
    private byte[] _diskData = null!;
    private const string SampleDiskPath = "Samples/appleIIDos.dsk";

    [GlobalSetup]
    public void Setup()
    {
        // Load the disk image into memory once to avoid I/O overhead in benchmarks
        _diskData = File.ReadAllBytes(SampleDiskPath);
    }

    [Benchmark(Description = "Create AppleIIDisk from stream")]
    public AppleIIDisk CreateDisk()
    {
        using var stream = new MemoryStream(_diskData);
        return new AppleIIDisk(stream);
    }

    [Benchmark(Description = "Enumerate catalog entries")]
    public int EnumerateCatalogEntries()
    {
        using var stream = new MemoryStream(_diskData);
        var disk = new AppleIIDisk(stream);

        int count = 0;
        foreach (var entry in disk.EnumerateCatalogEntries())
        {
            count++;
        }
        return count;
    }

    [Benchmark(Description = "Enumerate file entries")]
    public int EnumerateFileEntries()
    {
        using var stream = new MemoryStream(_diskData);
        var disk = new AppleIIDisk(stream);

        int count = 0;
        foreach (var entry in disk.EnumerateFileEntries())
        {
            count++;
        }
        return count;
    }

    [Benchmark(Description = "Enumerate file entries with names")]
    public int EnumerateFileEntriesWithNames()
    {
        using var stream = new MemoryStream(_diskData);
        var disk = new AppleIIDisk(stream);

        int totalNameLength = 0;
        foreach (var entry in disk.EnumerateFileEntries())
        {
            totalNameLength += entry.FileName.Length;
        }
        return totalNameLength;
    }

    [Benchmark(Description = "Read all file data")]
    public long ReadAllFileData()
    {
        using var stream = new MemoryStream(_diskData);
        var disk = new AppleIIDisk(stream);

        long totalBytes = 0;
        foreach (var entry in disk.EnumerateFileEntries())
        {
            byte[] data = disk.ReadFileData(entry);
            totalBytes += data.Length;
        }
        return totalBytes;
    }

    [Benchmark(Description = "Read VTOC properties")]
    public int ReadVtocProperties()
    {
        using var stream = new MemoryStream(_diskData);
        var disk = new AppleIIDisk(stream);

        var vtoc = disk.VolumeTableOfContents;
        return vtoc.DiskVolumeNumber + vtoc.TracksPerDiskette + vtoc.SectorsPerTrack + vtoc.BytesPerSector;
    }

    [Benchmark(Description = "Enumerate track/sector lists for all files")]
    public int EnumerateTrackSectorLists()
    {
        using var stream = new MemoryStream(_diskData);
        var disk = new AppleIIDisk(stream);

        int count = 0;
        foreach (var entry in disk.EnumerateFileEntries())
        {
            foreach (var tsList in disk.EnumerateTrackSectorLists(entry))
            {
                count++;
            }
        }
        return count;
    }
}
