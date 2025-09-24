using Shared;
using Shared.Models;

namespace MatchingApi.Search;

/// <summary>
/// Non-Fuzzy and Fuzzy search with smaller DOB range.
/// Includes DOB range in non-fuzzy search and postcode in fuzzy search.
/// </summary>
public class SearchStrategy3 : ISearchStrategy
{
    private const int AlgorithmVersion = 1;

    public OrderedDictionary<string, SearchQuery> BuildQuery(SearchSpecification model)
    {
        if (!model.BirthDate.HasValue)
        {
            throw new InvalidOperationException("Birthdate is required for search queries");
        }

        var dobRange = new[]
        {
            "ge" + model.BirthDate.Value.AddMonths(-1).ToString(SharedConstants.SearchQuery.DateFormat),
            "le" + model.BirthDate.Value.AddMonths(1).ToString(SharedConstants.SearchQuery.DateFormat)
        };
        
        var dob = new[] { "eq" + model.BirthDate.Value.ToString(SharedConstants.SearchQuery.DateFormat) };

        var modelName = model.Given is not null ? new[] { model.Given } : null;
        var queryOrderedMap = new OrderedDictionary<string, SearchQuery>
        {
            {
                "NonFuzzyGFD", new SearchQuery() // 1. non-fuzzy search on only given, family and dob
                {
                    ExactMatch = false, Given = modelName, Family = model.Family, Birthdate = dob
                }
            },
            {
                "NonFuzzyGFDRange", new SearchQuery() // 2. non-fuzzy search on only given, family and dob range
                {
                    ExactMatch = false, Given = modelName, Family = model.Family, Birthdate = dobRange
                }
            },
            {
                "NonFuzzyAll", new SearchQuery() // 3. non-fuzzy search
                {
                    ExactMatch = false,
                    Given = modelName,
                    Family = model.Family,
                    Email = model.Email,
                    Gender = model.Gender,
                    Phone = model.Phone,
                    Birthdate = dob,
                    AddressPostalcode = model.AddressPostalCode,
                }
            },
            {
                "FuzzyGFD", new SearchQuery() // 4. fuzzy search on only given, family and dob
                {
                    FuzzyMatch = true, Given = modelName, Family = model.Family, Birthdate = dob
                }
            },
            {
                "FuzzyAll", new SearchQuery() // 5. fuzzy search with given name, family name and DOB.
                {
                    FuzzyMatch = true,
                    Given = modelName,
                    Family = model.Family,
                    Email = model.Email,
                    Gender = model.Gender,
                    Phone = model.Phone,
                    Birthdate = dob,
                    AddressPostalcode = model.AddressPostalCode,
                }
            },
            {
                "FuzzyGFDRangePostcode",
                new SearchQuery() // 6. fuzzy search with given name, family name and DOB range either side of given date.
                {
                    FuzzyMatch = true, Given = modelName, Family = model.Family, Birthdate = dobRange, AddressPostalcode = model.AddressPostalCode
                }
            },
            {
                "FuzzyGFDRange",
                new SearchQuery() // 7. fuzzy search with given name, family name and DOB range either side of given date.
                {
                    FuzzyMatch = true, Given = modelName, Family = model.Family, Birthdate = dobRange,
                }
            },
        };

        // Only applicable if dob day is less than or equal to 12
        if (model.BirthDate.Value.Day <=
            12) // fuzzy search with given name, family name and DOB. Day swapped with month if day equal to or less than 12.
        {
            var altDob = new DateTime(
                model.BirthDate.Value.Year,
                model.BirthDate.Value.Day,
                model.BirthDate.Value.Month,
                0, 0, 0,
                DateTimeKind.Unspecified
            );

            queryOrderedMap.Add("FuzzyAltDob", new SearchQuery
            {
                FuzzyMatch = true,
                Given = modelName,
                Family = model.Family,
                Email = model.Email,
                Gender = model.Gender,
                Phone = model.Phone,
                Birthdate = [$"eq{altDob:yyyy-MM-dd}"],
                AddressPostalcode = model.AddressPostalCode,
            });
        }

        return queryOrderedMap;
    }

    public int GetAlgorithmVersion()
    {
        return AlgorithmVersion;
    }
}