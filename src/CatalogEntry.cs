namespace AppleIIDiskReader;

/// <summary>
/// Represents a Catalog Sector in an Apple II DOS 3.3 disk.
/// Each catalog sector contains up to 7 file descriptive entries and a link to the next catalog sector.
/// </summary>
public readonly struct CatalogEntry
{
    /// <summary>
    /// The size of a Catalog Sector in bytes.
    /// </summary>
    public const int Size = 256;

    /// <summary>
    /// The number of file descriptive entries per catalog sector.
    /// </summary>
    public const int FileEntriesPerSector = 7;

    /// <summary>
    /// Offset $00: Not used.
    /// </summary>
    public byte Unused1 { get; }

    /// <summary>
    /// Offset $01: Track number of next catalog sector.
    /// A value of 0 indicates this is the last catalog sector.
    /// </summary>
    public byte NextCatalogTrack { get; }

    /// <summary>
    /// Offset $02: Sector number of next catalog sector.
    /// </summary>
    public byte NextCatalogSector { get; }

    /// <summary>
    /// Offset $03-0A: Not used (8 bytes).
    /// </summary>
    public byte[] Unused2 { get; }

    /// <summary>
    /// The file descriptive entries in this catalog sector.
    /// There are up to 7 entries per sector, starting at offset $0B.
    /// </summary>
    public FileDescriptiveEntry[] FileEntries { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogEntry"/> struct.
    /// </summary>
    /// <param name="data">A 256-byte span containing the catalog sector data.</param>
    /// <exception cref="ArgumentException">Thrown when data is not exactly 256 bytes.</exception>
    public CatalogEntry(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be {Size} bytes in length.", nameof(data));
        }

        int offset = 0;

        // $00: Not used
        Unused1 = data[offset];
        offset += 1;

        // $01: Track number of next catalog sector
        NextCatalogTrack = data[offset];
        offset += 1;

        // $02: Sector number of next catalog sector
        NextCatalogSector = data[offset];
        offset += 1;

        // $03-0A: Not used (8 bytes)
        Unused2 = data.Slice(offset, 8).ToArray();
        offset += 8;

        // $0B-FF: Seven file descriptive entries
        // Each entry is 35 bytes ($23)
        // $0B-2D: First entry
        // $2E-50: Second entry
        // $51-73: Third entry
        // $74-96: Fourth entry
        // $97-B9: Fifth entry
        // $BA-DC: Sixth entry
        // $DD-FF: Seventh entry
        FileEntries = new FileDescriptiveEntry[FileEntriesPerSector];
        for (int i = 0; i < FileEntriesPerSector; i++)
        {
            FileEntries[i] = new FileDescriptiveEntry(data.Slice(offset, FileDescriptiveEntry.Size));
            offset += FileDescriptiveEntry.Size;
        }
    }

    /// <summary>
    /// Gets a value indicating whether there is another catalog sector after this one.
    /// </summary>
    public bool HasNextCatalogSector => NextCatalogTrack != 0;
}