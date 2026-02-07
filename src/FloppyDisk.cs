namespace AppleIIDiskReader;

/// <summary>
/// Represents a generic floppy disk.
/// </summary>
public class FloppyDisk
{
    /// <summary>
    /// The underlying stream representing the floppy disk data.
    /// </summary>
    private readonly Stream _stream;

    /// <summary>
    /// The number of tracks on the floppy disk.
    /// </summary>
    public int NumberOfTracks { get; }

    /// <summary>
    /// The number of sectors per track on the floppy disk.
    /// </summary>
    public int NumberOfSectors { get; }

    /// <summary>
    /// The size of each sector in bytes.
    /// </summary>
    public int SectorSize { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FloppyDisk"/> class.
    /// </summary>
    /// <param name="stream">The stream representing the floppy disk data.</param>
    /// <param name="numberOfTracks">The number of tracks on the floppy disk.</param>
    /// <param name="numberOfSectors">The number of sectors per track on the floppy disk.</param>
    /// <param name="sectorSize">The size of each sector in bytes.</param>
    public FloppyDisk(Stream stream, int numberOfTracks, int numberOfSectors, int sectorSize)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek || !stream.CanRead)
        {
            throw new ArgumentException("Stream must be seekable and readable.", nameof(stream));
        }

        _stream = stream;
        NumberOfTracks = numberOfTracks;
        NumberOfSectors = numberOfSectors;
        SectorSize = sectorSize;
    }

    /// <summary>
    /// Reads a sector from the floppy disk into the provided buffer.
    /// </summary>
    /// <param name="track">The track number to read from.</param>
    /// <param name="sector">The sector number to read from.</param>
    /// <param name="buffer">The buffer to read the sector data into.</param>
    /// <returns>The number of bytes read.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when track or sector is out of range.</exception>
    /// <exception cref="ArgumentException">Thrown when buffer is too small.</exception>
    public int ReadSector(int track, int sector, Span<byte> buffer)
    {
        if (track < 0 || track >= NumberOfTracks)
        {
            throw new ArgumentOutOfRangeException(nameof(track), "Track number is out of range.");
        }

        if (sector < 0 || sector >= NumberOfSectors)
        {
            throw new ArgumentOutOfRangeException(nameof(sector), "Sector number is out of range.");
        }

        if (buffer.Length < SectorSize)
        {
            throw new ArgumentException("Buffer is too small to hold sector data.", nameof(buffer));
        }

        var offset = (track * NumberOfSectors + sector) * SectorSize;
        _stream.Seek(offset, SeekOrigin.Begin);
        return _stream.Read(buffer);
    }
}
