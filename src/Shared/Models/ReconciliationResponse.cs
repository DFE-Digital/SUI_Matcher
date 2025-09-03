using Newtonsoft.Json.Converters;

namespace Shared.Models;

public class ReconciliationResponse
{
    public NhsPerson? Person { get; set; }

    public List<string> Errors { get; set; } = [];

    public List<Difference>? Differences { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public ReconciliationStatus Status { get; set; }
}