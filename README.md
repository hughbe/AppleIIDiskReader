# AppleIIDiskReader

A lightweight .NET library for reading Apple II DOS 3.3 floppy disk images (.dsk, .do). Supports reading the Volume Table of Contents (VTOC), catalog entries, and extracting files from 140KB 5.25" disk images.

## Features

- Read Apple II DOS 3.3 disk images (140KB, 35 tracks, 16 sectors)
- Parse Volume Table of Contents (VTOC)
- Enumerate catalog entries and file descriptive entries
- Support for all DOS 3.3 file types:
  - Text files (with high ASCII conversion)
  - Integer BASIC programs
  - Applesoft BASIC programs
  - Binary files
  - Relocatable object modules
- Extract file data to byte arrays or streams
- Read text files with automatic Apple II high ASCII decoding
- Support for .NET 9.0
- Zero external dependencies (core library)

## Installation

Add the project reference to your .NET application:

```sh
dotnet add reference path/to/AppleIIDiskReader.csproj
```

Or, if published on NuGet:

```sh
dotnet add package AppleIIDiskReader
```

## Usage

### Opening a Disk Image

```csharp
using AppleIIDiskReader;

// Open an Apple II disk image file
using var stream = File.OpenRead("disk.dsk");

// Parse the disk image
var disk = new AppleIIDisk(stream);

// Get VTOC information
Console.WriteLine($"Volume Number: {disk.VolumeTableOfContents.DiskVolumeNumber}");
Console.WriteLine($"Tracks: {disk.VolumeTableOfContents.TracksPerDiskette}");
Console.WriteLine($"Sectors per Track: {disk.VolumeTableOfContents.SectorsPerTrack}");
```

### Listing Files on the Disk

```csharp
// Enumerate all files on the disk
foreach (var file in disk.EnumerateFileEntries())
{
    Console.WriteLine($"{file.FileName} - {file.FileType} - {file.LengthInSectors} sectors");
    
    if (file.IsLocked)
    {
        Console.WriteLine("  [Locked]");
    }
}
```

### Reading File Data

```csharp
// Find a specific file
var file = disk.EnumerateFileEntries()
    .FirstOrDefault(f => f.FileName == "HELLO");

if (file.FirstTrackSectorListTrack != 0)
{
    // Read raw file data
    byte[] data = disk.ReadFileData(file);
    File.WriteAllBytes("HELLO.bin", data);
    
    // Or read text files with Apple II high ASCII conversion
    if (file.FileType == AppleIIFileType.Text)
    {
        string text = disk.ReadTextFile(file);
        Console.WriteLine(text);
    }
}
```

### Reading to a Stream

```csharp
using var outputStream = File.Create("output.bin");
disk.ReadFileData(file, outputStream);
```

## API Overview

### AppleIIDisk

The main class for reading Apple II DOS 3.3 disk images.

- `AppleIIDisk(Stream stream)` - Opens a disk image from a stream
- `VolumeTableOfContents` - Gets the VTOC containing disk metadata
- `EnumerateCatalogEntries()` - Enumerates all catalog sectors
- `EnumerateFileEntries()` - Enumerates all file descriptive entries
- `ReadFileData(FileDescriptiveEntry)` - Reads file data as a byte array
- `ReadFileData(FileDescriptiveEntry, Stream)` - Reads file data to a stream
- `ReadTextFile(FileDescriptiveEntry)` - Reads a text file with high ASCII conversion

### VolumeTableOfContents

Contains the disk VTOC metadata (located at track 17, sector 0):

- `DiskVolumeNumber` - Volume number (1-254)
- `FirstCatalogTrack` - Track of first catalog sector
- `FirstCatalogSector` - Sector of first catalog sector
- `TracksPerDiskette` - Number of tracks (normally 35)
- `SectorsPerTrack` - Sectors per track (13 or 16)
- `BytesPerSector` - Bytes per sector (normally 256)
- `FreeSectorBitMaps` - Bitmap of free sectors per track

### FileDescriptiveEntry

Represents a file on the disk:

- `FileName` - The file name (up to 30 characters)
- `FileType` - The file type (Text, IntegerBasic, ApplesoftBasic, Binary, etc.)
- `LengthInSectors` - File length in sectors
- `IsLocked` - Whether the file is locked
- `IsDeleted` - Whether the file has been deleted
- `IsUnused` - Whether the entry is unused

### AppleIIFileType

Enum of supported file types:

- `Text` - Text file ($00)
- `IntegerBasic` - Integer BASIC program ($01)
- `ApplesoftBasic` - Applesoft BASIC program ($02)
- `Binary` - Binary file ($04)
- `SType` - S type file ($08)
- `Relocatable` - Relocatable object module ($10)
- `AType` - A type file ($20)
- `BType` - B type file ($40)

## Building

Build the project using the .NET SDK:

```sh
dotnet build
```

Run tests:

```sh
dotnet test
```

## AppleIIDiskDumper CLI

Extract files from an Apple II disk image using the dumper tool.

### Install/Build

```sh
dotnet build dumper/AppleIIDiskDumper.csproj -c Release
```

### Usage

```sh
appleii-dumper <input> [-o|--output <path>]
```

- `<input>`: Path to the Apple II disk image file (.dsk, .do)
- `-o|--output`: Destination directory for extracted files (defaults to input filename)

Output files are named with appropriate extensions based on file type:
- `.txt` - Text files
- `.bas` - Applesoft BASIC files
- `.int` - Integer BASIC files
- `.bin` - Binary files
- `.rel` - Relocatable files

## Requirements

- .NET 9.0 or later

## License

MIT License. See [LICENSE](LICENSE) for details.

Copyright (c) 2025 Hugh Bellamy

## About Apple II DOS 3.3

DOS 3.3 was the primary disk operating system for the Apple II series of computers, released in 1980. It supported 5.25" floppy disks with the following characteristics:

- 140KB capacity (35 tracks × 16 sectors × 256 bytes)
- Volume Table of Contents (VTOC) at track 17, sector 0
- Catalog sectors containing up to 7 file entries each
- Track/sector list structure for file data allocation
- High ASCII text encoding (bit 7 set)

## Related Projects

- [DiskCopyReader](https://github.com/hughbe/DiskCopyReader) - Reader for Disk Copy 4.2 (.dc42) images
- [MfsReader](https://github.com/hughbe/MfsReader) - Reader for MFS (Macintosh File System) volumes
- [HfsReader](https://github.com/hughbe/HfsReader) - Reader for HFS (Hierarchical File System) volumes

## Documentation

- [Apple DOS File System - Just Solve](http://justsolve.archiveteam.org/wiki/Apple_DOS_file_system)
- [Beneath Apple DOS](https://archive.org/details/beneath-apple-dos/page/n35/mode/2up)