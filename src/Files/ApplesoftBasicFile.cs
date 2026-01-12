using System.Buffers.Binary;
using System.Diagnostics;
using Microsoft.VisualBasic;

namespace AppleIIDiskReader.Files;

/// <summary>
/// Represents an Applesoft BASIC file on an Apple II disk.
/// </summary>
public readonly struct ApplesoftBasicFile
{
    /// <summary>
    /// The minimum size of an Applesoft BASIC file in bytes (length).
    /// </summary>
    public const int MinSize = 2;

    /// <summary>
    /// The length of the Applesoft BASIC file in bytes.
    /// </summary>
    public ushort Length { get; }

    /// <summary>
    /// The data of the Applesoft BASIC file.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplesoftBasicFile"/> struct.
    /// </summary>
    /// <param name="data">A span containing the Applesoft BASIC file data.</param>
    /// <exception cref="ArgumentException">Thrown when data is less than the minimum size or does not match the specified length.</exception>
    public ApplesoftBasicFile(ReadOnlySpan<byte> data)
    {
        if (data.Length < MinSize)
        {
            throw new ArgumentException($"Data must be at least {MinSize} bytes long.", nameof(data));
        }

        int offset = 0;

        Length = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        if (data.Length < 2 + Length)
        {
            throw new ArgumentException("Data length does not match the specified length.", nameof(data));
        }

        Data = data.Slice(offset, Length).ToArray();
        offset += Length;

        // Because each sector is 256 bytes, we may have some padding at the end.
        Debug.Assert(offset <= data.Length, "Did not consume all bytes.");
    }
}