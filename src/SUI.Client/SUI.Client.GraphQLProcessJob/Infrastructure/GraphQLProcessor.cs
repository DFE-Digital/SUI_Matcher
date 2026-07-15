using System.Diagnostics;

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
    private record PersonMetadata(int ObjectVersion, string? NhsNumber, IReadOnlyList<PersonType> PersonTypes);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        logger.LogInformation("Running Graph QL Process Job.");
        var mappings = csvMatchDataOptions.Value.ColumnMappings;

        (List<CsvRecordDto> csvRecords, Dictionary<string, PersonMetadata> personMetadata) = await FetchAndCompilePersonRecordsAsync(mappings, cancellationToken);

        logger.LogInformation("Completed compiling GraphQL records. Total records retrieved: {Count}. Elapsed Time: {ElapsedTime}",
            csvRecords.Count, timer.Elapsed.ToString("g"));

        var matchedResults = await matchPersonRecordOrchestrator.ProcessAsync(
            csvRecords,
            "graphql_extract",
            cancellationToken
        );

        logger.LogInformation(
            "Finished processing matching. Result count: {Count}. Matches: {MatchCount}. Elapsed time: {ElapsedTime}",
            matchedResults.Count, matchedResults.Count(x => x.ApiResult is
            {
                Result.IsHighConfidenceMatch: true
            }), timer.Elapsed.ToString("g"));

        await SaveMatchedNhsNumbersAsync(matchedResults, personMetadata, mappings, cancellationToken);

        logger.LogInformation("GraphQL Job Complete. Total time: {ElapsedTime}", timer.Elapsed.ToString("g"));
    }

    private async Task<(List<CsvRecordDto> CsvRecords, Dictionary<string, PersonMetadata> PersonMetadata)> FetchAndCompilePersonRecordsAsync(
        CsvMatchDataOptions.Headers mappings,
        CancellationToken cancellationToken)
    {
        int pageNumber = 1;
        const int pageSize = 100;
        var csvRecords = new List<CsvRecordDto>();
        var personMetadata = new Dictionary<string, PersonMetadata>();

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
                    personMetadata[person.Id] = new PersonMetadata(
                        person.ObjectVersion,
                        person.NhsNumber,
                        person.PersonTypes ?? []
                    );
                    csvRecords.Add(new CsvRecordDto(MapPersonToDictionary(person, mappings)));
                }
            }

            var cursor = results.Data?.PersonByCriteria?.Cursor;
            if (cursor != null && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Processed Page: {Page}. Page size: {PageSize}. Total Records: {TotalRecords}",
                    cursor.PageNumber, cursor.PageSize, cursor.TotalSize);
            }

            if (cursor == null || cursor.Offset + cursor.Returned >= cursor.TotalSize)
            {
                break;
            }

            pageNumber++;
        }

        return (csvRecords, personMetadata);
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
        Dictionary<string, PersonMetadata> personMetadata,
        CsvMatchDataOptions.Headers mappings,
        CancellationToken cancellationToken)
    {
        foreach (var result in matchedResults)
        {
            if (result.ApiResult is not { Result.IsHighConfidenceMatch: true } ||
                string.IsNullOrEmpty(result.ApiResult.Result.NhsNumber))
            {
                logger.LogInformation("Match is low confidence, Skipping update.");
                continue;
            }

            var personId = result.OriginalData.Record[mappings.Id];
            var matchedNhsNumber = result.ApiResult.Result.NhsNumber;

            if (!personMetadata.TryGetValue(personId, out var metadata))
            {
                logger.LogWarning("Could not find metadata for Person {PersonId}. Skipping NHS number update.", personId);
                continue;
            }

            if (!string.IsNullOrEmpty(metadata.NhsNumber))
            {
                logger.LogInformation("Person {PersonId} already has NHS number {ExistingNhsNumber}. Skipping update.",
                    personId, metadata.NhsNumber);
                continue;
            }

            logger.LogInformation("Saving matched NHS number {NhsNumber} for Person {PersonId} with ObjectVersion {ObjectVersion}.",
                matchedNhsNumber, personId, metadata.ObjectVersion);

            try
            {
                var updateInput = new UpdatePerson
                {
                    Id = personId,
                    NhsNumber = matchedNhsNumber,
                    ObjectVersion = metadata.ObjectVersion,
                    PersonTypes = metadata.PersonTypes
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