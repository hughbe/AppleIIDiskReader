using System.Buffers.Binary;
using System.Diagnostics;

namespace AppleIIDiskReader;

/// <summary>
/// Represents the Volume Table of Contents (VTOC) for an Apple II DOS 3.3 disk.
/// The VTOC is located at track 17, sector 0 and contains disk layout information
/// and free sector bitmaps.
/// </summary>
public readonly struct VolumeTableOfContents
{
    /// <summary>
    /// The size of the VTOC structure in bytes.
    /// </summary>
    public const int Size = 256;

    /// <summary>
    /// Offset $00: Not used.
    /// </summary>
    public byte Unused1 { get; }

    /// <summary>
    /// Offset $01: Track number of first catalog sector.
    /// </summary>
    public byte FirstCatalogTrack { get; }

    /// <summary>
    /// Offset $02: Sector number of first catalog sector.
    /// </summary>
    public byte FirstCatalogSector { get; }

    /// <summary>
    /// Offset $03: Release number of DOS used to INIT this disk.
    /// </summary>
    public byte ReleaseNumber { get; }

    /// <summary>
    /// Offset $04-05: Not used.
    /// </summary>
    public ushort Unused2 { get; }

    /// <summary>
    /// Offset $06: Diskette volume number (1-254).
    /// </summary>
    public byte DiskVolumeNumber { get; }

    /// <summary>
    /// Offset $07-26: Not used.
    /// </summary>
    public byte[] Unused3 { get; }

    /// <summary>
    /// Offset $27: Maximum number of track/sector pairs which will fit in one file
    /// track/sector list sector (122 for 256 byte sectors).
    /// </summary>
    public byte MaxTrackSectorPairs { get; }

    /// <summary>
    /// Offset $28-2F: Not used.
    /// </summary>
    public byte[] Unused4 { get; }

    /// <summary>
    /// Offset $30: Last track where sectors were allocated.
    /// </summary>
    public byte LastAllocatedTrack { get; }

    /// <summary>
    /// Offset $31: Direction of track allocation (+1 or -1).
    /// </summary>
    public sbyte TrackAllocationDirection { get; }

    /// <summary>
    /// Offset $32-33: Not used.
    /// </summary>
    public ushort Unused5 { get; }

    /// <summary>
    /// Offset $34: Number of tracks per diskette (normally 35).
    /// </summary>
    public byte TracksPerDiskette { get; }

    /// <summary>
    /// Offset $35: Number of sectors per track (13 or 16).
    /// </summary>
    public byte SectorsPerTrack { get; }

    /// <summary>
    /// Offset $36-37: Number of bytes per sector (LO/HI format).
    /// </summary>
    public ushort BytesPerSector { get; }

    /// <summary>
    /// Offset $38-FF: Bit maps of free sectors for each track.
    /// Each track uses 4 bytes. Starting at $38 for track 0, $3C for track 1, etc.
    /// </summary>
    public byte[] FreeSectorBitMaps { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VolumeTableOfContents"/> struct.
    /// </summary>
    /// <param name="data">A 256-byte span containing the VTOC sector data.</param>
    /// <exception cref="ArgumentException">Thrown when data is not exactly 256 bytes.</exception>
    public VolumeTableOfContents(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be {Size} bytes in length.", nameof(data));
        }

        // Structure documented in http://justsolve.archiveteam.org/wiki/Apple_DOS_file_system
        int offset = 0;

        // $00: not used
        Unused1 = data[offset];
        offset += 1;

        // $01: track number of first catalog sector
        FirstCatalogTrack = data[offset];
        offset += 1;

        // $02: sector number of first catalog sector
        FirstCatalogSector = data[offset];
        offset += 1;

        // $03: release number of DOS used to INIT this disk
        ReleaseNumber = data[offset];
        offset += 1;

        // $04-05: not used
        Unused2 = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        // $06: Diskette volume number (1-254)
        DiskVolumeNumber = data[offset];
        offset += 1;

        // $07-26: not used (32 bytes: 0x07 to 0x26 inclusive)
        Unused3 = data.Slice(offset, 0x20).ToArray();
        offset += 0x20;

        // $27: maximum number of track/sector pairs which will fit in
        // one file track/sector list sector (122 for 256 byte sectors)
        MaxTrackSectorPairs = data[offset];
        offset += 1;

        // $28-2F: not used (8 bytes)
        Unused4 = data.Slice(offset, 8).ToArray();
        offset += 8;

        // $30: last track where sectors were allocated
        LastAllocatedTrack = data[offset];
        offset += 1;

        // $31: direction of track allocation (+1 or -1)
        TrackAllocationDirection = (sbyte)data[offset];
        offset += 1;

        // $32-33: not used
        Unused5 = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        // $34: number of tracks per diskette (normally 35)
        TracksPerDiskette = data[offset];
        offset += 1;

        // $35: number of sectors per track (13 or 16)
        SectorsPerTrack = data[offset];
        offset += 1;

        // $36-37: number of bytes per sector (LO/HI format)
        BytesPerSector = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        // $38-FF: bit maps of free sectors for each track
        // Each track uses 4 bytes. Remaining bytes from offset $38 to end of sector.
        FreeSectorBitMaps = data[offset..].ToArray();
        offset += FreeSectorBitMaps.Length;

        Debug.Assert(offset == data.Length, "Did not consume all data for VTOC structure.");
    }

    /// <summary>
    /// Gets the free sector bitmap for a specific track.
    /// </summary>
    /// <param name="track">The track number (0-based).</param>
    /// <returns>A 4-byte array representing the free sector bitmap for the track.</returns>
    public ReadOnlySpan<byte> GetFreeSectorBitMap(int track)
    {
        if (track < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(track), "Track number must be non-negative.");
        }

        int bitmapOffset = track * 4;
        if (bitmapOffset + 4 > FreeSectorBitMaps.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(track), "Track number exceeds available bitmap data.");
        }

        return FreeSectorBitMaps.AsSpan(bitmapOffset, 4);
    }

    /// <summary>
    /// Checks if a specific sector on a track is free.
    /// </summary>
    /// <param name="track">The track number (0-based).</param>
    /// <param name="sector">The sector number (0-based).</param>
    /// <returns>True if the sector is free; otherwise, false.</returns>
    public bool IsSectorFree(int track, int sector)
    {
        if (sector < 0 || sector >= SectorsPerTrack)
        {
            throw new ArgumentOutOfRangeException(nameof(sector), "Sector number is out of range.");
        }

        ReadOnlySpan<byte> bitmap = GetFreeSectorBitMap(track);
        
        // The bitmap is stored in big-endian format with sectors mapped to bits
        // Sector 0 is the MSB of the first byte, etc.
        int byteIndex = sector / 8;
        int bitIndex = 7 - (sector % 8);
        
        return (bitmap[byteIndex] & (1 << bitIndex)) != 0;
    }
}
