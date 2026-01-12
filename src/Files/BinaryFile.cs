using System.Buffers.Binary;
using System.Diagnostics;

namespace AppleIIDiskReader.Files;

/// <summary>
/// Represents a binary file on an Apple II disk.
/// </summary>
public readonly struct BinaryFile
{
    /// <summary>
    /// The minimum size of a binary file in bytes (address + length).
    /// </summary>
    public const int MinSize = 4;

    /// <summary>
    /// The load address of the binary file.
    /// </summary>
    public ushort Address { get; }

    /// <summary>
    /// The length of the binary file in bytes.
    /// </summary>
    public ushort Length { get; }

    /// <summary>
    /// The data of the binary file.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryFile"/> struct.
    /// </summary>
    /// <param name="data">A span containing the binary file data.</param>
    public BinaryFile(ReadOnlySpan<byte> data)
    {
        if (data.Length < MinSize)
        {
            // Handle short/empty binary files - treat as empty data with no header.
            Address = 0;
            Length = 0;
            Data = data.ToArray();
            return;
        }

        int offset = 0;

        Address = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        Length = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        // Use the minimum of the specified length and available data to handle
        // truncated files or files with incorrect length headers.
        int actualLength = Math.Min(Length, data.Length - 4);
        Data = data.Slice(4, actualLength).ToArray();
        offset += actualLength;

        // Because each sector is 256 bytes, we may have some padding at the end.
        Debug.Assert(offset <= data.Length, "Did not consume all bytes.");
    }
}
