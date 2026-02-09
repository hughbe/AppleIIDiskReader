using AppleIIDiskReader;
using AppleIIDiskReader.Files;
using AppleIIDiskReader.Utilities;

namespace AppleIIDiskReader.Tests;

public class FloppyDiskValidationTests
{
    private static MemoryStream CreateDiskStream(int tracks = 35, int sectors = 16, int sectorSize = 256)
    {
        return new MemoryStream(new byte[tracks * sectors * sectorSize]);
    }

    [Fact]
    public void Ctor_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("stream", () => new FloppyDisk(null!, 35, 16, 256));
    }

    [Fact]
    public void Ctor_NonSeekableStream_ThrowsArgumentException()
    {
        using var stream = new NonSeekableStream();
        var ex = Assert.Throws<ArgumentException>("stream", () => new FloppyDisk(stream, 35, 16, 256));
        Assert.Contains("seekable and readable", ex.Message);
    }

    [Fact]
    public void Ctor_NonReadableStream_ThrowsArgumentException()
    {
        using var stream = new NonReadableStream();
        var ex = Assert.Throws<ArgumentException>("stream", () => new FloppyDisk(stream, 35, 16, 256));
        Assert.Contains("seekable and readable", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Ctor_InvalidNumberOfTracks_ThrowsArgumentOutOfRangeException(int tracks)
    {
        using var stream = CreateDiskStream();
        Assert.Throws<ArgumentOutOfRangeException>("numberOfTracks", () => new FloppyDisk(stream, tracks, 16, 256));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Ctor_InvalidNumberOfSectors_ThrowsArgumentOutOfRangeException(int sectors)
    {
        using var stream = CreateDiskStream();
        Assert.Throws<ArgumentOutOfRangeException>("numberOfSectors", () => new FloppyDisk(stream, 35, sectors, 256));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Ctor_InvalidSectorSize_ThrowsArgumentOutOfRangeException(int sectorSize)
    {
        using var stream = CreateDiskStream();
        Assert.Throws<ArgumentOutOfRangeException>("sectorSize", () => new FloppyDisk(stream, 35, 16, sectorSize));
    }

    [Fact]
    public void Ctor_ValidParameters_Succeeds()
    {
        using var stream = CreateDiskStream();
        var disk = new FloppyDisk(stream, 35, 16, 256);
        Assert.Equal(35, disk.NumberOfTracks);
        Assert.Equal(16, disk.NumberOfSectors);
        Assert.Equal(256, disk.SectorSize);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(35)]
    [InlineData(100)]
    public void ReadSector_InvalidTrack_ThrowsArgumentOutOfRangeException(int track)
    {
        using var stream = CreateDiskStream();
        var disk = new FloppyDisk(stream, 35, 16, 256);
        var buffer = new byte[256];
        Assert.Throws<ArgumentOutOfRangeException>("track", () => disk.ReadSector(track, 0, buffer));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(16)]
    [InlineData(100)]
    public void ReadSector_InvalidSector_ThrowsArgumentOutOfRangeException(int sector)
    {
        using var stream = CreateDiskStream();
        var disk = new FloppyDisk(stream, 35, 16, 256);
        var buffer = new byte[256];
        Assert.Throws<ArgumentOutOfRangeException>("sector", () => disk.ReadSector(0, sector, buffer));
    }

    [Fact]
    public void ReadSector_BufferTooSmall_ThrowsArgumentException()
    {
        using var stream = CreateDiskStream();
        var disk = new FloppyDisk(stream, 35, 16, 256);
        var buffer = new byte[100];
        var ex = Assert.Throws<ArgumentException>("buffer", () => disk.ReadSector(0, 0, buffer));
        Assert.Contains("too small", ex.Message);
    }

    [Fact]
    public void ReadSector_ValidParameters_ReturnsData()
    {
        using var stream = CreateDiskStream();
        var disk = new FloppyDisk(stream, 35, 16, 256);
        Span<byte> buffer = stackalloc byte[256];
        int bytesRead = disk.ReadSector(0, 0, buffer);
        Assert.True(bytesRead >= 0);
    }

    [Fact]
    public void ReadSector_BoundaryTrackAndSector_Succeeds()
    {
        using var stream = CreateDiskStream();
        var disk = new FloppyDisk(stream, 35, 16, 256);
        Span<byte> buffer = stackalloc byte[256];

        // First valid track/sector
        disk.ReadSector(0, 0, buffer);

        // Last valid track/sector
        disk.ReadSector(34, 15, buffer);
    }

    private class NonSeekableStream : MemoryStream
    {
        public NonSeekableStream() : base(new byte[35 * 16 * 256]) { }
        public override bool CanSeek => false;
    }

    private class NonReadableStream : MemoryStream
    {
        public NonReadableStream() : base(new byte[35 * 16 * 256]) { }
        public override bool CanRead => false;
    }
}

public class VolumeTableOfContentsValidationTests
{
    [Fact]
    public void Ctor_DataTooShort_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>("data", () => new VolumeTableOfContents(new byte[100]));
        Assert.Contains($"{VolumeTableOfContents.Size}", ex.Message);
    }

    [Fact]
    public void Ctor_DataTooLong_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>("data", () => new VolumeTableOfContents(new byte[300]));
        Assert.Contains($"{VolumeTableOfContents.Size}", ex.Message);
    }

    [Fact]
    public void Ctor_EmptyData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new VolumeTableOfContents(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Ctor_ExactSize_Succeeds()
    {
        var data = new byte[VolumeTableOfContents.Size];
        var vtoc = new VolumeTableOfContents(data);
        Assert.Equal(0, vtoc.DiskVolumeNumber);
    }
}

public class CatalogEntryValidationTests
{
    [Fact]
    public void Ctor_DataTooShort_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>("data", () => new CatalogEntry(new byte[100]));
        Assert.Contains($"{CatalogEntry.Size}", ex.Message);
    }

    [Fact]
    public void Ctor_DataTooLong_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>("data", () => new CatalogEntry(new byte[300]));
        Assert.Contains($"{CatalogEntry.Size}", ex.Message);
    }

    [Fact]
    public void Ctor_EmptyData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new CatalogEntry(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Ctor_ExactSize_Succeeds()
    {
        var data = new byte[CatalogEntry.Size];
        var entry = new CatalogEntry(data);
        Assert.Equal(0, entry.NextCatalogTrack);
    }
}

public class FileDescriptiveEntryValidationTests
{
    [Fact]
    public void Ctor_DataTooShort_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>("data", () => new FileDescriptiveEntry(new byte[10]));
        Assert.Contains($"{FileDescriptiveEntry.Size}", ex.Message);
    }

    [Fact]
    public void Ctor_DataTooLong_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>("data", () => new FileDescriptiveEntry(new byte[100]));
        Assert.Contains($"{FileDescriptiveEntry.Size}", ex.Message);
    }

    [Fact]
    public void Ctor_EmptyData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new FileDescriptiveEntry(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Ctor_ExactSize_Succeeds()
    {
        var data = new byte[FileDescriptiveEntry.Size];
        var entry = new FileDescriptiveEntry(data);
        Assert.True(entry.IsUnused);
    }

    [Fact]
    public void IsUnused_WhenTrackIsZero_ReturnsTrue()
    {
        var data = new byte[FileDescriptiveEntry.Size];
        data[0] = 0x00;
        var entry = new FileDescriptiveEntry(data);
        Assert.True(entry.IsUnused);
    }

    [Fact]
    public void IsDeleted_WhenTrackIsFF_ReturnsTrue()
    {
        var data = new byte[FileDescriptiveEntry.Size];
        data[0] = 0xFF;
        var entry = new FileDescriptiveEntry(data);
        Assert.True(entry.IsDeleted);
    }

    [Fact]
    public void IsLocked_WhenHighBitSet_ReturnsTrue()
    {
        var data = new byte[FileDescriptiveEntry.Size];
        data[0] = 0x11; // Valid track
        data[2] = 0x82; // Locked + ApplesoftBasic
        var entry = new FileDescriptiveEntry(data);
        Assert.True(entry.IsLocked);
        Assert.Equal(AppleIIFileType.ApplesoftBasic, entry.FileType);
    }

    [Fact]
    public void IsLocked_WhenHighBitClear_ReturnsFalse()
    {
        var data = new byte[FileDescriptiveEntry.Size];
        data[0] = 0x11; // Valid track
        data[2] = 0x04; // Binary, not locked
        var entry = new FileDescriptiveEntry(data);
        Assert.False(entry.IsLocked);
        Assert.Equal(AppleIIFileType.Binary, entry.FileType);
    }
}

public class TrackSectorListValidationTests
{
    [Fact]
    public void Ctor_DataTooShort_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>("data", () => new TrackSectorList(new byte[100]));
        Assert.Contains($"{TrackSectorList.Size}", ex.Message);
    }

    [Fact]
    public void Ctor_DataTooLong_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>("data", () => new TrackSectorList(new byte[300]));
        Assert.Contains($"{TrackSectorList.Size}", ex.Message);
    }

    [Fact]
    public void Ctor_EmptyData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new TrackSectorList(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Ctor_ExactSize_Succeeds()
    {
        var data = new byte[TrackSectorList.Size];
        var tsList = new TrackSectorList(data);
        Assert.Equal(TrackSectorList.MaxTrackSectorPairs, tsList.DataSectors.Length);
    }

    [Fact]
    public void HasNextTrackSectorList_WhenTrackZero_ReturnsFalse()
    {
        var data = new byte[TrackSectorList.Size];
        var tsList = new TrackSectorList(data);
        Assert.False(tsList.HasNextTrackSectorList);
    }

    [Fact]
    public void HasNextTrackSectorList_WhenTrackNonZero_ReturnsTrue()
    {
        var data = new byte[TrackSectorList.Size];
        data[1] = 0x11; // Next track
        var tsList = new TrackSectorList(data);
        Assert.True(tsList.HasNextTrackSectorList);
    }
}

public class TrackSectorPairValidationTests
{
    [Fact]
    public void Ctor_DataTooShort_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>("data", () => new TrackSectorPair(new byte[1]));
        Assert.Contains($"{TrackSectorPair.Size}", ex.Message);
    }

    [Fact]
    public void Ctor_DataTooLong_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>("data", () => new TrackSectorPair(new byte[10]));
        Assert.Contains($"{TrackSectorPair.Size}", ex.Message);
    }

    [Fact]
    public void Ctor_EmptyData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new TrackSectorPair(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Ctor_ExactSize_Succeeds()
    {
        var data = new byte[] { 0x11, 0x0F };
        var pair = new TrackSectorPair(data);
        Assert.Equal(0x11, pair.Track);
        Assert.Equal(0x0F, pair.Sector);
    }

    [Fact]
    public void IsEmpty_WhenBothZero_ReturnsTrue()
    {
        var pair = new TrackSectorPair(0, 0);
        Assert.True(pair.IsEmpty);
    }

    [Fact]
    public void IsEmpty_WhenTrackNonZero_ReturnsFalse()
    {
        var pair = new TrackSectorPair(1, 0);
        Assert.False(pair.IsEmpty);
    }

    [Fact]
    public void IsEmpty_WhenSectorNonZero_ReturnsFalse()
    {
        var pair = new TrackSectorPair(0, 1);
        Assert.False(pair.IsEmpty);
    }
}

public class VtocBitMapValidationTests
{
    [Fact]
    public void Ctor_DataTooShort_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>("data", () => new VtocBitMap(new byte[100]));
        Assert.Contains($"{VtocBitMap.Size}", ex.Message);
    }

    [Fact]
    public void Ctor_DataTooLong_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>("data", () => new VtocBitMap(new byte[300]));
        Assert.Contains($"{VtocBitMap.Size}", ex.Message);
    }

    [Fact]
    public void Ctor_EmptyData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new VtocBitMap(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void GetFreeSectorBitMap_NegativeTrack_ThrowsArgumentOutOfRangeException()
    {
        var bitmap = new VtocBitMap(new byte[VtocBitMap.Size]);
        Assert.Throws<ArgumentOutOfRangeException>("track", () => bitmap.GetFreeSectorBitMap(-1));
    }

    [Fact]
    public void GetFreeSectorBitMap_TrackTooLarge_ThrowsArgumentOutOfRangeException()
    {
        var bitmap = new VtocBitMap(new byte[VtocBitMap.Size]);
        // 200 / 4 = 50 tracks max. Track 50 should fail.
        Assert.Throws<ArgumentOutOfRangeException>("track", () => bitmap.GetFreeSectorBitMap(50));
    }

    [Fact]
    public void GetFreeSectorBitMap_MaxValidTrack_Succeeds()
    {
        var bitmap = new VtocBitMap(new byte[VtocBitMap.Size]);
        // Track 49 is the last valid one (49 * 4 + 4 = 200 = Size)
        var result = bitmap.GetFreeSectorBitMap(49);
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public void GetFreeSectorBitMap_TrackZero_Succeeds()
    {
        var bitmap = new VtocBitMap(new byte[VtocBitMap.Size]);
        var result = bitmap.GetFreeSectorBitMap(0);
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public void IsSectorFree_NegativeTrack_ThrowsArgumentOutOfRangeException()
    {
        var bitmap = new VtocBitMap(new byte[VtocBitMap.Size]);
        Assert.Throws<ArgumentOutOfRangeException>("track", () => bitmap.IsSectorFree(-1, 0, 16));
    }

    [Fact]
    public void IsSectorFree_NegativeSector_ThrowsArgumentOutOfRangeException()
    {
        var bitmap = new VtocBitMap(new byte[VtocBitMap.Size]);
        Assert.Throws<ArgumentOutOfRangeException>("sector", () => bitmap.IsSectorFree(0, -1, 16));
    }

    [Fact]
    public void IsSectorFree_SectorEqualToSectorsPerTrack_ThrowsArgumentOutOfRangeException()
    {
        var bitmap = new VtocBitMap(new byte[VtocBitMap.Size]);
        Assert.Throws<ArgumentOutOfRangeException>("sector", () => bitmap.IsSectorFree(0, 16, 16));
    }

    [Fact]
    public void IsSectorFree_TrackTooLarge_ThrowsArgumentOutOfRangeException()
    {
        var bitmap = new VtocBitMap(new byte[VtocBitMap.Size]);
        Assert.Throws<ArgumentOutOfRangeException>("track", () => bitmap.IsSectorFree(50, 0, 16));
    }

    [Fact]
    public void IsSectorFree_AllZeros_ReturnsNotFree()
    {
        var bitmap = new VtocBitMap(new byte[VtocBitMap.Size]);
        Assert.False(bitmap.IsSectorFree(0, 0, 16));
    }

    [Fact]
    public void IsSectorFree_AllOnes_ReturnsFree()
    {
        var data = new byte[VtocBitMap.Size];
        Array.Fill(data, (byte)0xFF);
        var bitmap = new VtocBitMap(data);
        Assert.True(bitmap.IsSectorFree(0, 0, 16));
    }

    [Fact]
    public void IsSectorFree_SpecificBitSet_ReturnsFreeForThatSector()
    {
        var data = new byte[VtocBitMap.Size];
        // Set bit for sector 0 on track 0 (MSB of first byte = 0x80)
        data[0] = 0x80;
        var bitmap = new VtocBitMap(data);
        Assert.True(bitmap.IsSectorFree(0, 0, 16));
        Assert.False(bitmap.IsSectorFree(0, 1, 16));
    }
}

public class InlineArrayValidationTests
{
    [Fact]
    public void ByteArray5_WrongSize_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new ByteArray5(new byte[3]));
        Assert.Throws<ArgumentException>("data", () => new ByteArray5(new byte[10]));
        Assert.Throws<ArgumentException>("data", () => new ByteArray5(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void ByteArray5_ExactSize_Succeeds()
    {
        var arr = new ByteArray5(new byte[ByteArray5.Size]);
        Assert.Equal(ByteArray5.Size, arr.AsSpan().Length);
    }

    [Fact]
    public void ByteArray8_WrongSize_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new ByteArray8(new byte[3]));
        Assert.Throws<ArgumentException>("data", () => new ByteArray8(new byte[20]));
        Assert.Throws<ArgumentException>("data", () => new ByteArray8(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void ByteArray8_ExactSize_Succeeds()
    {
        var arr = new ByteArray8(new byte[ByteArray8.Size]);
        Assert.Equal(ByteArray8.Size, arr.AsSpan().Length);
    }

    [Fact]
    public void ByteArray30_WrongSize_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new ByteArray30(new byte[10]));
        Assert.Throws<ArgumentException>("data", () => new ByteArray30(new byte[50]));
        Assert.Throws<ArgumentException>("data", () => new ByteArray30(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void ByteArray30_ExactSize_Succeeds()
    {
        var arr = new ByteArray30(new byte[ByteArray30.Size]);
        Assert.Equal(ByteArray30.Size, arr.AsSpan().Length);
    }

    [Fact]
    public void ByteArray32_WrongSize_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new ByteArray32(new byte[10]));
        Assert.Throws<ArgumentException>("data", () => new ByteArray32(new byte[50]));
        Assert.Throws<ArgumentException>("data", () => new ByteArray32(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void ByteArray32_ExactSize_Succeeds()
    {
        var arr = new ByteArray32(new byte[ByteArray32.Size]);
        Assert.Equal(ByteArray32.Size, arr.AsSpan().Length);
    }
}

public class BinaryFileValidationTests
{
    [Fact]
    public void Ctor_EmptyData_HandledGracefully()
    {
        var file = new BinaryFile(ReadOnlySpan<byte>.Empty);
        Assert.Equal(0, file.Address);
        Assert.Equal(0, file.Length);
        Assert.Empty(file.Data);
    }

    [Fact]
    public void Ctor_ShortData_HandledGracefully()
    {
        var file = new BinaryFile(new byte[] { 0x01, 0x02 });
        Assert.Equal(0, file.Address);
        Assert.Equal(0, file.Length);
        Assert.Equal(2, file.Data.Length);
    }

    [Fact]
    public void Ctor_ExactMinSize_Succeeds()
    {
        var data = new byte[BinaryFile.MinSize];
        var file = new BinaryFile(data);
        Assert.Equal(0, file.Address);
        Assert.Equal(0, file.Length);
        Assert.Empty(file.Data);
    }

    [Fact]
    public void Ctor_TruncatedFile_ClampedToAvailable()
    {
        // Header says length is 1000 but only 10 bytes of actual data
        var data = new byte[14]; // 4 header + 10 data
        data[2] = 0xE8; // Length low = 1000
        data[3] = 0x03; // Length high
        var file = new BinaryFile(data);
        Assert.Equal(1000, file.Length);
        Assert.Equal(10, file.Data.Length); // Clamped to available
    }

    [Fact]
    public void Ctor_ValidBinaryFile_ParsesCorrectly()
    {
        var data = new byte[8];
        data[0] = 0x00; data[1] = 0xD0; // Address = 0xD000
        data[2] = 0x04; data[3] = 0x00; // Length = 4
        data[4] = 0xAA; data[5] = 0xBB; data[6] = 0xCC; data[7] = 0xDD;
        var file = new BinaryFile(data);
        Assert.Equal(0xD000, file.Address);
        Assert.Equal(4, file.Length);
        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }, file.Data);
    }
}

public class ApplesoftBasicFileValidationTests
{
    [Fact]
    public void Ctor_EmptyData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new ApplesoftBasicFile(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Ctor_OneByte_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new ApplesoftBasicFile(new byte[1]));
    }

    [Fact]
    public void Ctor_LengthExceedsData_ThrowsArgumentException()
    {
        var data = new byte[4];
        data[0] = 0xFF; data[1] = 0x00; // Length = 255, but only 2 bytes remain
        Assert.Throws<ArgumentException>("data", () => new ApplesoftBasicFile(data));
    }

    [Fact]
    public void Ctor_ExactMinSize_Succeeds()
    {
        var data = new byte[ApplesoftBasicFile.MinSize]; // Length = 0
        var file = new ApplesoftBasicFile(data);
        Assert.Equal(0, file.Length);
        Assert.Empty(file.Data);
    }

    [Fact]
    public void Ctor_ValidFile_ParsesCorrectly()
    {
        var data = new byte[6];
        data[0] = 0x04; data[1] = 0x00; // Length = 4
        data[2] = 0xAA; data[3] = 0xBB; data[4] = 0xCC; data[5] = 0xDD;
        var file = new ApplesoftBasicFile(data);
        Assert.Equal(4, file.Length);
        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }, file.Data);
    }
}

public class IntegerBasicFileValidationTests
{
    [Fact]
    public void Ctor_EmptyData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new IntegerBasicFile(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Ctor_OneByte_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new IntegerBasicFile(new byte[1]));
    }

    [Fact]
    public void Ctor_LengthExceedsData_ThrowsArgumentException()
    {
        var data = new byte[4];
        data[0] = 0xFF; data[1] = 0x00; // Length = 255, but only 2 bytes remain
        Assert.Throws<ArgumentException>("data", () => new IntegerBasicFile(data));
    }

    [Fact]
    public void Ctor_ExactMinSize_Succeeds()
    {
        var data = new byte[IntegerBasicFile.MinSize]; // Length = 0
        var file = new IntegerBasicFile(data);
        Assert.Equal(0, file.Length);
        Assert.Empty(file.Data);
    }

    [Fact]
    public void Ctor_ValidFile_ParsesCorrectly()
    {
        var data = new byte[6];
        data[0] = 0x04; data[1] = 0x00; // Length = 4
        data[2] = 0xAA; data[3] = 0xBB; data[4] = 0xCC; data[5] = 0xDD;
        var file = new IntegerBasicFile(data);
        Assert.Equal(4, file.Length);
        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }, file.Data);
    }
}

public class ApplesoftBasicLineValidationTests
{
    [Fact]
    public void Ctor_EmptyData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new ApplesoftBasicLine(ReadOnlySpan<byte>.Empty, out _));
    }

    [Fact]
    public void Ctor_DataTooShort_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new ApplesoftBasicLine(new byte[3], out _));
    }

    [Fact]
    public void Ctor_MinValidLine_Succeeds()
    {
        // 2 bytes address + 2 bytes line number + null terminator
        var data = new byte[] { 0x05, 0x00, 0x0A, 0x00, 0x00 };
        var line = new ApplesoftBasicLine(data, out int bytesRead);
        Assert.Equal(5, line.AddressOfNextLine);
        Assert.Equal(10, line.LineNumber);
        Assert.Equal(5, bytesRead);
    }
}

public class IntegerBasicLineValidationTests
{
    [Fact]
    public void Ctor_EmptyData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new IntegerBasicLine(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Ctor_DataTooShort_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new IntegerBasicLine(new byte[2]));
    }

    [Fact]
    public void Ctor_LengthTooSmall_ThrowsArgumentException()
    {
        // Length byte = 2, less than minimum of 3
        var data = new byte[] { 0x02, 0x0A, 0x00, 0x01 };
        Assert.Throws<ArgumentException>("data", () => new IntegerBasicLine(data));
    }

    [Fact]
    public void Ctor_LengthExceedsData_ThrowsArgumentException()
    {
        // Length byte says 10, but only 3 bytes of data
        var data = new byte[] { 0x0A, 0x0A, 0x00 };
        Assert.Throws<ArgumentException>("data", () => new IntegerBasicLine(data));
    }

    [Fact]
    public void Ctor_MinValidLine_Succeeds()
    {
        // Length=3, LineNumber=10, no content
        var data = new byte[] { 0x03, 0x0A, 0x00 };
        var line = new IntegerBasicLine(data);
        Assert.Equal(3, line.Length);
        Assert.Equal(10, line.LineNumber);
        Assert.Empty(line.Content);
    }
}

public class RelocatableBinaryFileValidationTests
{
    [Fact]
    public void Ctor_EmptyData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new RelocatableBinaryFile(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Ctor_DataTooShort_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new RelocatableBinaryFile(new byte[5]));
    }

    [Fact]
    public void Ctor_CodeImageLengthExceedsData_ThrowsArgumentException()
    {
        var data = new byte[8];
        data[4] = 0xFF; data[5] = 0x00; // CodeImageLength = 255, but only 2 bytes remain
        Assert.Throws<ArgumentException>("data", () => new RelocatableBinaryFile(data));
    }

    [Fact]
    public void Ctor_MinValidFile_Succeeds()
    {
        // 6 byte header + 0 code image + 0x00 end of RLD + 0x00 end of ESD
        var data = new byte[8];
        data[6] = 0x00; // End of RLD
        data[7] = 0x00; // End of ESD
        var file = new RelocatableBinaryFile(data);
        Assert.Equal(0, file.CodeImageLength);
        Assert.Empty(file.CodeImage);
        Assert.Empty(file.RelocationDictionary);
        Assert.Empty(file.ExternalSymbolDirectory);
    }
}

public class RelocationDictionaryEntryValidationTests
{
    [Fact]
    public void Ctor_DataTooShort_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new RelocationDictionaryEntry(new byte[2]));
    }

    [Fact]
    public void Ctor_DataTooLong_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new RelocationDictionaryEntry(new byte[10]));
    }

    [Fact]
    public void Ctor_EmptyData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new RelocationDictionaryEntry(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Ctor_ExactSize_Succeeds()
    {
        var data = new byte[RelocationDictionaryEntry.Size];
        var entry = new RelocationDictionaryEntry(data);
        Assert.Equal(0, entry.FieldOffset);
    }
}

public class ExternalSymbolDirectoryEntryValidationTests
{
    [Fact]
    public void Ctor_DataTooShort_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new ExternalSymbolDirectoryEntry(new byte[2], out _));
    }

    [Fact]
    public void Ctor_EmptyData_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("data", () => new ExternalSymbolDirectoryEntry(ReadOnlySpan<byte>.Empty, out _));
    }

    [Fact]
    public void Ctor_InsufficientDataAfterName_ThrowsArgumentException()
    {
        // Single char name (high bit clear = last char), but no room for flags + 2 bytes
        var data = new byte[] { 0x41, 0x00 }; // 'A' (last char), then only 1 byte
        Assert.Throws<ArgumentException>("data", () => new ExternalSymbolDirectoryEntry(data, out _));
    }

    [Fact]
    public void Ctor_ValidEntry_ParsesCorrectly()
    {
        // Name: "AB" (0xC1 = 'A' with high bit, 0x42 = 'B' without high bit = last)
        // Flags: 0x10 (Entry), SymbolNumber: 0x01, OffsetHigh: 0x02
        var data = new byte[] { 0xC1, 0x42, 0x10, 0x01, 0x02 };
        var entry = new ExternalSymbolDirectoryEntry(data, out int bytesRead);
        Assert.Equal("AB", entry.SymbolName);
        Assert.Equal(5, bytesRead);
    }
}

public class TextFileValidationTests
{
    [Fact]
    public void Ctor_EmptyData_Succeeds()
    {
        var file = new TextFile(ReadOnlySpan<byte>.Empty);
        Assert.Equal(string.Empty, file.Value);
    }

    [Fact]
    public void Ctor_HighAscii_DecodesCorrectly()
    {
        // 'H' = 0xC8, 'I' = 0xC9 in high ASCII
        var data = new byte[] { 0xC8, 0xC9 };
        var file = new TextFile(data);
        Assert.Equal("HI", file.Value);
    }
}

public class AppleIIDiskReadFileDataValidationTests
{
    [Fact]
    public void ReadFileData_UnusedEntry_ThrowsArgumentException()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "appleIIDos.dsk"));
        var disk = new AppleIIDisk(stream);

        // Create an unused entry (track = 0x00)
        var data = new byte[FileDescriptiveEntry.Size];
        data[0] = 0x00; // Unused
        var unusedEntry = new FileDescriptiveEntry(data);

        var ex = Assert.Throws<ArgumentException>("fileEntry", () => disk.ReadFileData(unusedEntry));
        Assert.Contains("unused", ex.Message);
    }

    [Fact]
    public void ReadFileData_DeletedEntry_ThrowsArgumentException()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "appleIIDos.dsk"));
        var disk = new AppleIIDisk(stream);

        // Create a deleted entry (track = 0xFF)
        var data = new byte[FileDescriptiveEntry.Size];
        data[0] = 0xFF; // Deleted
        var deletedEntry = new FileDescriptiveEntry(data);

        var ex = Assert.Throws<ArgumentException>("fileEntry", () => disk.ReadFileData(deletedEntry));
        Assert.Contains("deleted", ex.Message);
    }

    [Fact]
    public void ReadFileData_Stream_NullDestination_ThrowsArgumentNullException()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "appleIIDos.dsk"));
        var disk = new AppleIIDisk(stream);

        var fileEntry = disk.EnumerateFileEntries().First();
        Assert.Throws<ArgumentNullException>("destination", () => disk.ReadFileData(fileEntry, null!));
    }

    [Fact]
    public void ReadFileData_Stream_UnusedEntry_ThrowsArgumentException()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "appleIIDos.dsk"));
        var disk = new AppleIIDisk(stream);

        var data = new byte[FileDescriptiveEntry.Size];
        data[0] = 0x00; // Unused
        var unusedEntry = new FileDescriptiveEntry(data);

        using var destination = new MemoryStream();
        var ex = Assert.Throws<ArgumentException>("fileEntry", () => disk.ReadFileData(unusedEntry, destination));
        Assert.Contains("unused", ex.Message);
    }

    [Fact]
    public void ReadFileData_Stream_DeletedEntry_ThrowsArgumentException()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "appleIIDos.dsk"));
        var disk = new AppleIIDisk(stream);

        var data = new byte[FileDescriptiveEntry.Size];
        data[0] = 0xFF; // Deleted
        var deletedEntry = new FileDescriptiveEntry(data);

        using var destination = new MemoryStream();
        var ex = Assert.Throws<ArgumentException>("fileEntry", () => disk.ReadFileData(deletedEntry, destination));
        Assert.Contains("deleted", ex.Message);
    }

    [Fact]
    public void ReadFileData_ByteArray_MatchesStreamVersion()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "appleIIDos.dsk"));
        var disk = new AppleIIDisk(stream);

        var fileEntry = disk.EnumerateFileEntries().First(f => !f.IsDeleted);
        var byteArrayResult = disk.ReadFileData(fileEntry);

        using var destination = new MemoryStream();
        disk.ReadFileData(fileEntry, destination);
        var streamResult = destination.ToArray();

        Assert.Equal(byteArrayResult, streamResult);
    }

    [Fact]
    public void ReadTextFile_WithRelocatableFile_ThrowsArgumentException()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "appleIIDos.dsk"));
        var disk = new AppleIIDisk(stream);

        var fpbasic = disk.EnumerateFileEntries().Single(f => f.FileName == "FPBASIC");
        // FPBASIC is binary, not relocatable, but demonstrates wrong type check
        var ex = Assert.Throws<ArgumentException>("fileEntry", () => disk.ReadTextFile(fpbasic));
        Assert.Contains("not a text file", ex.Message);
    }

    [Fact]
    public void ReadRelocatableFile_WithTextFile_ThrowsArgumentException()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "appleIIDos.dsk"));
        var disk = new AppleIIDisk(stream);

        var appleProms = disk.EnumerateFileEntries().Single(f => f.FileName == "APPLE PROMS");
        var ex = Assert.Throws<ArgumentException>("fileEntry", () => disk.ReadRelocatableFile(appleProms));
        Assert.Contains("not a Relocatable file", ex.Message);
    }

    [Fact]
    public void ReadIntegerBasicFile_WithBinaryFile_ThrowsArgumentException()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "appleIIDos.dsk"));
        var disk = new AppleIIDisk(stream);

        var fpbasic = disk.EnumerateFileEntries().Single(f => f.FileName == "FPBASIC");
        Assert.Equal(AppleIIFileType.Binary, fpbasic.FileType);

        var ex = Assert.Throws<ArgumentException>("fileEntry", () => disk.ReadIntegerBasicFile(fpbasic));
        Assert.Contains("not an Integer BASIC file", ex.Message);
    }

    [Fact]
    public void ReadApplesoftBasicFile_WithBinaryFile_ThrowsArgumentException()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "appleIIDos.dsk"));
        var disk = new AppleIIDisk(stream);

        var fpbasic = disk.EnumerateFileEntries().Single(f => f.FileName == "FPBASIC");
        Assert.Equal(AppleIIFileType.Binary, fpbasic.FileType);

        var ex = Assert.Throws<ArgumentException>("fileEntry", () => disk.ReadApplesoftBasicFile(fpbasic));
        Assert.Contains("not an Applesoft BASIC file", ex.Message);
    }

    [Fact]
    public void ReadBinaryFile_WithIntegerBasicFile_ThrowsArgumentException()
    {
        using var stream = File.OpenRead(Path.Combine("Samples", "appleIIDos.dsk"));
        var disk = new AppleIIDisk(stream);

        var animals = disk.EnumerateFileEntries().Single(f => f.FileName == "ANIMALS");
        Assert.Equal(AppleIIFileType.IntegerBasic, animals.FileType);

        var ex = Assert.Throws<ArgumentException>("fileEntry", () => disk.ReadBinaryFile(animals));
        Assert.Contains("not a binary file", ex.Message);
    }
}

public class AppleIIDiskConstructorValidationTests
{
    [Fact]
    public void Ctor_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("stream", () => new AppleIIDisk(null!));
    }

    [Fact]
    public void Ctor_NonSeekableStream_ThrowsArgumentException()
    {
        using var inner = new MemoryStream(new byte[35 * 16 * 256]);
        using var stream = new NonSeekableWrapper(inner);
        Assert.Throws<ArgumentException>("stream", () => new AppleIIDisk(stream));
    }

    private class NonSeekableWrapper : Stream
    {
        private readonly Stream _inner;
        public NonSeekableWrapper(Stream inner) => _inner = inner;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
    }
}
