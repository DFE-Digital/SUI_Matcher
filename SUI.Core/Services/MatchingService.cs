using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shared.Models;
using SUI.Core.Domain;
using SUI.Core.Endpoints;
using SUI.Types;

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
        StoreUniqueSearchIdFor(personSpecification);
        
        logger.LogInformation("Searching for matching person");
        
        var validationResults = validationService.Validate(personSpecification);

        var dataQualityResult = ToQualityResult(personSpecification, validationResults.Results!);
        
        logger.LogError($"Person data validation resulted in: {JsonConvert.SerializeObject(dataQualityResult.ToDictionary())}");

        if (!HasMinDataRequirements(dataQualityResult))
        {
            logger.LogError($"The minimized data requirements for a search weren't met, returning match status 'Error'");

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

        logger.LogInformation($"The person match request resulted in match status '{result.Status.ToString()}' " +
                              $"at process stage ({result.ProcessStage}), and the data quality was " +
                              $"{JsonConvert.SerializeObject(dataQualityResult.ToDictionary())}");

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

    private async Task<MatchResult2> MatchAsync(PersonSpecification model)
    {
        var queries = GetSearchQueries(model);

        for (var i = 0; i < queries.Length; i++)
        {
            var query = queries[i];
            
            logger.LogInformation($"Performing search query ({i}) against Nhs Fhir API");
            
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
                    
                    logger.LogInformation($"Search query ({i}) resulted in status '{status.ToString()}'");
                    
                    return new MatchResult2(searchResult, status, i); // single match with confidence score
                }
                else if (searchResult.Type == SearchResult.ResultType.MultiMatched)
                {
                    logger.LogInformation($"Search query ({i}) resulted in status 'ManyMatch'");
                    
                    return new MatchResult2(searchResult, MatchStatus.ManyMatch, i); // multiple matches
                }
            }
        }
        
        logger.LogInformation($"Search query ({queries.Length-1}) resulted in status 'NoMatch'");

        return new MatchResult2(MatchStatus.NoMatch);
    }

    private SearchQuery[] GetSearchQueries(PersonSpecification model)
    {
        if (!model.BirthDate.HasValue)
        {
            throw new InvalidOperationException("Birthdate is required for search queries");
        }

        var dobRange = new[] { "ge" + model.BirthDate.Value.AddMonths(-6).ToString("yyyy-MM-dd"), "le" + model.BirthDate.Value.AddMonths(6).ToString("yyyy-MM-dd") };
        var dob = new[] { "eq" + model.BirthDate.Value.ToString("yyyy-MM-dd") };

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
        if (model.BirthDate.Value.Day <= 12) // 5. fuzzy search with given name, family name and DOB. Day swapped with month if day equal to or less than 12.
        {
            var altDob = new DateTime(model.BirthDate.Value.Year, model.BirthDate.Value.Day, model.BirthDate.Value.Month);

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

    public static DataQualityResult ToQualityResult(PersonSpecification spec, IEnumerable<ValidationResponse.ValidationResult> validationResults)
    {
        var result = new DataQualityResult();
        
        foreach (var vResult in validationResults)
        {
            if (result.Given == QualityType.Valid)
            {
                if (vResult.MemberNames.Contains("Given", StringComparer.OrdinalIgnoreCase))
                {
                    if (vResult.ErrorMessage?.Equals(PersonValidationConstants.GivenNameRequired) ?? false)
                    {
                        result.Given = QualityType.NotProvided;
                    }
                    else if (vResult.ErrorMessage?.Equals(PersonValidationConstants.GivenNameInvalid) ?? false)
                    {
                        result.Given = QualityType.Invalid;
                    }
                }
            }

            if (result.Family == QualityType.Valid)
            {
                if (vResult.MemberNames.Contains("Family", StringComparer.OrdinalIgnoreCase))
                {
                    if (vResult.ErrorMessage?.Equals(PersonValidationConstants.FamilyNameRequired) ?? false)
                    {
                        result.Family = QualityType.NotProvided;
                    }
                    else if (vResult.ErrorMessage?.Equals(PersonValidationConstants.FamilyNameInvalid) ?? false)
                    {
                        result.Family = QualityType.Invalid;
                    }
                }
            }

            if (result.Birthdate == QualityType.Valid)
            {
                if (vResult.MemberNames.Contains("Birthdate", StringComparer.OrdinalIgnoreCase))
                {
                    if (vResult.ErrorMessage?.Equals(PersonValidationConstants.BirthDateRequired) ?? false)
                    {
                        result.Birthdate = QualityType.NotProvided;
                    }
                    else if (vResult.ErrorMessage?.Equals(PersonValidationConstants.BirthDateInvalid) ?? false)
                    {
                        result.Birthdate = QualityType.Invalid;
                    }
                }
            }

            if (result.Gender == QualityType.Valid)
            {
                if (vResult.MemberNames.Contains("Gender", StringComparer.OrdinalIgnoreCase))
                {
                    if (vResult.ErrorMessage?.Equals(PersonValidationConstants.GenderInvalid) ?? false)
                    {
                        result.Gender = QualityType.Invalid;

                        // Remove from search query, if invalid
                        spec.Gender = null;
                    }
                }
                else if (string.IsNullOrEmpty(spec.Gender))
                {
                    result.Gender = QualityType.NotProvided;
                }
            }

            if (result.Phone == QualityType.Valid)
            {
                if (vResult.MemberNames.Contains("Phone", StringComparer.OrdinalIgnoreCase))
                {
                    if (vResult.ErrorMessage?.Equals(PersonValidationConstants.PhoneInvalid) ?? false)
                    {
                        result.Phone = QualityType.Invalid;

                        // Remove from search query, if invalid
                        spec.Phone = null;
                    }
                }
                else if (string.IsNullOrEmpty(spec.Phone))
                {
                    result.Phone = QualityType.NotProvided;
                }
            }

            if (result.Email == QualityType.Valid) {
                if (vResult.MemberNames.Contains("Email", StringComparer.OrdinalIgnoreCase))
                {
                    if (vResult.ErrorMessage?.Equals(PersonValidationConstants.EmailInvalid) ?? false)
                    {
                        result.Email = QualityType.Invalid;

                        // Remove from search query, if invalid
                        spec.Email = null;
                    }
                }
                else if (string.IsNullOrEmpty(spec.Email))
                {
                    result.Email = QualityType.NotProvided;
                }
            }

            if (result.AddressPostalCode == QualityType.Valid)
            {
                if (vResult.MemberNames.Contains("AddressPostalCode", StringComparer.OrdinalIgnoreCase))
                {
                    if (vResult.ErrorMessage?.Equals(PersonValidationConstants.PostCodeInvalid) ?? false)
                    {
                        result.AddressPostalCode = QualityType.Invalid;

                        // Remove from search query, if invalid
                        spec.AddressPostalCode = null;
                    }
                }
                else if (string.IsNullOrEmpty(spec.AddressPostalCode))
                {
                    result.AddressPostalCode = QualityType.NotProvided;
                }
            }
        }
        
        return result;
    }
    
    private static void StoreUniqueSearchIdFor(PersonSpecification personSpecification)
    {
        using var md5 = MD5.Create();
        byte[] bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(personSpecification));
        byte[] hashBytes = md5.ComputeHash(bytes);
            
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            builder.Append(hashBytes[i].ToString("x2"));
        }
            
        var hash = builder.ToString();
        
        Activity.Current?.SetTag("SearchId", hash);
    }

    private bool HasMinDataRequirements(DataQualityResult dataQualityResult)
    {
        return dataQualityResult is
        {
            Given: QualityType.Valid, 
            Family: QualityType.Valid, 
            Birthdate: QualityType.Valid
        };
    }
}
