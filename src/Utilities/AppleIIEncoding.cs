using System.Text;

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
        var sb = new StringBuilder(data.Length);

        foreach (byte b in data)
        {
            // Stop at null terminator
            if (b == 0x00)
            {
                break;
            }

            // Clear high bit to convert from Apple II high ASCII to standard ASCII
            char c = ToChar(b);

            // Convert carriage return (0x0D) to newline for modern systems
            if (c == '\r')
            {
                sb.AppendLine();
            }
            else if (c >= 0x20 && c < 0x7F) // Printable ASCII
            {
                sb.Append(c);
            }
            else if (c == '\t') // Tab
            {
                sb.Append(c);
            }
            // Skip other control characters
        }

        return sb.ToString();
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
        int length = isDeleted ? Math.Min(fileNameBytes.Length, 29) : fileNameBytes.Length;
        var sb = new StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            // Clear high bit and convert to standard ASCII
            char c = ToChar(fileNameBytes[i]);

            // Stop at first space padding (Apple II pads with spaces)
            if (c == ' ' && i > 0)
            {
                // Check if remaining characters are all spaces
                bool allSpaces = true;
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

            sb.Append(c);
        }

        return sb.ToString().TrimEnd();
    }
}
