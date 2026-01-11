namespace AppleIIDiskReader;

/// <summary>
/// Represents a track/sector pair pointing to a data sector.
/// </summary>
public readonly struct TrackSectorPair
{
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
