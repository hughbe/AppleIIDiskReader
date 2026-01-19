using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AppleIIDiskReader;

/// <summary>
/// An inline array of 5 bytes.
/// </summary>
[InlineArray(5)]
public struct ByteArray5
{
    /// <summary>
    /// The size of the array in bytes.
    /// </summary>
    public const int Size = 5;

    private byte _element0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteArray5"/> struct.
    /// </summary>
    public ByteArray5(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be {Size} bytes in length.", nameof(data));
        }

        data.CopyTo(AsSpan());
    }

    /// <summary>
    /// Gets a span over the elements of the array.
    /// </summary>   
    public Span<byte> AsSpan() =>
        MemoryMarshal.CreateSpan(ref _element0, 5);
}

/// <summary>
/// An inline array of 8 bytes.
/// </summary>
[InlineArray(8)]
public struct ByteArray8
{
    /// <summary>
    /// The size of the array in bytes.
    /// </summary>
    public const int Size = 8;

    private byte _element0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteArray8"/> struct.
    /// </summary>
    public ByteArray8(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be exactly {Size} bytes long.", nameof(data));
        }
        
        data.CopyTo(AsSpan());
    }

    /// <summary>
    /// Gets a span over the elements of the array.
    /// </summary>   
    public Span<byte> AsSpan() =>
        MemoryMarshal.CreateSpan(ref _element0, Size);
}

/// <summary>
/// An inline array of 30 bytes.
/// </summary>
[InlineArray(30)]
public struct ByteArray30
{
    /// <summary>
    /// The size of the array in bytes.
    /// </summary>
    public const int Size = 30;

    private byte _element0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteArray30"/> struct.
    /// </summary>
    public ByteArray30(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be exactly {Size} bytes long.", nameof(data));
        }

        data.CopyTo(AsSpan());
    }

    /// <summary>
    /// Gets a span over the elements of the array.
    /// </summary>   
    public Span<byte> AsSpan() =>
        MemoryMarshal.CreateSpan(ref _element0, Size);
}

/// <summary>
/// An inline array of 32 bytes.
/// </summary>
[InlineArray(32)]
public struct ByteArray32
{
    /// <summary>
    /// The size of the array in bytes.
    /// </summary>
    public const int Size = 32;

    private byte _element0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteArray32"/> struct.
    /// </summary>
    public ByteArray32(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be {Size} bytes in length.", nameof(data));
        }

        data.CopyTo(AsSpan());
    }

    /// <summary>
    /// Gets a span over the elements of the array.
    /// </summary>   
    public Span<byte> AsSpan() =>
        MemoryMarshal.CreateSpan(ref _element0, Size);
}
