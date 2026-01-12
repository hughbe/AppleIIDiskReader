using System.Diagnostics;

namespace AppleIIDiskReader;

/// <summary>
/// Represents a track/sector pair pointing to a data sector.
/// </summary>
public readonly struct TrackSectorPair
{
    /// <summary>
    /// The size of a Track/Sector pair in bytes.
    /// </summary>
    public const int Size = 2;

    /// <summary>
    /// The track number of the data sector.
    /// </summary>
    public byte Track { get; }

    /// <summary>
    /// The sector number of the data sector.
    /// </summary>
    public byte Sector { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackSectorPair"/> struct.
    /// </summary>
    /// <param name="data">A span containing the track/sector pair data.</param>
    /// <exception cref="ArgumentException">Thrown when data is not exactly 2 bytes.</exception>
    public TrackSectorPair(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be {Size} bytes in length.", nameof(data));
        }

        int offset = 0;

        Track = data[offset];
        offset += 1;

        Sector = data[offset];
        offset += 1;

        Debug.Assert(offset == data.Length, "Did not consume all pair bytes.");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackSectorPair"/> struct.
    /// </summary>
    /// <param name="track">The track number.</param>
    /// <param name="sector">The sector number.</param>
    public TrackSectorPair(byte track, byte sector)
    {
        Track = track;
        Sector = sector;
    }

    /// <summary>
    /// Gets a value indicating whether this pair is empty (both track and sector are zero).
    /// </summary>
    public bool IsEmpty => Track == 0 && Sector == 0;
}
