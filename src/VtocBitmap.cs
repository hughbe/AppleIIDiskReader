using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AppleIIDiskReader;

/// <summary>
/// A bitmap representing used and free sectors on the disk.
/// </summary>
[InlineArray(Size)]
public struct VtocBitMap
{
    /// <summary>
    /// The size of the bitmap in bytes.
    /// </summary>
    public const int Size = 200;

    /// <summary>
    /// The first element of the bitmap.
    /// </summary>
    public byte element0;

    /// <summary>
    /// Initializes a new instance of the <see cref="VtocBitMap"/> struct.
    /// </summary>
    public VtocBitMap(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be exactly {Size} bytes long.", nameof(data));
        }

        data.CopyTo(AsSpan());
    }

    /// <summary>
    /// Gets a span over the bitmap bytes.
    /// </summary>
    /// <returns>>A span containing the bitmap bytes.</returns>
    public readonly Span<byte> AsSpan() =>
        MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in element0), Size);

    /// <summary>
    /// Gets a read-only span over the bitmap bytes.
    /// </summary>
    /// <returns>A read-only span containing the bitmap bytes.</returns>
    public readonly ReadOnlySpan<byte> AsReadOnlySpan() =>
        MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in element0), Size);

    /// <summary>
    /// Gets the free sector bitmap for a specific track.
    /// </summary>
    /// <param name="track">The track number (0-based).</param>
    /// <returns>A 4-byte span representing the free sector bitmap for the track.</returns>
    public readonly ReadOnlySpan<byte> GetFreeSectorBitMap(int track)
    {
        if (track < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(track), "Track number must be non-negative.");
        }

        int bitmapOffset = track * 4;
        if (bitmapOffset + 4 > Size)
        {
            throw new ArgumentOutOfRangeException(nameof(track), "Track number exceeds available bitmap data.");
        }

        return AsReadOnlySpan().Slice(bitmapOffset, 4);
    }

    /// <summary>
    /// Checks if a specific sector on a track is free.
    /// </summary>
    /// <param name="track">The track number (0-based).</param>
    /// <param name="sector">The sector number (0-based).</param>
    /// <param name="sectorsPerTrack">The number of sectors per track.</param>
    /// <returns>True if the sector is free; otherwise, false.</returns>
    public readonly bool IsSectorFree(int track, int sector, int sectorsPerTrack)
    {
        if (sector < 0 || sector >= sectorsPerTrack)
        {
            throw new ArgumentOutOfRangeException(nameof(sector), "Sector number is out of range.");
        }

        if (track < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(track), "Track number must be non-negative.");
        }

        int bitmapOffset = track * 4;
        if (bitmapOffset + 4 > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(track), "Track number exceeds available bitmap data.");
        }

        ReadOnlySpan<byte> bitmap = AsReadOnlySpan().Slice(bitmapOffset, 4);

        // The bitmap is stored in big-endian format with sectors mapped to bits
        // Sector 0 is the MSB of the first byte, etc.
        int byteIndex = sector / 8;
        int bitIndex = 7 - (sector % 8);

        return (bitmap[byteIndex] & (1 << bitIndex)) != 0;
    }
}
