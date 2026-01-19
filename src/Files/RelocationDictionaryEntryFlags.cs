namespace AppleIIDiskReader.Files;

/// <summary>
/// Flags for a relocation dictionary entry.
/// </summary>
[Flags]
public enum RelocationDictionaryEntryFlags : byte
{
    /// <summary>
    /// No flags set. This marks the end of the relocation dictionary.
    /// </summary>
    None = 0x00,

    /// <summary>
    /// 'NOT END OF RLD' flag bit. Always set for valid RLD entries.
    /// When clear, marks the end of the relocation dictionary.
    /// </summary>
    NotEndOfRld = 0x01,

    /// <summary>
    /// Field is an EXTRN 16-bit reference.
    /// When clear, the field is not external.
    /// When set, the field is an EXTRN reference and <see cref="RelocationDictionaryEntry.Value"/>
    /// contains the ESD symbol number.
    /// </summary>
    External = 0x10,

    /// <summary>
    /// Normal/reversed 2-byte field order.
    /// When clear, the field is stored low-high (little-endian).
    /// When set, the field is stored high-low (big-endian, caused by DDB pseudo-op).
    /// </summary>
    Reversed = 0x20,

    /// <summary>
    /// Upper/Lower 8 bits of a 16-bit value.
    /// When clear, the field contains the low 8 bits.
    /// When set, the field contains the high 8 bits.
    /// </summary>
    HighByte = 0x40,

    /// <summary>
    /// Size of the relocatable field.
    /// When clear, the field is 1 byte.
    /// When set, the field is 2 bytes.
    /// </summary>
    TwoByteField = 0x80,
}
