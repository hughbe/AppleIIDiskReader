using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AppleIIDiskReader;

/// <summary>
/// An inline array of 7 File Descriptive Entries.
/// </summary>
[InlineArray(Length)]
public struct FileDescriptiveEntriesArray
{
    /// <summary>
    /// The number of entries in the array.
    /// </summary>
    public const int Length = 7;

    /// <summary>
    /// The first element of the array.
    /// </summary>
    private FileDescriptiveEntry _element0;

    /// <summary>
    /// Gets a span over the elements of the array.
    /// </summary>
    /// <returns>A span containing the elements of the array.</returns>
    public Span<FileDescriptiveEntry> AsSpan() =>
        MemoryMarshal.CreateSpan(ref _element0, Length);

    /// <summary>
    /// Gets a read-only span over the elements of the array.
    /// </summary>
    /// <returns>A read-only span containing the elements of the array.</returns>
    public ReadOnlySpan<FileDescriptiveEntry> AsReadOnlySpan() =>
        MemoryMarshal.CreateReadOnlySpan(ref _element0, Length);
}
