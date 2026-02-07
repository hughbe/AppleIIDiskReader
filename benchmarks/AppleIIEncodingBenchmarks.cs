using System.Text;
using BenchmarkDotNet.Attributes;
using AppleIIDiskReader.Utilities;

namespace AppleIIDiskReader.Benchmarks;

/// <summary>
/// Benchmarks comparing StringBuilder-based vs Span-based Apple II encoding.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class AppleIIEncodingBenchmarks
{
    private byte[] _shortText = null!;
    private byte[] _longText = null!;
    private byte[] _fileName = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Short text: "HELLO WORLD" in Apple II high ASCII
        _shortText = "HELLO WORLD"u8.ToArray();
        for (int i = 0; i < _shortText.Length; i++)
            _shortText[i] |= 0x80;

        // Long text: ~512 bytes of Apple II high ASCII with carriage returns
        var longBuilder = new List<byte>();
        for (int line = 0; line < 20; line++)
        {
            foreach (byte b in "THIS IS A LINE OF APPLE II TEXT FOR BENCHMARKING."u8)
                longBuilder.Add((byte)(b | 0x80));
            longBuilder.Add(0x8D); // Apple II carriage return (0x0D | 0x80)
        }
        _longText = longBuilder.ToArray();

        // File name: "TESTFILE" padded with spaces to 30 bytes in high ASCII
        _fileName = new byte[30];
        var name = "TESTFILE"u8;
        for (int i = 0; i < 30; i++)
            _fileName[i] = i < name.Length ? (byte)(name[i] | 0x80) : (byte)(' ' | 0x80);
    }

    // --- GetString benchmarks ---

    [Benchmark(Description = "GetString_Old (short)")]
    public string GetString_Old_Short() => GetStringOld(_shortText);

    [Benchmark(Description = "GetString_Span (short)")]
    public string GetString_Span_Short() => AppleIIEncoding.GetString(_shortText);

    [Benchmark(Description = "GetString_Old (long)")]
    public string GetString_Old_Long() => GetStringOld(_longText);

    [Benchmark(Description = "GetString_Span (long)")]
    public string GetString_Span_Long() => AppleIIEncoding.GetString(_longText);

    // --- DecodeFileName benchmarks ---

    [Benchmark(Description = "DecodeFileName_Old")]
    public string DecodeFileName_Old() => DecodeFileNameOld(_fileName);

    [Benchmark(Description = "DecodeFileName_Span")]
    public string DecodeFileName_Span() => AppleIIEncoding.DecodeFileName(_fileName);

    // --- WriteString (zero-alloc) benchmark ---

    private readonly char[] _writeBuffer = new char[1024];

    [Benchmark(Description = "WriteString_Span (long, no alloc)")]
    public int WriteString_Span_Long() => AppleIIEncoding.WriteString(_longText, _writeBuffer);

    // --- Old implementations using StringBuilder for comparison ---

    private static string GetStringOld(ReadOnlySpan<byte> data)
    {
        var sb = new StringBuilder(data.Length);

        foreach (byte b in data)
        {
            if (b == 0x00)
                break;

            char c = (char)(b & 0x7F);

            if (c == '\r')
                sb.AppendLine();
            else if (c >= 0x20 && c < 0x7F)
                sb.Append(c);
            else if (c == '\t')
                sb.Append(c);
        }

        return sb.ToString();
    }

    private static string DecodeFileNameOld(ReadOnlySpan<byte> fileNameBytes, bool isDeleted = false)
    {
        int length = isDeleted ? Math.Min(fileNameBytes.Length, 29) : fileNameBytes.Length;
        var sb = new StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            char c = (char)(fileNameBytes[i] & 0x7F);

            if (c == ' ' && i > 0)
            {
                bool allSpaces = true;
                for (int j = i; j < length; j++)
                {
                    if ((char)(fileNameBytes[j] & 0x7F) != ' ')
                    {
                        allSpaces = false;
                        break;
                    }
                }

                if (allSpaces)
                    break;
            }

            sb.Append(c);
        }

        return sb.ToString().TrimEnd();
    }
}
