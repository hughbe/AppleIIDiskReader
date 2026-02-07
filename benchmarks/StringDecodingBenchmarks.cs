using System.Buffers.Binary;
using System.Text;
using BenchmarkDotNet.Attributes;
using AppleIIDiskReader;
using AppleIIDiskReader.Files;

namespace AppleIIDiskReader.Benchmarks;

/// <summary>
/// Benchmarks comparing StringBuilder-based vs Span-based string decoding
/// for BASIC line decompilation and ESD symbol name parsing.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class StringDecodingBenchmarks
{
    private ApplesoftBasicFile _applesoftFile;
    private ApplesoftBasicLine _applesoftLine;
    private byte[] _esdData = null!;

    private const string SampleDiskPath = "Samples/appleIIDos.dsk";

    [GlobalSetup]
    public void Setup()
    {
        var diskData = File.ReadAllBytes(SampleDiskPath);
        using var stream = new MemoryStream(diskData);
        var disk = new AppleIIDisk(stream);

        // Find the first Applesoft BASIC file on the disk
        foreach (var entry in disk.EnumerateFileEntries())
        {
            if (entry.FileType == AppleIIFileType.ApplesoftBasic)
            {
                _applesoftFile = disk.ReadApplesoftBasicFile(entry);
                _applesoftLine = _applesoftFile.GetLines()[0];
                break;
            }
        }

        // Construct realistic ESD data: "MAIN" with high bits set (except last char)
        // Format: each byte has $80 set except the last, followed by flags + 2 value bytes
        _esdData = [(byte)('M' | 0x80), (byte)('A' | 0x80), (byte)('I' | 0x80), (byte)'N', 0x10, 0x00, 0x08];
    }

    // --- Applesoft BASIC line decompilation ---

    [Benchmark(Description = "ApplesoftLine.ToString_Old")]
    public string ApplesoftLine_Old() => ApplesoftLineToStringOld(_applesoftLine);

    [Benchmark(Description = "ApplesoftLine.ToString_Span")]
    public string ApplesoftLine_Span() => _applesoftLine.ToString();

    // --- Applesoft BASIC file (all lines) ---

    [Benchmark(Description = "ApplesoftFile.ToString_Old")]
    public string ApplesoftFile_Old() => ApplesoftFileToStringOld(_applesoftFile);

    [Benchmark(Description = "ApplesoftFile.ToString_Span")]
    public string ApplesoftFile_Span() => _applesoftFile.ToString();

    // --- ESD symbol name parsing ---

    [Benchmark(Description = "ESD_SymbolName_Old")]
    public string ESD_Old() => ParseEsdSymbolNameOld(_esdData);

    [Benchmark(Description = "ESD_SymbolName_Span")]
    public string ESD_Span() => new ExternalSymbolDirectoryEntry(_esdData, out _).SymbolName;

    // --- Old implementations using StringBuilder for comparison ---

    private static readonly string[] s_applesoftTokens =
    [
        "END", "FOR", "NEXT", "DATA", "INPUT", "DEL", "DIM", "READ",
        "GR", "TEXT", "PR #", "IN #", "CALL", "PLOT", "HLIN", "VLIN",
        "HGR2", "HGR", "HCOLOR=", "HPLOT", "DRAW", "XDRAW", "HTAB", "HOME",
        "ROT=", "SCALE=", "SHLOAD", "TRACE", "NOTRACE", "NORMAL", "INVERSE", "FLASH",
        "COLOR=", "POP", "VTAB", "HIMEM:", "LOMEM:", "ONERR", "RESUME", "RECALL",
        "STORE", "SPEED=", "LET", "GOTO", "RUN", "IF", "RESTORE", "&",
        "GOSUB", "RETURN", "REM", "STOP", "ON", "WAIT", "LOAD", "SAVE",
        "DEF FN", "POKE", "PRINT", "CONT", "LIST", "CLEAR", "GET", "NEW",
        "TAB", "TO", "FN", "SPC(", "THEN", "AT", "NOT", "STEP",
        "+", "-", "*", "/", ";", "AND", "OR", ">",
        "=", "<", "SGN", "INT", "ABS", "USR", "FRE", "SCRN (",
        "PDL", "POS", "SQR", "RND", "LOG", "EXP", "COS", "SIN",
        "TAN", "ATN", "PEEK", "LEN", "STR$", "VAL", "ASC", "CHR$",
        "LEFT$", "RIGHT$", "MID$",
        "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "",
    ];

    private static string ApplesoftLineToStringOld(ApplesoftBasicLine line)
    {
        const byte RemToken = 0xB2;
        Span<byte> content = line.Content;
        var sb = new StringBuilder();
        sb.Append(line.LineNumber);
        sb.Append(' ');

        bool inRem = false, inQuote = false, lastAlphanumeric = false, leadingSpace = false, lastToken = false;

        int offset = 0;
        while (offset < content.Length && content[offset] != 0x00)
        {
            byte b = content[offset++];
            leadingSpace = leadingSpace || lastAlphanumeric;

            if (b >= 0x80 && !inRem && !inQuote)
            {
                string token = s_applesoftTokens[b - 0x80];
                if (token.Length > 0)
                {
                    char lastChar = token[^1];
                    if (leadingSpace && char.IsLetterOrDigit(token[0]))
                        sb.Append(' ');
                    sb.Append(token);
                    if (b == RemToken) inRem = true;
                    lastAlphanumeric = false;
                    leadingSpace = char.IsLetterOrDigit(lastChar) || lastChar == ')' || lastChar == '"';
                    lastToken = true;
                }
            }
            else
            {
                char c = (char)(b & 0x7F);
                if (!inRem && !inQuote && lastToken && leadingSpace && (char.IsLetterOrDigit(c) || c == '"'))
                    sb.Append(' ');
                if (c == '"') inQuote = !inQuote;
                sb.Append(c);
                lastAlphanumeric = char.IsLetterOrDigit(c);
                lastToken = false;
            }
        }

        return sb.ToString();
    }

    private static string ApplesoftFileToStringOld(ApplesoftBasicFile file)
    {
        var lines = file.GetLines();
        return string.Join(Environment.NewLine, lines.Select(line => ApplesoftLineToStringOld(line)));
    }

    private static string ParseEsdSymbolNameOld(ReadOnlySpan<byte> data)
    {
        var nameBuilder = new StringBuilder();
        int offset = 0;
        while (offset < data.Length)
        {
            byte b = data[offset++];
            nameBuilder.Append((char)(b & 0x7F));
            if ((b & 0x80) == 0) break;
        }
        return nameBuilder.ToString();
    }
}
