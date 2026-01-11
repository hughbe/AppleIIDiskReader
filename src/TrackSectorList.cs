using System.Buffers.Binary;

namespace AppleIIDiskReader;

/// <summary>
/// Represents a Track/Sector List sector in an Apple II DOS 3.3 disk.
/// This structure maps out which sectors on the disk belong to a file.
/// </summary>
public readonly struct TrackSectorList
{
    /// <summary>
    /// The size of a Track/Sector List sector in bytes.
    /// </summary>
    public const int Size = 256;

    /// <summary>
    /// The maximum number of track/sector pairs in a T/S list sector.
    /// (256 - 12 header bytes) / 2 bytes per pair = 122 pairs
    /// </summary>
    public const int MaxTrackSectorPairs = 122;

    /// <summary>
    /// Offset $00: Not used.
    /// </summary>
    public byte Unused1 { get; }

    /// <summary>
    /// Offset $01: Track number of next T/S list sector, or zero if no more.
    /// </summary>
    public byte NextTrackSectorListTrack { get; }

    /// <summary>
    /// Offset $02: Sector number of next T/S list sector (if one is present).
    /// </summary>
    public byte NextTrackSectorListSector { get; }

    /// <summary>
    /// Offset $03-04: Not used.
    /// </summary>
    public ushort Unused2 { get; }

    /// <summary>
    /// Offset $05-06: Sector offset in file of the first sector described by this list.
    /// </summary>
    public ushort SectorOffsetInFile { get; }

    /// <summary>
    /// Offset $07-0B: Not used (5 bytes).
    /// </summary>
    public byte[] Unused3 { get; }

    /// <summary>
    /// Offset $0C-FF: Track and sector pairs for data sectors.
    /// Each pair is 2 bytes: track followed by sector.
    /// A pair of zeros indicates no more data sectors or a sparse file hole.
    /// </summary>
    public TrackSectorPair[] DataSectors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackSectorList"/> struct.
    /// </summary>
    /// <param name="data">A 256-byte span containing the T/S list sector data.</param>
    /// <exception cref="ArgumentException">Thrown when data is not exactly 256 bytes.</exception>
    public TrackSectorList(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be {Size} bytes in length.", nameof(data));
        }

        int offset = 0;

        // $00: Not used
        Unused1 = data[offset];
        offset += 1;

        // $01: Track number of next T/S list sector
        NextTrackSectorListTrack = data[offset];
        offset += 1;

        // $02: Sector number of next T/S list sector
        NextTrackSectorListSector = data[offset];
        offset += 1;

        // $03-04: Not used
        Unused2 = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        // $05-06: Sector offset in file
        SectorOffsetInFile = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        // $07-0B: Not used (5 bytes)
        Unused3 = data.Slice(offset, 5).ToArray();
        offset += 5;

        // $0C-FF: Track and sector pairs (122 pairs max)
        DataSectors = new TrackSectorPair[MaxTrackSectorPairs];
        for (int i = 0; i < MaxTrackSectorPairs; i++)
        {
            DataSectors[i] = new TrackSectorPair(data[offset], data[offset + 1]);
            offset += 2;
        }
    }

    /// <summary>
    /// Gets a value indicating whether there is another T/S list sector after this one.
    /// </summary>
    public bool HasNextTrackSectorList => NextTrackSectorListTrack != 0;
}
