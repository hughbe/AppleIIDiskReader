using AppleIIDiskReader.Files;
using AppleIIDiskReader.Utilities;

namespace AppleIIDiskReader;

/// <summary>
/// Represents an Apple II disk.
/// </summary>
public class AppleIIDisk : FloppyDisk
{
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
        byte track = VolumeTableOfContents.FirstCatalogTrack;
        byte sector = VolumeTableOfContents.FirstCatalogSector;

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
        Span<byte> buffer = stackalloc byte[256];
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
        Span<byte> buffer = stackalloc byte[256];
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

        byte track = fileEntry.FirstTrackSectorListTrack;
        byte sector = fileEntry.FirstTrackSectorListSector;

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

        // Calculate total size based on sectors
        int totalBytes = fileEntry.LengthInSectors * SectorSize;
        byte[] data = new byte[totalBytes];
        int dataOffset = 0;

        Span<byte> sectorBuffer = stackalloc byte[256];

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

                    // Sparse hole - fill with zeros (already initialized to zero)
                    dataOffset += SectorSize;
                    continue;
                }

                if (dataOffset >= totalBytes)
                {
                    break;
                }

                ReadSector(pair.Track, pair.Sector, sectorBuffer);

                int bytesToCopy = Math.Min(SectorSize, totalBytes - dataOffset);
                sectorBuffer.Slice(0, bytesToCopy).CopyTo(data.AsSpan(dataOffset));
                dataOffset += bytesToCopy;
            }
        }

        return data;
    }

    /// <summary>
    /// Reads the raw data of a file from the disk into a stream.
    /// </summary>
    /// <param name="fileEntry">The file descriptive entry.</param>
    /// <param name="destination">The destination stream to write the file data to.</param>
    public void ReadFileData(FileDescriptiveEntry fileEntry, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        byte[] data = ReadFileData(fileEntry);
        destination.Write(data);
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

        byte[] data = ReadFileData(fileEntry);
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

        byte[] data = ReadFileData(fileEntry);
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

        byte[] data = ReadFileData(fileEntry);
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

        byte[] data = ReadFileData(fileEntry);
        return new IntegerBasicFile(data);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppleIIDisk"/> class.
    /// </summary>
    /// <param name="stream">The stream representing the Apple II disk data.</param>
    public AppleIIDisk(Stream stream)
        : base(stream, numberOfTracks: 35, numberOfSectors: 16, sectorSize: 256)
    {
        // Read the Volume Table of Contents (VTOC) from track 17, sector 0
        Span<byte> buffer = stackalloc byte[256];
        ReadSector(0x11, 0x00, buffer);
        VolumeTableOfContents = new VolumeTableOfContents(buffer);
    }
}
