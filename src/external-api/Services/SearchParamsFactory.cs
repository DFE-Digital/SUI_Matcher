using System.Collections;

using Hl7.Fhir.Rest;

using Shared.Models;

namespace ExternalApi.Services;

public static class SearchParamsFactory
{
    public static SearchParams Create(SearchQuery query)
    {
        var searchParams = new SearchParams();
        var queryMap = query.ToDictionary();

        var keyValuePairs = queryMap.SelectMany(kvp =>
            kvp.Value is IEnumerable enumerable and not string
                ? enumerable.Cast<object?>().Select(item => (kvp.Key, Value: item?.ToString() ?? string.Empty))
                : [(kvp.Key, Value: kvp.Value.ToString() ?? string.Empty)]
        );

        foreach ((string key, string value) in keyValuePairs)
        {
            searchParams.Add(key, value);
        }

        return searchParams;
    }
}