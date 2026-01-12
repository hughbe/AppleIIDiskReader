using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AppleIIDiskReader;

/// <summary>
/// An inline array of 7 File Descriptive Entries.
/// </summary>
[InlineArray(7)]
public struct FileDescriptiveEntriesArray
{
    /// <summary>
    /// The first element of the array.
    /// </summary>
    private FileDescriptiveEntry _element0;

    /// <summary>
    /// Gets a span over the elements of the array.
    /// </summary>   
    public Span<FileDescriptiveEntry> AsSpan() =>
        MemoryMarshal.CreateSpan(ref _element0, 7); 
}
