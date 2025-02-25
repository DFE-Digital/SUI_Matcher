using System.Text.Json;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shared.Models;
using SUI.Core.Domain;
using SUI.Core.Endpoints;

namespace SUI.Core.Services;

public interface IMatchingService
{
    Task<PersonMatchResponse> SearchAsync(PersonSpecification personSpecification);
}

public class MatchingService(
    ILogger<MatchingService> logger, 
    INhsFhirClient nhsFhirClient, 
    IValidationService validationService) : IMatchingService
{
	public async Task<PersonMatchResponse> SearchAsync(PersonSpecification personSpecification)
	{
        logger.LogInformation("Validating the person data fields");
        
        var validationResults = validationService.Validate(personSpecification);

        var dataQualityResult = ToQualityResult(personSpecification, validationResults.Results!);
        
        if (!HasMinDataRequirements(dataQualityResult))
        {
            logger.LogError($"Multiple validation errors found: {JsonConvert.SerializeObject(dataQualityResult)}");

            return new PersonMatchResponse
            {
                Result = new()
                {
                    MatchStatus = MatchStatus.Error,
                },
                DataQuality = dataQualityResult
            };
        }
        
        var result = await MatchAsync(personSpecification);

        return new PersonMatchResponse
        {
            Result = new()
            {
                MatchStatus = result.Status,
                Score = result.Result?.Score,
                NhsNumber = result.Result?.NhsNumber,
                ProcessStage = result.ProcessStage,
            },
            DataQuality = dataQualityResult
        };
    }

    private async Task<MatchResult> MatchAsync(PersonSpecification model)
    {
        var queries = GetSearchQueries(model);

        for (var i = 0; i < queries.Length; i++)
        {
            var query = queries[i];
            
            logger.LogInformation($"Performing search query ({i}) again Nhs Fhir API");
            
            var searchResult = await nhsFhirClient.PerformSearch(query);
            if (searchResult != null)
            {
                if (searchResult.Type == SearchResult.ResultType.Matched)
                {
                    var status = MatchStatus.NoMatch;
                    if (searchResult.Score.GetValueOrDefault() >= 0.95m)
                    {
                        status = MatchStatus.Match;
                    } 
                    else if (searchResult.Score.GetValueOrDefault() >= 0.85m)
                    {
                        status = MatchStatus.PotentialMatch;
                    }
                    
                    logger.LogInformation($"Search query ({i}) resulted in status '{status}'");
                    
                    return new MatchResult(searchResult, status, i); // single match with confidence score
                }
                else if (searchResult.Type == SearchResult.ResultType.MultiMatched)
                {
                    logger.LogInformation($"Search query ({i}) resulted in status '{MatchStatus.ManyMatch}'");
                    
                    return new MatchResult(searchResult, MatchStatus.ManyMatch, i); // multiple matches
                }
            }
        }
        
        logger.LogInformation($"Search query ({queries.Length-1}) resulted in status '{MatchStatus.NoMatch}'");

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

    public PersonMatchResponse.DataQualityResult ToQualityResult(PersonSpecification spec, IEnumerable<ValidationResponse.ValidationResult> validationResults)
    {
        var result = new PersonMatchResponse.DataQualityResult();
        
        foreach (var vResult in validationResults)
        {
            if (result.Given == PersonMatchResponse.QualityType.Valid)
            {
                if (vResult.MemberNames.Contains("Given"))
                {
                    if (vResult.ErrorMessage?.Equals(PersonValidationConstants.GivenNameRequired) ?? false)
                    {
                        result.Given = PersonMatchResponse.QualityType.NotProvided;
                    }
                    else if (vResult.ErrorMessage?.Equals(PersonValidationConstants.GivenNameInvalid) ?? false)
                    {
                        result.Given = PersonMatchResponse.QualityType.Invalid;
                    }
                }
            }

            if (result.Family == PersonMatchResponse.QualityType.Valid)
            {
                if (vResult.MemberNames.Contains("Family"))
                {
                    if (vResult.ErrorMessage?.Equals(PersonValidationConstants.FamilyNameRequired) ?? false)
                    {
                        result.Family = PersonMatchResponse.QualityType.NotProvided;
                    }
                    else if (vResult.ErrorMessage?.Equals(PersonValidationConstants.FamilyNameInvalid) ?? false)
                    {
                        result.Family = PersonMatchResponse.QualityType.Invalid;
                    }
                }
            }

            if (result.Birthdate == PersonMatchResponse.QualityType.Valid)
            {
                if (vResult.MemberNames.Contains("Birthdate"))
                {
                    if (vResult.ErrorMessage?.Equals(PersonValidationConstants.BirthDateRequired) ?? false)
                    {
                        result.Birthdate = PersonMatchResponse.QualityType.NotProvided;
                    }
                    else if (vResult.ErrorMessage?.Equals(PersonValidationConstants.BirthDateInvalid) ?? false)
                    {
                        result.Birthdate = PersonMatchResponse.QualityType.Invalid;
                    }
                }
            }

            if (result.Gender == PersonMatchResponse.QualityType.Valid)
            {
                if (vResult.MemberNames.Contains("Gender"))
                {
                    if (vResult.ErrorMessage?.Equals(PersonValidationConstants.GenderInvalid) ?? false)
                    {
                        result.Gender = PersonMatchResponse.QualityType.Invalid;

                        // Remove from search query, if invalid
                        spec.Gender = null;
                    }
                }
                else if (string.IsNullOrEmpty(spec.Gender))
                {
                    result.Gender = PersonMatchResponse.QualityType.NotProvided;
                }
            }

            if (result.Phone == PersonMatchResponse.QualityType.Valid)
            {
                if (vResult.MemberNames.Contains("Phone"))
                {
                    if (vResult.ErrorMessage?.Equals(PersonValidationConstants.PhoneInvalid) ?? false)
                    {
                        result.Phone = PersonMatchResponse.QualityType.Invalid;

                        // Remove from search query, if invalid
                        spec.Phone = null;
                    }
                }
                else if (string.IsNullOrEmpty(spec.Phone))
                {
                    result.Phone = PersonMatchResponse.QualityType.NotProvided;
                }
            }

            if (result.Email == PersonMatchResponse.QualityType.Valid) {
                if (vResult.MemberNames.Contains("Email"))
                {
                    if (vResult.ErrorMessage?.Equals(PersonValidationConstants.EmailInvalid) ?? false)
                    {
                        result.Email = PersonMatchResponse.QualityType.Invalid;

                        // Remove from search query, if invalid
                        spec.Email = null;
                    }
                }
                else if (string.IsNullOrEmpty(spec.Email))
                {
                    result.Email = PersonMatchResponse.QualityType.NotProvided;
                }
            }

            if (result.AddressPostalCode == PersonMatchResponse.QualityType.Valid)
            {
                if (vResult.MemberNames.Contains("AddressPostalCode"))
                {
                    if (vResult.ErrorMessage?.Equals(PersonValidationConstants.PostCodeInvalid) ?? false)
                    {
                        result.AddressPostalCode = PersonMatchResponse.QualityType.Invalid;

                        // Remove from search query, if invalid
                        spec.AddressPostalCode = null;
                    }
                }
                else if (string.IsNullOrEmpty(spec.AddressPostalCode))
                {
                    result.AddressPostalCode = PersonMatchResponse.QualityType.NotProvided;
                }
            }
        }
        
        return result;
    }
    
    public bool HasMinDataRequirements(PersonMatchResponse.DataQualityResult dataQualityResult)
    {
        return dataQualityResult.Given == PersonMatchResponse.QualityType.Valid &&
               dataQualityResult.Family == PersonMatchResponse.QualityType.Valid &&
               dataQualityResult.Birthdate == PersonMatchResponse.QualityType.Valid;
    }
}
