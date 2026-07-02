using System.Diagnostics.CodeAnalysis;

namespace FakeEclipseGraphQLApi.Models;

[ExcludeFromCodeCoverage]
public class Address
{
    public string Id { get; set; } = string.Empty;
    public Location? Location { get; set; }
}