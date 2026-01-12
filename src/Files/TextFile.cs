using AppleIIDiskReader.Utilities;

namespace AppleIIDiskReader.Files;

/// <summary>
/// Represents a text file on an Apple II disk.
/// </summary>
public readonly struct TextFile
{
    /// <summary>
    /// The text content of the file.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextFile"/> struct.
    /// </summary>
    /// <param name="data">The byte array containing the text file data.</param>
    public TextFile(byte[] data)
    {
        Value = AppleIIEncoding.GetString(data);
    }
}
