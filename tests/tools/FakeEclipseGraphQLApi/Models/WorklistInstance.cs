using System.Diagnostics.CodeAnalysis;

namespace FakeEclipseGraphQLApi.Models;

[ExcludeFromCodeCoverage]
public class WorklistInstance
{
    public WorklistDefinition? WorklistDefinition { get; set; }
}