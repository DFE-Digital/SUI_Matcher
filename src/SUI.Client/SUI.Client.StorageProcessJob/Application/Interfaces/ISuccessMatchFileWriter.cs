using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;

namespace SUI.Client.StorageProcessJob.Application.Interfaces;

public interface ISuccessMatchFileWriter
{
    /// <summary>
    /// Writer to filter out and write successful matches with a confident score
    /// to a new CSV file in blob storage.
    /// <para>Records that fail validation fail silently and are logged</para>
    /// </summary>
    Task WriteAsync(
        string sourceBlobName,
        IReadOnlyCollection<ProcessedMatchRecord<CsvRecordDto>> matchedResults,
        CancellationToken cancellationToken
    );
}
