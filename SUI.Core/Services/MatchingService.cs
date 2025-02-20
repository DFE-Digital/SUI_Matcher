using Shared.Models;
using SUI.Core.Domain;
using SUI.Core.Endpoints;

namespace SUI.Core.Services;

public interface IMatchingService
{
    Task<MatchResult> SearchAsync(PersonSpecification personSpecification);
}

public class MatchingService(INhsFhirClient nhsFhirClient, IValidationService validationService) : IMatchingService
{
	public async Task<MatchResult> SearchAsync(PersonSpecification personSpecification)
	{
        var validationResults = validationService.Validate(personSpecification);

        if (validationResults.Results!.Any())
        {
            return new(validationResults);
        }
        else
        {
            return await MatchAsync(personSpecification);
        }
    }


    private async Task<MatchResult> MatchAsync(PersonSpecification model)
    {
        var queries = GetSearchQueries(model);

        foreach (var query in queries)
        {
            var searchResult = await nhsFhirClient.PerformSearch(query);
            if (searchResult != null)
            {
                if (searchResult.Type == SearchResult.ResultType.Matched)
                {
                    var status = searchResult.Score.GetValueOrDefault() >= 0.95m ? MatchStatus.Confirmed : MatchStatus.Candidate;
                    return new MatchResult(searchResult, status); // single match with confidence score
                }
                else if (searchResult.Type == SearchResult.ResultType.MultiMatched)
                {
                    return new MatchResult(searchResult, MatchStatus.Multiple); // multiple matches
                }
            }
        }

        return new MatchResult(MatchStatus.NoMatch);
    }

    private SearchQuery[] GetSearchQueries(PersonSpecification model)
    {
        var dobRange = new[] { "ge" + model.BirthDate.AddMonths(-6).ToString("yyyy-MM-dd"), "le" + model.BirthDate.AddMonths(6).ToString("yyyy-MM-dd") };
        var dob = new[] { "eq" + model.BirthDate.ToString("yyyy-MM-dd") };

        var queries = new List<SearchQuery>
        {
            new() // exact search
            {
                ExactMatch = true,
                Given = [model.Given],
                Family = model.Family,
                Email = model.Email,
                Gender = model.Gender,
                Phone = model.Phone,
                Birthdate = dob,
                AddressPostalcode = model.AddressPostalCode,
            },
            new() // 1. fuzzy search with given name, family name and DOB.
            {
                FuzzyMatch = true,
                Given = [model.Given],
                Family = model.Family,
                Email = model.Email,
                Gender = model.Gender,
                Phone = model.Phone,
                Birthdate = dob,
                AddressPostalcode = model.AddressPostalCode,
            },
            new() // 2. fuzzy search with given name, family name and DOB range 6 months either side of given date.
            {
                FuzzyMatch = true,
                Given = [model.Given],
                Family = model.Family,
                Birthdate = dobRange,
            },
        };

        // Only applicable if dob day is less than or equal to 12
        if (model.BirthDate.Day <= 12) // 5. fuzzy search with given name, family name and DOB. Day swapped with month if day equal to or less than 12.
        {
            var altDob = new DateTime(model.BirthDate.Year, model.BirthDate.Day, model.BirthDate.Month);

            queries.Add(new()
            {
                FuzzyMatch = true,
                Given = [model.Given],
                Family = model.Family,
                Email = model.Email,
                Gender = model.Gender,
                Phone = model.Phone,
                Birthdate = [$"eq{FormatDob(altDob)}"],
                AddressPostalcode = model.AddressPostalCode,
            });
        }

        if (queries.Count == 0)
        {
            throw new InvalidOperationException("No search queries were generated for model");
        }

        return [.. queries];
    }

    public string FormatDob(DateTime dob) => dob.ToString("yyyy-MM-dd");

}
