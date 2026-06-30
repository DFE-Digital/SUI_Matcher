namespace SUI.Client.GraphQLProcessJob;

public sealed class GraphQlProcessJobOptions
{
    public const string SectionName = "GraphQLProcessJob";

    public string? Url { get; init; }
    public string? TenantId { get; init; }
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public string? Scope { get; init; }
    public int? MaxAge { get; init; }
    public bool UseAuth { get; init; }
    public string? MatchApiBaseAddress { get; set; }
}