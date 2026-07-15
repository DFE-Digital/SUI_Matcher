using System.Diagnostics.CodeAnalysis;

using HotChocolate;

namespace FakeEclipseGraphQLApi.Models;

[ExcludeFromCodeCoverage]
[InputObjectType(Name = "UpdatePerson")]
public class UpdatePerson
{
    [GraphQLType(typeof(NonNullType<IdType>))]
    public string Id { get; set; } = string.Empty;

    public string? NhsNumber { get; set; }

    public int ObjectVersion { get; set; }

    public List<PersonType>? PersonTypes { get; set; }
}