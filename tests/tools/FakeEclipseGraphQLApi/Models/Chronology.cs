using System.Collections.Generic;

namespace FakeEclipseGraphQLApi.Models;

public class Chronology
{
    public List<ChronologyEntry> ChronologyEntries { get; set; } = new();
}