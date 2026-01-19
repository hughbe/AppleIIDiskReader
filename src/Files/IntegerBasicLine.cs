using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

namespace AppleIIDiskReader.Files;

/// <summary>
/// Represents a line in an Integer BASIC file.
/// </summary>
public readonly struct IntegerBasicLine
{
    private const byte EndOfLineToken = 0x01;
    private const byte RemToken = 0x5D;
    private const byte UnaryPlus = 0x35;
    private const byte UnaryMinus = 0x36;
    private const byte QuoteStart = 0x28;
    private const byte QuoteEnd = 0x29;

    private static readonly string[] s_tokens =
    [
        /* $00-$0F */
        "HIMEM:", "<$01>", "_", " : ",
        "LOAD", "SAVE", "CON", "RUN",    /* Direct commands */
        "RUN", "DEL", ",", "NEW",
        "CLR", "AUTO", ",", "MAN",

        /* $10-$1F */
        "HIMEM:", "LOMEM:", "+", "-",    /* Binary ops */
        "*", "/", "=", "#",
        ">=", ">", "<=", "<>",
        "<", "AND", "OR", "MOD",

        /* $20-$2F */
        "^", "+", "(", ",",
        "THEN", "THEN", ",", ",",
        "\"", "\"", "(", "!",
        "!", "(", "PEEK", "RND",

        /* $30-$3F */
        "SGN", "ABS", "PDL", "RNDX",
        "(", "+", "-", "NOT",            /* Unary ops */
        "(", "=", "#", "LEN(",
        "ASC(", "SCRN(", ",", "(",

        /* $40-$4F */
        "$", "$", "(", ",",
        ",", ";", ";", ";",
        ",", ",", ",", "TEXT",           /* Statements */
        "GR", "CALL", "DIM", "DIM",

        /* $50-$5F */
        "TAB", "END", "INPUT", "INPUT",
        "INPUT", "FOR", "=", "TO",
        "STEP", "NEXT", ",", "RETURN",
        "GOSUB", "REM", "LET", "GOTO",

        /* $60-$6F */
        "IF", "PRINT", "PRINT", "PRINT",
        "POKE", ",", "COLOR=", "PLOT",
        ",", "HLIN", ",", "AT",
        "VLIN", ",", "AT", "VTAB",

        /* $70-$7F */
        "=", "=", ")", ")",
        "LIST", ",", "LIST", "POP",
        "NODSP", "DSP", "NOTRACE", "DSP",
        "DSP", "TRACE", "PR#", "IN#",
    ];

    /// <summary>
    /// The minimum size of an Integer BASIC line in bytes (length + line number).
    /// </summary>
    public const int MinSize = 3;

    /// <summary>
    /// The length of the Integer BASIC line in bytes.
    /// </summary>
    public byte Length { get; }

    /// <summary>
    /// The line number of the Integer BASIC line.
    /// </summary>
    public ushort LineNumber { get; }

    /// <summary>
    /// The content of the Integer BASIC line.
    /// </summary>
    public byte[] Content { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegerBasicLine"/> struct.
    /// </summary>
    /// <param name="data">A span containing the Integer BASIC line data.</param>
    /// <exception cref="ArgumentException">>Thrown when data is less than 3 bytes or does not match the specified line length.</exception>
    public IntegerBasicLine(ReadOnlySpan<byte> data)
    {
        if (data.Length < MinSize)
        {
            throw new ArgumentException("Data must be at least 3 bytes long.", nameof(data));
        }

        int offset = 0;

        Length = data[offset];
        offset += 1;

        if (Length < 3)
        {
            throw new ArgumentException("Line length must be at least 3 bytes (length + line number).", nameof(data));
        }
        if (Length > data.Length)
        {
            throw new ArgumentException("Data length does not match the specified line length.", nameof(data));
        }

        LineNumber = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        Content = data.Slice(offset, Length - 3).ToArray();
        offset += Content.Length;

        Debug.Assert(offset <= data.Length, "Did not consume all bytes.");
    }

    /// <summary>
    /// Returns a string representation of the Integer BASIC line.
    /// </summary>
    /// <returns>A string representing the Integer BASIC line.</returns>
    public override string ToString()
    {
        Span<byte> content = Content;

        var sb = new StringBuilder();
        sb.Append(LineNumber);
        sb.Append(' ');

        bool inRem = false;
        bool inQuote = false;
        bool lastAlphanumeric = false;
        bool leadingSpace = false;
        bool lastToken = false;

        int offset = 0;
        while (offset < content.Length && content[offset] != EndOfLineToken)
        {
            byte b = content[offset];
            offset++;

            leadingSpace = leadingSpace || lastAlphanumeric;

            if ((b & 0x80) != 0)
            {
                // High bit set - ASCII character or integer constant
                if (!inRem && !inQuote && !lastAlphanumeric && b >= 0xB0 && b <= 0xB9)
                {
                    // Integer constant: 3 bytes - $B0-$B9 followed by 16-bit little-endian value
                    if (offset + 2 > content.Length)
                    {
                        throw new ArgumentException("Insufficient data for integer constant.", nameof(Content));
                    }

                    short integer = BinaryPrimitives.ReadInt16LittleEndian(content.Slice(offset, 2));
                    bool needSpace = lastToken && leadingSpace;
                    if (needSpace)
                    {
                        sb.Append(' ');
                    }

                    sb.Append(integer);
                    offset += 2;
                    leadingSpace = true;
                    lastToken = false;
                    continue;
                }
                else
                {
                    // ASCII character with high bit set
                    char c = (char)(b & 0x7F);
                    bool needSpace = !inRem && !inQuote && lastToken && leadingSpace && char.IsLetterOrDigit(c);
                    if (needSpace)
                    {
                        sb.Append(' ');
                    }

                    if (c >= 0x20)
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        // Control character - display as ^X
                        sb.Append('^');
                        sb.Append((char)(c + 0x40));
                    }

                    lastAlphanumeric = char.IsLetterOrDigit(c);
                }

                lastToken = false;
            }
            else
            {
                // Token ($00-$7F)
                string token = s_tokens[b];
                char lastChar = token[^1];
                bool needSpace = leadingSpace &&
                    (char.IsLetterOrDigit(token[0]) || b == UnaryPlus || b == UnaryMinus || b == QuoteStart);

                switch (b)
                {
                    case RemToken:
                        inRem = true;
                        break;
                    case QuoteStart:
                        inQuote = true;
                        break;
                    case QuoteEnd:
                        inQuote = false;
                        break;
                }

                if (needSpace)
                {
                    sb.Append(' ');
                }

                sb.Append(token);

                lastAlphanumeric = false;
                leadingSpace = char.IsLetterOrDigit(lastChar) || lastChar == ')' || lastChar == '"';
                lastToken = true;
            }
        }

        return sb.ToString();
    }
}