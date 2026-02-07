using System.Diagnostics;

namespace AppleIIDiskReader.Files;

/// <summary>
/// An external symbol directory (ESD) entry in a relocatable binary file.
/// Contains EXTRN and ENTRY symbol definitions.
/// </summary>
public readonly struct ExternalSymbolDirectoryEntry
{
    /// <summary>
    /// The minimum size of an ESD entry in bytes (1 byte name + 1 byte flags + 2 bytes value).
    /// </summary>
    public const int MinSize = 4;

    /// <summary>
    /// The symbolic name of the external/entry symbol.
    /// </summary>
    public string SymbolName { get; }

    /// <summary>
    /// The flags for this ESD entry, indicating whether it's an EXTRN or ENTRY symbol.
    /// </summary>
    public ExternalSymbolDirectoryEntryFlags Flags { get; }

    /// <summary>
    /// For EXTRN symbols: the symbol number referenced by RLD entries with the EXT bit set.
    /// For ENTRY symbols: the low byte of the offset.
    /// </summary>
    public byte SymbolNumberOrOffsetLow { get; }

    /// <summary>
    /// For ENTRY symbols: the high byte of the offset.
    /// For EXTRN symbols: this value is not meaningful.
    /// </summary>
    public byte OffsetHigh { get; }

    /// <summary>
    /// For ENTRY symbols, gets the full 16-bit offset of the symbol.
    /// </summary>
    public ushort EntryOffset => (ushort)(SymbolNumberOrOffsetLow | (OffsetHigh << 8));

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalSymbolDirectoryEntry"/> struct.
    /// </summary>
    /// <param name="data">A span containing the ESD entry data.</param>
    /// <param name="bytesRead">The number of bytes consumed from the data span.</param>
    /// <exception cref="ArgumentException">Thrown when data is too short or invalid.</exception>
    public ExternalSymbolDirectoryEntry(ReadOnlySpan<byte> data, out int bytesRead)
    {
        if (data.Length < MinSize)
        {
            throw new ArgumentException($"Data must be at least {MinSize} bytes long.", nameof(data));
        }

        int offset = 0;

        // Read the symbol name. All bytes have their $80 bit set except the last one.
        // Symbol names are short (typically 1-8 chars), so stackalloc is ideal.
        var nameBufferLength = data.Length - offset;
        Span<char> nameBuffer = nameBufferLength <= 512
            ? stackalloc char[nameBufferLength]
            : new char[nameBufferLength];
        int nameLength = 0;
        while (offset < data.Length)
        {
            byte b = data[offset];
            offset++;

            // Extract the 7-bit ASCII character
            nameBuffer[nameLength++] = (char)(b & 0x7F);

            // If the high bit is clear, this is the last character of the name
            if ((b & 0x80) == 0)
            {
                break;
            }
        }

        SymbolName = new string(nameBuffer[..nameLength]);

        if (offset + 3 > data.Length)
        {
            throw new ArgumentException("Insufficient data for ESD entry after symbol name.", nameof(data));
        }

        // s1+1: Symbol type flag byte
        Flags = (ExternalSymbolDirectoryEntryFlags)data[offset];
        offset++;

        // s1+2: Symbol number (for EXTRN) or low byte of offset (for ENTRY)
        SymbolNumberOrOffsetLow = data[offset];
        offset++;

        // s1+3: High byte of offset for ENTRY type symbol
        OffsetHigh = data[offset];
        offset++;

        bytesRead = offset;
        Debug.Assert(offset <= data.Length, "Did not consume all bytes.");
    }

    /// <summary>
    /// Returns a string representation of the ESD entry.
    /// </summary>
    public override string ToString()
    {
        if (Flags.HasFlag(ExternalSymbolDirectoryEntryFlags.Entry))
        {
            return $"ENTRY {SymbolName} = ${EntryOffset:X4}";
        }
        else if (Flags.HasFlag(ExternalSymbolDirectoryEntryFlags.External))
        {
            return $"EXTRN {SymbolName} (#{SymbolNumberOrOffsetLow})";
        }
        else
        {
            return $"{SymbolName} (flags={Flags:X2})";
        }
    }
}
