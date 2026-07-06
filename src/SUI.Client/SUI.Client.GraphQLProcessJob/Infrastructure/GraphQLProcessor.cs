using Eclipse.GraphQL;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using StrawberryShake;

using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Application.Models;
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
        int pageNumber = 1;
        const int pageSize = 10;
        bool hasMoreResults = true;
        var csvRecords = new List<CsvRecordDto>();
        var mappings = csvMatchDataOptions.Value.ColumnMappings;

        while (hasMoreResults && !cancellationToken.IsCancellationRequested)
        {
            var results = await eclipseClient.PersonByCriteria.ExecuteAsync(options.Value.MaxAge,
                new RequestCursorInput { PageNumber = pageNumber, PageSize = pageSize }, cancellationToken);
            results.EnsureNoErrors();

            if (results.Data?.PersonByCriteria.Results is { Count: > 0 } resultsList)
            {
                foreach (var result in resultsList)
                {
                    if (result is not IPersonByCriteria_PersonByCriteria_Results_Person person)
                    {
                        continue;
                    }

                    var personDictionary = new Dictionary<string, string>
                    {
                        { mappings.Id, person.Id },
                        { mappings.Given, person.Forename ?? "" },
                        { mappings.Family, person.Surname ?? "" },
                        { mappings.BirthDate, person.DateOfBirth?.Lower?.ToString("dd/MM/yyyy") ?? "" },
                        {
                            mappings.Postcode,
                            person.Addresses.FirstOrDefault(a => a.Id == person.PreferredAddress?.Id)?.Location
                                ?.Postcode ?? ""
                        }
                    };

                    if (!string.IsNullOrEmpty(mappings.NhsNumber))
                    {
                        personDictionary[mappings.NhsNumber] = person.NhsNumber ?? "";
                    }

                    if (!string.IsNullOrEmpty(mappings.Gender))
                    {
                        personDictionary[mappings.Gender] = person.Gender?.ToString() ?? "";
                    }

                    csvRecords.Add(new CsvRecordDto(personDictionary));
                }

                var cursor = results.Data?.PersonByCriteria.Cursor;
                if (cursor != null && cursor.Offset + cursor.Returned < cursor.TotalSize)
                {
                    pageNumber++;
                }
                else
                {
                    hasMoreResults = false;
                }
            }
            else
            {
                hasMoreResults = false;
            }
        }

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
    }
}