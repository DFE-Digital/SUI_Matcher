using Eclipse.GraphQL;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using StrawberryShake;

using SUI.Client.Core.Application.Interfaces;
using SUI.Client.Core.Application.Models;

namespace SUI.Client.GraphQLProcessJob.Infrastructure;

public class GraphQlProcessor(
    ILogger<GraphQlProcessor> logger,
    IEclipseClient eclipseClient,
    IOptions<GraphQlProcessJobOptions> options)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Running Graph QL Process Job.");
        int pageNumber = 1;
        const int pageSize = 10;
        bool hasMoreResults = true;

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

                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation(
                            "Person Id: {ID}, Name: {Forename} {Surname}, DOB: {DateOfBirth}, Gender: {Gender}, NHS No: {NHSNumber}, Postcode: {Postcode}",
                            person.Id, person.Forename, person.Surname, person.DateOfBirth?.Lower, person.Gender,
                            person.NhsNumber,
                            person.Addresses.FirstOrDefault(a => a.Id == person.PreferredAddress?.Id)?.Location
                                ?.Postcode);
                    }
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

        logger.LogInformation("Finished processing all data.");
    }
}