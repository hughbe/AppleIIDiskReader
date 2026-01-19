using System.Buffers.Binary;
using System.Diagnostics.Contracts;

namespace AppleIIDiskReader.Files;

/// <summary>
/// Represents a relocatable binary file on an Apple II disk.
/// </summary>
public readonly struct RelocatableBinaryFile
{
    /// <summary>
    /// The minimum size of a relocatable binary file in bytes (header).
    /// </summary>
    public const int MinSize = 6;

    /// <summary>
    /// The load address of the relocatable binary file.
    /// </summary>
    public ushort StartingRAMAddress { get; }

    /// <summary>
    /// The length of the RAM image of the relocatable binary file.
    /// </summary>
    public ushort RAMImageLength { get; }

    /// <summary>
    /// The length of the code image of the relocatable binary file.
    /// </summary>
    public ushort CodeImageLength { get; }

    /// <summary>
    /// The code image of the relocatable binary file.
    /// </summary>
    public byte[] CodeImage { get; }

    /// <summary>
    /// The relocation dictionary entries for the relocatable binary file.
    /// </summary>
    public RelocationDictionaryEntry[] RelocationDictionary { get; }

    /// <summary>
    /// The external symbol directory entries for the relocatable binary file.
    /// Contains EXTRN and ENTRY symbol definitions.
    /// </summary>
    public ExternalSymbolDirectoryEntry[] ExternalSymbolDirectory { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RelocatableBinaryFile"/> struct.
    /// </summary>
    /// <param name="data">A span containing the relocatable binary file data.</param>
    /// <exception cref="ArgumentException">Thrown when data is less than the minimum size or does not match the specified code image length.</exception>
    public RelocatableBinaryFile(ReadOnlySpan<byte> data)
    {
        if (data.Length < MinSize)
        {
            throw new ArgumentException($"Data must be at least {MinSize} bytes long.", nameof(data));
        }

        // Structure documented in https://archive.org/details/apple-6502-assembler-editor/page/n63/mode/2up
        int offset = 0;

        // 0 Starting RAM address, low byte
        // 1 Starting RAM address, high byte
        StartingRAMAddress = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        // 2 Length of RAM image, low byte
        // 3 Length of RAM image, high byte
        RAMImageLength = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        // 4 Length of code image, low byte
        // 5 Length of code image, high byte
        CodeImageLength = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        if (offset + CodeImageLength > data.Length)
        {
            throw new ArgumentException("Data length does not match the specified code image length.", nameof(data));
        }

        // 6 to cl+6 Binary code image, of length in bytes 4 and 5 above
        CodeImage = data.Slice(offset, CodeImageLength).ToArray();
        offset += CodeImageLength;

        // cl+7 Begin Relocation Dictionary, which consists of N 4 byte entries.
        // N is variable(0 to??)
        var dictionaryEntries = new List<RelocationDictionaryEntry>();
        while (offset < data.Length)
        {
            // N*4+1 Binary 00 marks end of RLD®
            if (data[offset] == 0x00)
            {
                // End of relocation dictionary.
                offset += 1;
                break;
            }

            var entry = new RelocationDictionaryEntry(data.Slice(offset, RelocationDictionaryEntry.Size));
            dictionaryEntries.Add(entry);
            offset += RelocationDictionaryEntry.Size;
        }

        RelocationDictionary = dictionaryEntries.ToArray();

        // N*4+2 Beginning of optional External Symbol Directory (ESD). This area
        // will only contain bytes if an EXTRN and/or ENTRY pseudo occurs in the
        // program.
        var esdEntries = new List<ExternalSymbolDirectoryEntry>();
        while (offset < data.Length)
        {
            // End mark Binary zero byte marks end of the ESD entries
            if (data[offset] == 0x00)
            {
                offset += 1;
                break;
            }

            var entry = new ExternalSymbolDirectoryEntry(data[offset..], out int bytesRead);
            esdEntries.Add(entry);
            offset += bytesRead;
        }

        ExternalSymbolDirectory = esdEntries.ToArray();

        Contract.Assert(offset <= data.Length, "Did not consume all bytes.");
    }
}
