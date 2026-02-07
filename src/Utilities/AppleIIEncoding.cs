namespace AppleIIDiskReader.Utilities;

/// <summary>
/// Provides utilities for encoding and decoding Apple II text.
/// Apple II uses "high ASCII" where bit 7 is set on printable characters.
/// </summary>
public static class AppleIIEncoding
{
    /// <summary>
    /// Converts a single Apple II high ASCII byte to a standard ASCII character.
    /// </summary>
    /// <param name="b">The Apple II byte.</param>
    /// <returns>The standard ASCII character.</returns>
    public static char ToChar(byte b) => (char)(b & 0x7F);

    /// <summary>
    /// Converts Apple II high ASCII bytes to a standard ASCII string.
    /// Stops at null terminator or end of data.
    /// </summary>
    /// <param name="data">The Apple II encoded data.</param>
    /// <returns>The decoded string.</returns>
    public static string GetString(ReadOnlySpan<byte> data)
    {
        // Worst case: every byte is '\r' which becomes Environment.NewLine (could be 2 chars on Windows).
        var maxLength = data.Length * Environment.NewLine.Length;
        Span<char> buffer = maxLength <= 256
            ? stackalloc char[maxLength]
            : new char[maxLength];

        var written = WriteString(data, buffer);
        return new string(buffer[..written]);
    }

    /// <summary>
    /// Converts Apple II high ASCII bytes into a span of characters.
    /// Stops at null terminator or end of data.
    /// </summary>
    /// <param name="data">The Apple II encoded data.</param>
    /// <param name="destination">The destination span to write decoded characters into.</param>
    /// <returns>The number of characters written to <paramref name="destination"/>.</returns>
    public static int WriteString(ReadOnlySpan<byte> data, Span<char> destination)
    {
        var pos = 0;

        foreach (byte b in data)
        {
            // Stop at null terminator
            if (b == 0x00)
            {
                break;
            }

            // Clear high bit to convert from Apple II high ASCII to standard ASCII
            var c = ToChar(b);

            // Convert carriage return (0x0D) to newline for modern systems
            if (c == '\r')
            {
                Environment.NewLine.AsSpan().CopyTo(destination[pos..]);
                pos += Environment.NewLine.Length;
            }
            else if ((c >= 0x20 && c < 0x7F) || c == '\t') // Printable ASCII or tab
            {
                destination[pos++] = c;
            }
            // Skip other control characters
        }

        return pos;
    }

    /// <summary>
    /// Decodes an Apple II file name from high ASCII bytes.
    /// File names are space-padded and may have trailing data for deleted files.
    /// </summary>
    /// <param name="fileNameBytes">The raw file name bytes (up to 30 bytes).</param>
    /// <param name="isDeleted">Whether the file is deleted (last byte contains original track).</param>
    /// <returns>The decoded file name.</returns>
    public static string DecodeFileName(ReadOnlySpan<byte> fileNameBytes, bool isDeleted = false)
    {
        // For deleted files, the last byte contains the original track number
        var length = isDeleted ? Math.Min(fileNameBytes.Length, 29) : fileNameBytes.Length;
        Span<char> buffer = stackalloc char[length];
        int pos = 0;

        for (int i = 0; i < length; i++)
        {
            // Clear high bit and convert to standard ASCII
            char c = ToChar(fileNameBytes[i]);

            // Stop at first space padding (Apple II pads with spaces)
            if (c == ' ' && i > 0)
            {
                // Check if remaining characters are all spaces
                var allSpaces = true;
                for (int j = i; j < length; j++)
                {
                    if (ToChar(fileNameBytes[j]) != ' ')
                    {
                        allSpaces = false;
                        break;
                    }
                }

                if (allSpaces)
                {
                    break;
                }
            }

            buffer[pos++] = c;
        }

        return new string(buffer[..pos].TrimEnd(' '));
    }
}
