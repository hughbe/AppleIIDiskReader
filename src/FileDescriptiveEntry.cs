using System.Buffers.Binary;
using System.Diagnostics;
using AppleIIDiskReader.Utilities;

namespace AppleIIDiskReader;

/// <summary>
/// Represents a File Descriptive Entry in an Apple II DOS 3.3 catalog sector.
/// Each entry describes a file on the disk including its location, type, name, and size.
/// </summary>
public readonly struct FileDescriptiveEntry
{
    /// <summary>
    /// The size of a File Descriptive Entry in bytes.
    /// </summary>
    public const int Size = 35;

    /// <summary>
    /// Offset $00: Track of first track/sector list sector.
    /// If this is a deleted file, this contains $FF and the original track number
    /// is copied to the last byte of the file name (byte $20).
    /// If this byte contains $00, the entry is assumed to never have been used.
    /// </summary>
    public byte FirstTrackSectorListTrack { get; }

    /// <summary>
    /// Offset $01: Sector of first track/sector list sector.
    /// </summary>
    public byte FirstTrackSectorListSector { get; }

    /// <summary>
    /// Offset $02: File type and flags.
    /// The high bit ($80) indicates if the file is locked.
    /// The lower 7 bits indicate the file type.
    /// </summary>
    public byte FileTypeAndFlags { get; }

    /// <summary>
    /// Offset $03-20: File name (30 characters).
    /// </summary>
    public ByteArray30 FileNameBytes { get; }

    /// <summary>
    /// Offset $21-22: Length of file in sectors (LO/HI format).
    /// </summary>
    public ushort LengthInSectors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDescriptiveEntry"/> struct.
    /// </summary>
    /// <param name="data">A span containing the file descriptive entry data.</param>
    /// <exception cref="ArgumentException">Thrown when data is not exactly the required size.</exception>
    public FileDescriptiveEntry(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be {Size} bytes in length.", nameof(data));
        }

        int offset = 0;

        // $00: Track of first track/sector list sector
        FirstTrackSectorListTrack = data[offset];
        offset += 1;

        // $01: Sector of first track/sector list sector
        FirstTrackSectorListSector = data[offset];
        offset += 1;

        // $02: File type and flags
        FileTypeAndFlags = data[offset];
        offset += 1;

        // $03-20: File name (30 characters)
        FileNameBytes = new ByteArray30(data.Slice(offset, ByteArray30.Size));
        offset += ByteArray30.Size;

        // $21-22: Length of file in sectors (LO/HI format)
        LengthInSectors = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        Debug.Assert(offset == data.Length, "Did not consume all entry bytes.");
    }

    /// <summary>
    /// Gets a value indicating whether this entry has never been used.
    /// </summary>
    public bool IsUnused => FirstTrackSectorListTrack == 0x00;

    /// <summary>
    /// Gets a value indicating whether this file has been deleted.
    /// </summary>
    public bool IsDeleted => FirstTrackSectorListTrack == 0xFF;

    /// <summary>
    /// Gets a value indicating whether this file is locked.
    /// </summary>
    public bool IsLocked => (FileTypeAndFlags & 0x80) != 0;

    /// <summary>
    /// Gets the file type without the locked flag.
    /// </summary>
    public AppleIIFileType FileType => (AppleIIFileType)(FileTypeAndFlags & 0x7F);

    /// <summary>
    /// Gets the original track number for deleted files.
    /// For deleted files, the original track is stored in the last byte of the file name.
    /// </summary>
    public byte OriginalTrackForDeletedFile => IsDeleted ? FileNameBytes[29] : FirstTrackSectorListTrack;

    /// <summary>
    /// Gets the file name as a string.
    /// Apple II file names are stored in high ASCII (with bit 7 set).
    /// </summary>
    public string FileName => AppleIIEncoding.DecodeFileName(FileNameBytes.AsSpan(), IsDeleted);
}
