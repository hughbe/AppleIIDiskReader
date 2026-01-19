using System.Buffers.Binary;
using System.Diagnostics;
using Microsoft.VisualBasic;

namespace AppleIIDiskReader.Files;

/// <summary>
/// Represents an Integer BASIC file on an Apple II disk.
/// </summary>
public readonly struct IntegerBasicFile
{
    /// <summary>
    /// The minimum size of an Integer BASIC file in bytes (length).
    /// </summary>
    public const int MinSize = 2;

    /// <summary>
    /// The length of the Integer BASIC file in bytes.
    /// </summary>
    public ushort Length { get; }

    /// <summary>
    /// The data of the Integer BASIC file.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegerBasicFile"/> struct.
    /// </summary>
    /// <param name="data">A span containing the Integer BASIC file data.</param>
    /// <exception cref="ArgumentException">Thrown when data is less than the minimum size or does not match the specified length.</exception>
    public IntegerBasicFile(ReadOnlySpan<byte> data)
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

    /// <summary>
    /// Parses the Integer BASIC file data into individual lines.
    /// </summary>
    /// <returns>A list of <see cref="IntegerBasicLine"/> objects representing the lines of the Integer BASIC file.</returns>
    public List<IntegerBasicLine> GetLines()
    {
        Span<byte> data = Data;
        // Each line starts with a 1-byte length and a 2-byte line number.
        var lines = new List<IntegerBasicLine>();
        int offset = 0;
        while (offset < data.Length)
        {
            // A zero-length byte indicates end of file.
            var length = data[offset];
            if (length == 0)
            {
                break;
            }

            var line = new IntegerBasicLine(data.Slice(offset, length));
            lines.Add(line);
            offset += line.Length;
        }

        return lines;
    }

    /// <summary>
    /// Returns a string representation of the Integer BASIC file.
    /// </summary>
    /// <returns>>A string representing the Integer BASIC file.</returns>
    public override string ToString()
    {
        var lines = GetLines();
        return string.Join(Environment.NewLine, lines.Select(line => line.ToString()));
    }
}