using System.Diagnostics.CodeAnalysis;

using CommandLine;

namespace SUI.Client.Core.Watcher;

[ExcludeFromCodeCoverage(Justification = "CLI config file")]
public class WatcherArgs
{
    [Option('i', "input", Required = true, HelpText = "Directory to watch for incoming files.")]
    public string InputDirectory { get; set; } = string.Empty;

    [Option('o', "output", Required = true, HelpText = "Directory to move processed files to.")]
    public string OutputDirectory { get; set; } = string.Empty;

    [Option('u', "uri", Required = true, HelpText = "Base URL of the service API.")]
    public string ApiBaseUrl { get; set; } = string.Empty;

    [Option('g', "enable-gender", Required = false, HelpText = "Enable gender option.")]
    public bool EnableGenderSearch { get; set; } = false;

    [Option('r', "enable-reconciliation", Required = false, HelpText = "Enable reconciliation report.")]
    public bool EnableReconciliation { get; set; } = false;

    [Option('m', "matched-dir", Required = false, HelpText = "Directory to write matched records to. If not set, this feature is disabled.")]
    public string? MatchedRecordsDirectory { get; set; }

    [Option('s', "search-strategy", Required = false, HelpText = "Choose only if you understand the different algorithms in the API")]
    public string? SearchStrategy { get; set; }
}