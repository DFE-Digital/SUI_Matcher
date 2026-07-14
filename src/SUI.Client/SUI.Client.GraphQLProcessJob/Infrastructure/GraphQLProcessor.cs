using Eclipse.GraphQL;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using StrawberryShake;

using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;
using SUI.Client.Core.Infrastructure.CsvParsers;

namespace SUI.Client.GraphQLProcessJob.Infrastructure;

public class GraphQlProcessor(
    ILogger<GraphQlProcessor> logger,
    IEclipseClient eclipseClient,
    IMatchPersonRecordOrchestrator<CsvRecordDto> matchPersonRecordOrchestrator,
    IOptions<GraphQlProcessJobOptions> options,
    IOptions<CsvMatchDataOptions> csvMatchDataOptions)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Running Graph QL Process Job.");
        var mappings = csvMatchDataOptions.Value.ColumnMappings;

        var (csvRecords, personObjectVersions) = await FetchAndCompilePersonRecordsAsync(mappings, cancellationToken);

        logger.LogInformation("Completed compiling GraphQL records. Total records retrieved: {Count}.",
            csvRecords.Count);

        var matchedResults = await matchPersonRecordOrchestrator.ProcessAsync(
            csvRecords,
            "graphql_extract",
            cancellationToken
        );

        logger.LogInformation(
            "Finished processing matching with orchestrator. Result count: {Count}. Matches: {MatchCount}",
            matchedResults.Count, matchedResults.Count(x => x.ApiResult is
            {
                Result.IsHighConfidenceMatch: true
            }));

        await SaveMatchedNhsNumbersAsync(matchedResults, personObjectVersions, mappings, cancellationToken);
    }

    private async Task<(List<CsvRecordDto> CsvRecords, Dictionary<string, int> PersonObjectVersions)> FetchAndCompilePersonRecordsAsync(
        CsvMatchDataOptions.Headers mappings,
        CancellationToken cancellationToken)
    {
        int pageNumber = 1;
        const int pageSize = 10;
        var csvRecords = new List<CsvRecordDto>();
        var personObjectVersions = new Dictionary<string, int>();

        while (!cancellationToken.IsCancellationRequested)
        {
            var results = await eclipseClient.PersonByCriteria.ExecuteAsync(options.Value.MaxAge,
                new RequestCursorInput { PageNumber = pageNumber, PageSize = pageSize }, cancellationToken);
            results.EnsureNoErrors();

            var resultsList = results.Data?.PersonByCriteria?.Results;
            if (resultsList == null || resultsList.Count == 0)
            {
                break;
            }

            foreach (var result in resultsList)
            {
                if (result is IPersonByCriteria_PersonByCriteria_Results_Person person)
                {
                    personObjectVersions[person.Id] = person.ObjectVersion;
                    csvRecords.Add(new CsvRecordDto(MapPersonToDictionary(person, mappings)));
                }
            }

            var cursor = results.Data?.PersonByCriteria?.Cursor;
            if (cursor == null || cursor.Offset + cursor.Returned >= cursor.TotalSize)
            {
                break;
            }

            pageNumber++;
        }

        return (csvRecords, personObjectVersions);
    }

    private Dictionary<string, string> MapPersonToDictionary(
        IPersonByCriteria_PersonByCriteria_Results_Person person,
        CsvMatchDataOptions.Headers mappings)
    {
        var personDictionary = new Dictionary<string, string>
        {
            { mappings.Id, person.Id },
            { mappings.Given, person.Forename ?? "" },
            { mappings.Family, person.Surname ?? "" },
            { mappings.BirthDate, person.DateOfBirth?.Lower?.ToString(csvMatchDataOptions.Value.DateFormat) ?? "" },
            { mappings.Postcode, GetPreferredPostcode(person) }
        };

        if (!string.IsNullOrEmpty(mappings.NhsNumber))
        {
            personDictionary[mappings.NhsNumber] = person.NhsNumber ?? "";
        }

        if (!string.IsNullOrEmpty(mappings.Gender))
        {
            personDictionary[mappings.Gender] = person.Gender?.ToString().ToLower() ?? "";
        }

        return personDictionary;
    }

    private static string GetPreferredPostcode(IPersonByCriteria_PersonByCriteria_Results_Person person) =>
        person.Addresses.FirstOrDefault(a => a.Id == person.PreferredAddress?.Id)?.Location?.Postcode ?? "";

    private async Task SaveMatchedNhsNumbersAsync(
        IEnumerable<ProcessedMatchRecord<CsvRecordDto>> matchedResults,
        Dictionary<string, int> personObjectVersions,
        CsvMatchDataOptions.Headers mappings,
        CancellationToken cancellationToken)
    {
        foreach (var result in matchedResults)
        {
            if (result.ApiResult is not { Result.IsHighConfidenceMatch: true } ||
                string.IsNullOrEmpty(result.ApiResult.Result.NhsNumber))
            {
                continue;
            }

            var personId = result.OriginalData.Record[mappings.Id];
            var matchedNhsNumber = result.ApiResult.Result.NhsNumber;

            if (!personObjectVersions.TryGetValue(personId, out var objectVersion))
            {
                logger.LogWarning("Could not find ObjectVersion for Person {PersonId}. Skipping NHS number update.", personId);
                continue;
            }

            logger.LogInformation("Saving matched NHS number {NhsNumber} for Person {PersonId} with ObjectVersion {ObjectVersion}.",
                matchedNhsNumber, personId, objectVersion);

            try
            {
                var updateInput = new UpdatePerson
                {
                    Id = personId,
                    NhsNumber = matchedNhsNumber,
                    ObjectVersion = objectVersion
                };

                var updateResult = await eclipseClient.UpdatePerson.ExecuteAsync(updateInput, cancellationToken);
                updateResult.EnsureNoErrors();

                logger.LogInformation("Successfully saved NHS number for Person {PersonId}.", personId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to save NHS number for Person {PersonId}.", personId);
            }
        }
    }
}