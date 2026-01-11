namespace AppleIIDiskReader;

/// <summary>
/// Represents the file types supported by Apple II DOS 3.3.
/// </summary>
public enum AppleIIFileType : byte
{
    /// <summary>
    /// Text file ($00).
    /// </summary>
    Text = 0x00,

    /// <summary>
    /// Integer BASIC file ($01).
    /// </summary>
    IntegerBasic = 0x01,

    /// <summary>
    /// Applesoft BASIC file ($02).
    /// </summary>
    ApplesoftBasic = 0x02,

    /// <summary>
    /// Binary file ($04).
    /// </summary>
    Binary = 0x04,

    /// <summary>
    /// S type file ($08).
    /// </summary>
    SType = 0x08,

    /// <summary>
    /// Relocatable object module file ($10).
    /// </summary>
    Relocatable = 0x10,

    /// <summary>
    /// A type file ($20).
    /// </summary>
    AType = 0x20,

    /// <summary>
    /// B type file ($40).
    /// </summary>
    BType = 0x40
}
