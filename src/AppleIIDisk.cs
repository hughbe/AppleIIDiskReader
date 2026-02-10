using AppleIIDiskReader.Files;
using AppleIIDiskReader.Utilities;

namespace AppleIIDiskReader;

/// <summary>
/// Represents an Apple II disk.
/// </summary>
public class AppleIIDisk : FloppyDisk
{
    private const int AppleIIDiskSectorSize = 256;

    private const int MinimumTracks = 35;

    /// <summary>
    /// The Volume Table of Contents (VTOC) for the disk.
    /// </summary>
    public VolumeTableOfContents VolumeTableOfContents { get; }

    /// <summary>
    /// Gets the first catalog entry from the VTOC.
    /// </summary>
    public CatalogEntry FirstCatalogEntry
        => GetCatalogEntry(VolumeTableOfContents.FirstCatalogTrack, VolumeTableOfContents.FirstCatalogSector);
        
    /// <summary>
    /// Enumerates all catalog entries on the disk.
    /// </summary>
    /// <returns>>An enumerable of <see cref="CatalogEntry"/> objects.</returns>
    public IEnumerable<CatalogEntry> EnumerateCatalogEntries()
    {
        var track = VolumeTableOfContents.FirstCatalogTrack;
        var sector = VolumeTableOfContents.FirstCatalogSector;

        while (track != 0)
        {
            var catalogEntry = GetCatalogEntry(track, sector);
            yield return catalogEntry;

            track = catalogEntry.NextCatalogTrack;
            sector = catalogEntry.NextCatalogSector;
        }
    }

    /// <summary>
    /// Enumerates all file descriptive entries on the disk.
    /// </summary>
    /// <returns>>An enumerable of <see cref="FileDescriptiveEntry"/> objects.</returns>
    public IEnumerable<FileDescriptiveEntry> EnumerateFileEntries()
    {
        foreach (var catalogEntry in EnumerateCatalogEntries())
        {
            foreach (var fileEntry in catalogEntry.FileEntries)
            {
                if (fileEntry.FirstTrackSectorListTrack == 0)
                {
                    // Unused entry.
                    continue;
                }

                yield return fileEntry;
            }
        }
    }

    /// <summary>
    /// Gets a catalog entry from the specified track and sector.
    /// </summary>
    /// <param name="track">The track number.</param>
    /// <param name="sector">The sector number.</param>
    /// <returns>>The <see cref="CatalogEntry"/> at the specified location.</returns>
    public CatalogEntry GetCatalogEntry(byte track, byte sector)
    {
        Span<byte> buffer = stackalloc byte[SectorSize];
        ReadSector(track, sector, buffer);
        return new CatalogEntry(buffer);
    }

    /// <summary>
    /// Gets a track/sector list from the specified track and sector.
    /// </summary>
    /// <param name="track">The track number.</param>
    /// <param name="sector">The sector number.</param>
    /// <returns>The <see cref="TrackSectorList"/> at the specified location.</returns>
    public TrackSectorList GetTrackSectorList(byte track, byte sector)
    {
        Span<byte> buffer = stackalloc byte[SectorSize];
        ReadSector(track, sector, buffer);
        return new TrackSectorList(buffer);
    }

    /// <summary>
    /// Enumerates all track/sector lists for a file.
    /// </summary>
    /// <param name="fileEntry">The file descriptive entry.</param>
    /// <returns>An enumerable of <see cref="TrackSectorList"/> objects.</returns>
    public IEnumerable<TrackSectorList> EnumerateTrackSectorLists(FileDescriptiveEntry fileEntry)
    {
        if (fileEntry.IsUnused || fileEntry.IsDeleted)
        {
            yield break;
        }

        var track = fileEntry.FirstTrackSectorListTrack;
        var sector = fileEntry.FirstTrackSectorListSector;

        while (track != 0)
        {
            var tsList = GetTrackSectorList(track, sector);
            yield return tsList;

            track = tsList.NextTrackSectorListTrack;
            sector = tsList.NextTrackSectorListSector;
        }
    }

    /// <summary>
    /// Reads the raw data of a file from the disk.
    /// </summary>
    /// <param name="fileEntry">The file descriptive entry.</param>
    /// <returns>A byte array containing the file data.</returns>
    public byte[] ReadFileData(FileDescriptiveEntry fileEntry)
    {
        if (fileEntry.IsUnused)
        {
            throw new ArgumentException("Cannot read data from an unused file entry.", nameof(fileEntry));
        }

        if (fileEntry.IsDeleted)
        {
            throw new ArgumentException("Cannot read data from a deleted file entry.", nameof(fileEntry));
        }

        var totalBytes = fileEntry.LengthInSectors * SectorSize;
        var result = new byte[totalBytes];
        int dataOffset = 0;

        Span<byte> sectorBuffer = stackalloc byte[SectorSize];

        foreach (var tsList in EnumerateTrackSectorLists(fileEntry))
        {
            foreach (var pair in tsList.DataSectors)
            {
                if (pair.IsEmpty)
                {
                    if (dataOffset >= totalBytes)
                    {
                        break;
                    }

                    // Sparse hole - result array is already zeroed
                    dataOffset += SectorSize;
                    continue;
                }

                if (dataOffset >= totalBytes)
                {
                    break;
                }

                ReadSector(pair.Track, pair.Sector, sectorBuffer);

                var bytesToCopy = Math.Min(SectorSize, totalBytes - dataOffset);
                sectorBuffer[..bytesToCopy].CopyTo(result.AsSpan(dataOffset));
                dataOffset += bytesToCopy;
            }
        }

        return result;
    }

    /// <summary>
    /// Reads the raw data of a file from the disk into a stream.
    /// </summary>
    /// <param name="fileEntry">The file descriptive entry.</param>
    /// <param name="destination">The destination stream to write the file data to.</param>
    public int ReadFileData(FileDescriptiveEntry fileEntry, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (fileEntry.IsUnused)
        {
            throw new ArgumentException("Cannot read data from an unused file entry.", nameof(fileEntry));
        }

        if (fileEntry.IsDeleted)
        {
            throw new ArgumentException("Cannot read data from a deleted file entry.", nameof(fileEntry));
        }

        // Calculate total size based on sectors
        var totalBytes = fileEntry.LengthInSectors * SectorSize;
        int dataOffset = 0;

        Span<byte> sectorBuffer = stackalloc byte[SectorSize];

        foreach (var tsList in EnumerateTrackSectorLists(fileEntry))
        {
            foreach (var pair in tsList.DataSectors)
            {
                if (pair.IsEmpty)
                {
                    // Sparse file hole or end of data - write zeros
                    // For end of data, we may have reached the end
                    if (dataOffset >= totalBytes)
                    {
                        break;
                    }

                    // Sparse hole - fill with zeros (reuse stackalloc'd buffer)
                    sectorBuffer.Clear();
                    destination.Write(sectorBuffer);
                    dataOffset += SectorSize;
                    continue;
                }

                if (dataOffset >= totalBytes)
                {
                    break;
                }

                ReadSector(pair.Track, pair.Sector, sectorBuffer);

                var bytesToCopy = Math.Min(SectorSize, totalBytes - dataOffset);
                destination.Write(sectorBuffer[..bytesToCopy]);
                dataOffset += bytesToCopy;
            }
        }

        return dataOffset;
    }

    /// <summary>
    /// Reads a text file from the disk and returns it as a string.
    /// Apple II text files use high ASCII (bit 7 set) and are terminated with null bytes.
    /// </summary>
    /// <param name="fileEntry">The file descriptive entry.</param>
    /// <returns>The text content of the file.</returns>
    public TextFile ReadTextFile(FileDescriptiveEntry fileEntry)
    {
        if (fileEntry.FileType != AppleIIFileType.Text)
        {
            throw new ArgumentException("The specified file entry is not a text file.", nameof(fileEntry));
        }

        var data = ReadFileData(fileEntry);
        return new TextFile(data);
    }

    /// <summary>
    /// Reads a binary file from the disk.
    /// </summary>
    /// <param name="fileEntry">The file descriptive entry.</param>
    /// <returns>>The <see cref="BinaryFile"/> object representing the binary file.</returns>
    public BinaryFile ReadBinaryFile(FileDescriptiveEntry fileEntry)
    {
        if (fileEntry.FileType != AppleIIFileType.Binary)
        {
            throw new ArgumentException("The specified file entry is not a binary file.", nameof(fileEntry));
        }

        var data = ReadFileData(fileEntry);
        return new BinaryFile(data);
    }

    /// <summary>
    /// Reads an Applesoft BASIC file from the disk.
    /// </summary>
    /// <param name="fileEntry">The file descriptive entry.</param>
    /// <returns>>The <see cref="ApplesoftBasicFile"/> object representing the Applesoft BASIC file.</returns>
    /// <exception cref="ArgumentException">Thrown when the specified file entry is not an Applesoft BASIC file.</exception>
    public ApplesoftBasicFile ReadApplesoftBasicFile(FileDescriptiveEntry fileEntry)
    {
        if (fileEntry.FileType != AppleIIFileType.ApplesoftBasic)
        {
            throw new ArgumentException("The specified file entry is not an Applesoft BASIC file.", nameof(fileEntry));
        }

        var data = ReadFileData(fileEntry);
        return new ApplesoftBasicFile(data);
    }

    /// <summary>
    /// Reads an Integer BASIC file from the disk.
    /// </summary>
    /// <param name="fileEntry">The file descriptive entry.</param>
    /// <returns>>The <see cref="IntegerBasicFile"/> object representing the Integer BASIC file.</returns>
    /// <exception cref="ArgumentException">>Thrown when the specified file entry is not an Integer BASIC file.</exception>
    public IntegerBasicFile ReadIntegerBasicFile(FileDescriptiveEntry fileEntry)
    {
        if (fileEntry.FileType != AppleIIFileType.IntegerBasic)
        {
            throw new ArgumentException("The specified file entry is not an Integer BASIC file.", nameof(fileEntry));
        }

        var data = ReadFileData(fileEntry);
        return new IntegerBasicFile(data);
    }

    /// <summary>
    /// Reads a Relocatable binary file from the disk.
    /// </summary>
    /// <param name="fileEntry">The file descriptive entry.</param>
    /// <returns>>The <see cref="RelocatableBinaryFile"/> object representing the Relocatable binary file.</returns>
    /// <exception cref="ArgumentException">>Thrown when the specified file entry is not a Relocatable file.</exception>
    public RelocatableBinaryFile ReadRelocatableFile(FileDescriptiveEntry fileEntry)
    {
        if (fileEntry.FileType != AppleIIFileType.Relocatable)
        {
            throw new ArgumentException("The specified file entry is not a Relocatable file.", nameof(fileEntry));
        }

        var data = ReadFileData(fileEntry);
        return new RelocatableBinaryFile(data);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppleIIDisk"/> class.
    /// </summary>
    /// <param name="stream">The stream representing the Apple II disk data.</param>
    public AppleIIDisk(Stream stream)
        : base(stream, numberOfTracks: DetectNumberOfTracks(stream), numberOfSectors: DetectSectorsPerTrack(stream), sectorSize: AppleIIDiskSectorSize)
    {
        // Read the Volume Table of Contents (VTOC) from track 17, sector 0
        Span<byte> buffer = stackalloc byte[AppleIIDiskSectorSize];
        ReadSector(0x11, 0x00, buffer);
        VolumeTableOfContents = new VolumeTableOfContents(buffer);
    }

    /// <summary>
    /// Detects the number of tracks from the stream length and sectors per track.
    /// Returns at least <see cref="MinimumTracks"/> to handle partial disk images.
    /// </summary>
    private static int DetectNumberOfTracks(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        int sectorsPerTrack = DetectSectorsPerTrack(stream);
        int tracks = (int)(stream.Length / (sectorsPerTrack * AppleIIDiskSectorSize));
        return Math.Max(tracks, MinimumTracks);
    }

    /// <summary>
    /// Reads the sectors per track value from the VTOC sector in the stream.
    /// Uses the stream length to locate the VTOC (track 17, sector 0), then
    /// reads the actual SectorsPerTrack field at offset $35.
    /// </summary>
    private static int DetectSectorsPerTrack(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Use file size to determine where the VTOC sector lives.
        // The VTOC is at track 17, sector 0, so its byte offset depends on sectors per track.
        // If divisible by 16*256, assume 16 spt; else if divisible by 13*256, assume 13.
        int likelySectors = stream.Length % (16 * AppleIIDiskSectorSize) == 0 ? 16
            : stream.Length % (13 * AppleIIDiskSectorSize) == 0 ? 13
            : 16;
        long vtocOffset = 17L * likelySectors * AppleIIDiskSectorSize;
        const int sectorsPerTrackField = 0x35;

        // If the image is too small to contain the VTOC, fall back to the size-based estimate.
        if (stream.Length < vtocOffset + sectorsPerTrackField + 1)
            return likelySectors;

        stream.Seek(vtocOffset + sectorsPerTrackField, SeekOrigin.Begin);
        int value = stream.ReadByte();

        if (value is 13 or 16)
            return value;

        return likelySectors;
    }
}
