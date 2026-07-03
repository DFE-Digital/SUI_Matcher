namespace SUI.Client.Core.Application.Models;

public sealed class OptionalPropertiesLog
{
    public const string SectionName = "OptionalPropertiesLog";

    public Dictionary<string, string> Fields { get; init; } = new();
}
