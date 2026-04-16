using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;

namespace SUI.Client.StorageProcessJob.Application.Interfaces;

public interface ISuccessMatchFileWriter
{
    Task WriteAsync(
        string sourceBlobName,
        IReadOnlyCollection<ProcessedMatchRecord<CsvRecordDto>> matchedResults,
        CancellationToken cancellationToken
    );
}
