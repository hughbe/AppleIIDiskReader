using System.Buffers.Binary;
using System.Diagnostics;

namespace AppleIIDiskReader.Files;

/// <summary>
/// A relocation dictionary entry in a relocatable binary file.
/// </summary>
public readonly struct RelocationDictionaryEntry
{
    /// <summary>
    /// The size of a relocation dictionary entry in bytes.
    /// </summary>
    public const int Size = 4;

    /// <summary>
    /// The flags for the relocation dictionary entry.
    /// </summary>
    public RelocationDictionaryEntryFlags Flags { get; }

    /// <summary>
    /// The offset of the relocatable field within the code image.
    /// </summary>
    public ushort FieldOffset { get; }

    /// <summary>
    /// For an 8-bit field containing the upper 8 bits of a 16-bit value
    /// (<see cref="RelocationDictionaryEntryFlags.HighByte"/> is set), this contains
    /// the low 8 bits of the 16-bit value. Zero if <see cref="RelocationDictionaryEntryFlags.HighByte"/> is clear.
    /// If <see cref="RelocationDictionaryEntryFlags.External"/> is set, this contains the ESD symbol number.
    /// </summary>
    public byte Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RelocationDictionaryEntry"/> struct.
    /// </summary>
    /// <param name="data">A span containing the relocation dictionary entry data.</param>
    /// <exception cref="ArgumentException">>Thrown when data is not exactly 4 bytes.</exception>
    public RelocationDictionaryEntry(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be exactly {Size} bytes long.", nameof(data));
        }

        // Structure documented in https://archive.org/details/apple-6502-assembler-editor/page/n63/mode/2up
        int offset = 0;

        // 1 RLD flags bytes containing 4 flag bits as follows
        // $80 bit Size of relocatable field
        // Clear => 1 byte, SET => 2 byte
        // $40 bit Upper/Lower 8 of a 16 bit value
        // Clear => low 8, Set -> high 8
        // $20 bit Normal/reversed 2 byte field Clr -> low-hi, Set -> hi-low
        // (the DDB pseudo causes Set)
        // $10 Field is EXTRN 16 bit reference Clr => not ext, Set => is EXTRN
        // $01 'NOT END OF RLD' flag bit ALWAYS SET ON for RLD entry Clear
        // marks end of RLD
        Flags = (RelocationDictionaryEntryFlags)data[offset];
        offset += 1;

        // 2 Field offset in code, low byte
        // 3 Field offset in code, high byte
        FieldOffset = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        // Low 8 bits of 16 bit value for an 8 bit field containing upper 8 bits.
        // Zero if $40 bit clear in RLD byte one# Or if the $10 bit is set, then
        // this is the ESD symbol number.
        Value = data[offset];
        offset += 1;

        Debug.Assert(offset == data.Length, "Did not consume all bytes for RelocationDictionaryEntry.");
    }
}
