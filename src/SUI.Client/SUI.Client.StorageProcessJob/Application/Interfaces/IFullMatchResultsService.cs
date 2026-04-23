using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;

namespace SUI.Client.StorageProcessJob.Application.Interfaces;

public interface IFullMatchResultsService
{
    /// <Summary>
    /// Append result headers to the original data and then write to blob
    /// </summary>
    Task ExportFullResultsAsync(
        string destinationBlobName,
        string sourceBlobName,
        IReadOnlyCollection<ProcessedMatchRecord<CsvRecordDto>> matchedResults,
        CancellationToken cancellationToken
    );
}
