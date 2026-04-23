using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;

namespace SUI.Client.StorageProcessJob.Application.Interfaces;

public interface IMatchResultsService
{
    /// <summary>
    /// Filter on successful matches with a confident score
    /// and export a new CSV file in blob storage.
    /// <para>Records that fail validation fail silently and are logged</para>
    /// </summary>
    Task ExportSuccessResultsAsync(
        MatchResultsBlobNames blobNames,
        string sourceBlobName,
        IReadOnlyCollection<ProcessedMatchRecord<CsvRecordDto>> matchedResults,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Append result headers to the original data and export to blob storage.
    /// </summary>
    Task ExportFullResultsAsync(
        MatchResultsBlobNames blobNames,
        string sourceBlobName,
        IReadOnlyCollection<ProcessedMatchRecord<CsvRecordDto>> matchedResults,
        CancellationToken cancellationToken
    );
}
