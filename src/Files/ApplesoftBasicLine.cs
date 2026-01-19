using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using Microsoft.VisualBasic;

namespace AppleIIDiskReader.Files;

/// <summary>
/// An Applesoft BASIC line.
/// </summary>
public readonly struct ApplesoftBasicLine
{
    /// <summary>
    /// The Applesoft BASIC tokens indexed by (byte - 0x80).
    /// </summary>
    private static readonly string[] s_tokens =
    [
        "END",      // $80 (128)
        "FOR",      // $81 (129)
        "NEXT",     // $82 (130)
        "DATA",     // $83 (131)
        "INPUT",    // $84 (132)
        "DEL",      // $85 (133)
        "DIM",      // $86 (134)
        "READ",     // $87 (135)
        "GR",       // $88 (136)
        "TEXT",     // $89 (137)
        "PR #",     // $8A (138)
        "IN #",     // $8B (139)
        "CALL",     // $8C (140)
        "PLOT",     // $8D (141)
        "HLIN",     // $8E (142)
        "VLIN",     // $8F (143)
        "HGR2",     // $90 (144)
        "HGR",      // $91 (145)
        "HCOLOR=",  // $92 (146)
        "HPLOT",    // $93 (147)
        "DRAW",     // $94 (148)
        "XDRAW",    // $95 (149)
        "HTAB",     // $96 (150)
        "HOME",     // $97 (151)
        "ROT=",     // $98 (152)
        "SCALE=",   // $99 (153)
        "SHLOAD",   // $9A (154)
        "TRACE",    // $9B (155)
        "NOTRACE",  // $9C (156)
        "NORMAL",   // $9D (157)
        "INVERSE",  // $9E (158)
        "FLASH",    // $9F (159)
        "COLOR=",   // $A0 (160)
        "POP",      // $A1 (161)
        "VTAB",     // $A2 (162)
        "HIMEM:",   // $A3 (163)
        "LOMEM:",   // $A4 (164)
        "ONERR",    // $A5 (165)
        "RESUME",   // $A6 (166)
        "RECALL",   // $A7 (167)
        "STORE",    // $A8 (168)
        "SPEED=",   // $A9 (169)
        "LET",      // $AA (170)
        "GOTO",     // $AB (171)
        "RUN",      // $AC (172)
        "IF",       // $AD (173)
        "RESTORE",  // $AE (174)
        "&",        // $AF (175)
        "GOSUB",    // $B0 (176)
        "RETURN",   // $B1 (177)
        "REM",      // $B2 (178)
        "STOP",     // $B3 (179)
        "ON",       // $B4 (180)
        "WAIT",     // $B5 (181)
        "LOAD",     // $B6 (182)
        "SAVE",     // $B7 (183)
        "DEF FN",   // $B8 (184)
        "POKE",     // $B9 (185)
        "PRINT",    // $BA (186)
        "CONT",     // $BB (187)
        "LIST",     // $BC (188)
        "CLEAR",    // $BD (189)
        "GET",      // $BE (190)
        "NEW",      // $BF (191)
        "TAB",      // $C0 (192)
        "TO",       // $C1 (193)
        "FN",       // $C2 (194)
        "SPC(",     // $C3 (195)
        "THEN",     // $C4 (196)
        "AT",       // $C5 (197)
        "NOT",      // $C6 (198)
        "STEP",     // $C7 (199)
        "+",        // $C8 (200)
        "-",        // $C9 (201)
        "*",        // $CA (202)
        "/",        // $CB (203)
        ";",        // $CC (204)
        "AND",      // $CD (205)
        "OR",       // $CE (206)
        ">",        // $CF (207)
        "=",        // $D0 (208)
        "<",        // $D1 (209)
        "SGN",      // $D2 (210)
        "INT",      // $D3 (211)
        "ABS",      // $D4 (212)
        "USR",      // $D5 (213)
        "FRE",      // $D6 (214)
        "SCRN (",   // $D7 (215)
        "PDL",      // $D8 (216)
        "POS",      // $D9 (217)
        "SQR",      // $DA (218)
        "RND",      // $DB (219)
        "LOG",      // $DC (220)
        "EXP",      // $DD (221)
        "COS",      // $DE (222)
        "SIN",      // $DF (223)
        "TAN",      // $E0 (224)
        "ATN",      // $E1 (225)
        "PEEK",     // $E2 (226)
        "LEN",      // $E3 (227)
        "STR$",     // $E4 (228)
        "VAL",      // $E5 (229)
        "ASC",      // $E6 (230)
        "CHR$",     // $E7 (231)
        "LEFT$",    // $E8 (232)
        "RIGHT$",   // $E9 (233)
        "MID$",     // $EA (234)
        "",         // $EB (235) - unused
        "",         // $EC (236) - unused
        "",         // $ED (237) - unused
        "",         // $EE (238) - unused
        "",         // $EF (239) - unused
        "",         // $F0 (240) - unused
        "",         // $F1 (241) - unused
        "",         // $F2 (242) - unused
        "",         // $F3 (243) - unused
        "",         // $F4 (244) - unused
        "",         // $F5 (245) - unused
        "",         // $F6 (246) - unused
        "",         // $F7 (247) - unused
        "",         // $F8 (248) - unused
        "",         // $F9 (249) - unused
        "",         // $FA (250) - unused
        "",         // $FB (251) - unused
        "",         // $FC (252) - unused
        "",         // $FD (253) - unused
        "",         // $FE (254) - unused
        "",         // $FF (255) - unused
    ];

    /// <summary>
    /// The minimum size of an Applesoft BASIC line in bytes (address of next line + line number).
    /// </summary>
    public const int MinSize = 4;

    /// <summary>
    /// The address of the next line in memory.
    /// </summary>
    public ushort AddressOfNextLine { get; }

    /// <summary>
    /// The line number of the Applesoft BASIC line.
    /// </summary>
    public ushort LineNumber { get; }

    /// <summary>
    /// The content of the Applesoft BASIC line.
    /// </summary>
    public byte[] Content { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplesoftBasicLine"/> struct.
    /// </summary>
    /// <param name="data">A span containing the Applesoft BASIC line data.</param>
    /// <param name="bytesRead">The number of bytes read from the data span.</param>
    /// <exception cref="ArgumentException">>Thrown when data is less than 4 bytes.</exception>
    public ApplesoftBasicLine(ReadOnlySpan<byte> data, out int bytesRead)
    {
        if (data.Length < MinSize)
        {
            throw new ArgumentException("Data must be at least 4 bytes long.", nameof(data));
        }

        int offset = 0;

        AddressOfNextLine = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        LineNumber = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        int endOfLineIndex = data[offset..].IndexOf((byte)0x00);
        if (endOfLineIndex == -1)
        {
            // If no end of line marker found, assume the rest of the data is the line content.
            endOfLineIndex = data.Length - offset - 1;
        }

        // Read the content includiung the end of line marker.
        Content = data.Slice(offset, endOfLineIndex + 1).ToArray();
        offset += Content.Length;

        bytesRead = offset;
        Debug.Assert(offset <= data.Length, "Did not consume all bytes.");
    }

    /// <summary>
    /// Returns a string representation of the Applesoft BASIC line.
    /// </summary>
    /// <returns>>A string representing the Applesoft BASIC line.</returns>
    public override string ToString()
    {
        const byte RemToken = 0xB2;

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
        while (offset < content.Length && content[offset] != 0x00)
        {
            byte b = content[offset];
            offset++;

            leadingSpace = leadingSpace || lastAlphanumeric;

            // Handle tokens and characters.
            // Inside REM or quoted strings, bytes >= 0x80 are treated as ASCII, not tokens.
            if (b >= 0x80 && !inRem && !inQuote)
            {
                // Token ($80-$FF)
                string token = s_tokens[b - 0x80];
                if (token.Length > 0)
                {
                    char lastChar = token[^1];
                    bool needSpace = leadingSpace && char.IsLetterOrDigit(token[0]);

                    if (needSpace)
                    {
                        sb.Append(' ');
                    }

                    sb.Append(token);

                    if (b == RemToken)
                    {
                        inRem = true;
                    }

                    lastAlphanumeric = false;
                    leadingSpace = char.IsLetterOrDigit(lastChar) || lastChar == ')' || lastChar == '"';
                    lastToken = true;
                }
            }
            else
            {
                // ASCII character ($00-$7F, or $80-$FF inside REM/quote)
                char c = (char)(b & 0x7F);

                bool needSpace = !inRem && !inQuote && lastToken && leadingSpace && (char.IsLetterOrDigit(c) || c == '"');
                if (needSpace)
                {
                    sb.Append(' ');
                }

                if (c == '"')
                {
                    inQuote = !inQuote;
                }

                sb.Append(c);
                lastAlphanumeric = char.IsLetterOrDigit(c);
                lastToken = false;
            }
        }

        return sb.ToString();
    }
}