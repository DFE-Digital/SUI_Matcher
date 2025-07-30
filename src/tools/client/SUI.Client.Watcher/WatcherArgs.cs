namespace SUI.Client.Watcher;
using CommandLine;

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
}