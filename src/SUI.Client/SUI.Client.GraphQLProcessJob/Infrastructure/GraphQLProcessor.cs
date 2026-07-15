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
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        logger.LogInformation("Running Graph QL Process Job.");
        var mappings = csvMatchDataOptions.Value.ColumnMappings;

        List<CsvRecordDto> csvRecords = await FetchAndCompilePersonRecordsAsync(mappings, cancellationToken);

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

        await SaveMatchedNhsNumbersAsync(matchedResults, mappings, cancellationToken);

        logger.LogInformation("GraphQL Job Complete. Total time: {ElapsedTime}", timer.Elapsed.ToString("g"));
    }

    private async Task<List<CsvRecordDto>> FetchAndCompilePersonRecordsAsync(
        CsvMatchDataOptions.Headers mappings,
        CancellationToken cancellationToken)
    {
        int pageNumber = 1;
        const int pageSize = 100;
        var csvRecords = new List<CsvRecordDto>();

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

        return csvRecords;
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
            { mappings.Postcode, GetPreferredPostcode(person) },
            { "__ObjectVersion", person.ObjectVersion.ToString() },
            { "__PersonTypes", string.Join(",", person.PersonTypes ?? []) }
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

            var existingNhsNumber = !string.IsNullOrEmpty(mappings.NhsNumber) &&
                                    result.OriginalData.Record.TryGetValue(mappings.NhsNumber, out var extNhs)
                                    ? extNhs
                                    : null;

            if (!string.IsNullOrEmpty(existingNhsNumber))
            {
                logger.LogInformation("Person {PersonId} already has NHS number. Skipping update.",
                    personId);
                continue;
            }

            if (!result.OriginalData.Record.TryGetValue("__ObjectVersion", out var objVerStr) ||
                !int.TryParse(objVerStr, out var objectVersion))
            {
                logger.LogWarning("Could not find ObjectVersion for Person {PersonId}. Skipping NHS number update.", personId);
                continue;
            }

            logger.LogInformation("Saving matched NHS number {NhsNumber} for Person {PersonId} with ObjectVersion {ObjectVersion}.",
                matchedNhsNumber, personId, objectVersion);

            try
            {
                var personTypesList = new List<PersonType>();
                if (result.OriginalData.Record.TryGetValue("__PersonTypes", out var typesStr) &&
                    !string.IsNullOrEmpty(typesStr))
                {
                    personTypesList = typesStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(Enum.Parse<PersonType>)
                        .ToList();
                }

                var updateInput = new UpdatePerson
                {
                    Id = personId,
                    NhsNumber = matchedNhsNumber,
                    ObjectVersion = objectVersion,
                    PersonTypes = personTypesList
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