namespace AppleIIDiskReader.Files;

/// <summary>
/// Flags for an external symbol directory (ESD) entry.
/// </summary>
[Flags]
public enum ExternalSymbolDirectoryEntryFlags : byte
{
    /// <summary>
    /// No flags set.
    /// </summary>
    None = 0x00,

    /// <summary>
    /// Symbol is an ENTRY point (exported symbol).
    /// When set, the symbol is defined in this module and can be referenced by other modules.
    /// </summary>
    Entry = 0x08,

    /// <summary>
    /// Symbol is an EXTRN reference (imported symbol).
    /// When set, the symbol is defined in another module and referenced by this module.
    /// </summary>
    External = 0x10,
}
