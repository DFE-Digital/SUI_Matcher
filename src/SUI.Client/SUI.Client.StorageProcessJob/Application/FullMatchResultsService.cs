using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;
using Shared.Models;
using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;
using SUI.Client.StorageProcessJob.Application.Interfaces;

namespace SUI.Client.StorageProcessJob.Application;

public sealed class FullMatchResultsService(
    IBlobStorageClient blobStorageClient,
    IOptions<StorageProcessJobOptions> options
) : IFullMatchResultsService
{
    private const string StatusHeader = "SUI_Status";
    private const string ScoreHeader = "SUI_Score";
    private const string NhsNumberHeader = "SUI_NHSNo";
    private const string CsvContentType = "text/csv";

    public Task ExportFullResultsAsync(
        string destinationBlobName,
        string sourceBlobName,
        IReadOnlyCollection<ProcessedMatchRecord<CsvRecordDto>> matchedResults,
        CancellationToken cancellationToken
    )
    {
        var csvContent = BuildCsv(matchedResults);

        return blobStorageClient.UploadBlobAsync(
            options.Value.ProcessedContainerName,
            destinationBlobName,
            BinaryData.FromString(csvContent),
            CsvContentType,
            cancellationToken
        );
    }

    private static string BuildCsv(
        IReadOnlyCollection<ProcessedMatchRecord<CsvRecordDto>> matchedResults
    )
    {
        var originalHeaders =
            matchedResults.FirstOrDefault()?.OriginalData.Record.Keys.ToList() ?? [];

        var builder = new StringBuilder();
        using var textWriter = new StringWriter(builder, CultureInfo.InvariantCulture);
        using var csvWriter = new CsvWriter(
            textWriter,
            new CsvConfiguration(CultureInfo.InvariantCulture) { NewLine = Environment.NewLine }
        );

        foreach (var header in originalHeaders)
        {
            csvWriter.WriteField(header);
        }

        csvWriter.WriteField(StatusHeader);
        csvWriter.WriteField(ScoreHeader);
        csvWriter.WriteField(NhsNumberHeader);
        csvWriter.NextRecord();

        foreach (var matchedResult in matchedResults)
        {
            foreach (var header in originalHeaders)
            {
                matchedResult.OriginalData.Record.TryGetValue(header, out var value);
                csvWriter.WriteField(value);
            }

            csvWriter.WriteField(MapStatus(matchedResult));
            csvWriter.WriteField(
                matchedResult.ApiResult?.Result?.Score?.ToString(CultureInfo.InvariantCulture)
            );
            csvWriter.WriteField(matchedResult.ApiResult?.Result?.NhsNumber);
            csvWriter.NextRecord();
        }

        return builder.ToString();
    }

    private static string MapStatus(ProcessedMatchRecord<CsvRecordDto> matchedResult)
    {
        if (matchedResult.ApiResult?.Result is not null)
        {
            return matchedResult.ApiResult.Result.MatchStatus.ToString();
        }

        return matchedResult.IsSuccess ? string.Empty : MatchStatus.Error.ToString();
    }
}
