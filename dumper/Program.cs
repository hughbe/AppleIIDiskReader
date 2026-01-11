using Spectre.Console;
using Spectre.Console.Cli;
using AppleIIDiskReader;

public sealed class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp<ExtractCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("appleii-dumper");
            config.ValidateExamples();
        });

        return app.Run(args);
    }
}

sealed class ExtractSettings : CommandSettings
{
    [CommandArgument(0, "<input>")]
    public required string Input { get; init; }

    [CommandOption("-o|--output")]
    public string? Output { get; init; }
}

sealed class ExtractCommand : AsyncCommand<ExtractSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ExtractSettings settings, CancellationToken cancellationToken)
    {
        var input = new FileInfo(settings.Input);
        if (!input.Exists)
        {
            AnsiConsole.MarkupLine($"[red]Input file not found[/]: {input.FullName}");
            return -1;
        }

        var outputPath = settings.Output ?? Path.GetFileNameWithoutExtension(input.Name);
        var outputDir = new DirectoryInfo(outputPath);
        if (!outputDir.Exists)
        {
            outputDir.Create();
        }

        await using var stream = input.OpenRead();
        var disk = new AppleIIDisk(stream);

        var entries = disk.EnumerateFileEntries().ToList();
        AnsiConsole.MarkupLine($"[green]Found[/] {entries.Count} files on disk.");
        AnsiConsole.MarkupLine($"Volume: {disk.VolumeTableOfContents.DiskVolumeNumber}");

        foreach (var entry in entries)
        {
            var safeName = SanitizeName(entry.FileName);

            // Add extension based on file type
            string extension = entry.FileType switch
            {
                AppleIIFileType.Text => ".txt",
                AppleIIFileType.IntegerBasic => ".int",
                AppleIIFileType.ApplesoftBasic => ".bas",
                AppleIIFileType.Binary => ".bin",
                AppleIIFileType.Relocatable => ".rel",
                AppleIIFileType.SType => ".s",
                AppleIIFileType.AType => ".a",
                AppleIIFileType.BType => ".b",
                _ => ".dat"
            };

            var filePath = Path.Combine(outputDir.FullName, safeName + extension);

            // For text files, convert from Apple II high ASCII
            if (entry.FileType == AppleIIFileType.Text)
            {
                var text = disk.ReadTextFile(entry);
                await File.WriteAllTextAsync(filePath, text, cancellationToken);
                AnsiConsole.MarkupLine($"Wrote: {Path.GetFileName(filePath)} ({text.Length} chars) [Text]");
            }
            else
            {
                var data = disk.ReadFileData(entry);
                await File.WriteAllBytesAsync(filePath, data, cancellationToken);
                var lockedStr = entry.IsLocked ? " [Locked]" : "";
                AnsiConsole.MarkupLine($"Wrote: {Path.GetFileName(filePath)} ({data.Length} bytes) [{entry.FileType}]{lockedStr}");
            }
        }

        AnsiConsole.MarkupLine($"[green]Extraction complete[/]: {outputDir.FullName}");
        return 0;
    }

    private static string SanitizeName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var invalidChar in invalidChars)
        {
            name = name.Replace(invalidChar, '_');
        }

        return name;
    }
}
