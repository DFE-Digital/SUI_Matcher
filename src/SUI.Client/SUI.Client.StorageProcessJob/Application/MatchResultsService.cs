using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Models;
using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;
using SUI.Client.Core.Infrastructure.CsvParsers;
using SUI.Client.StorageProcessJob.Application.Interfaces;

namespace SUI.Client.StorageProcessJob.Application;

public sealed class MatchResultsService(
    ILogger<MatchResultsService> logger,
    IBlobStorageClient blobStorageClient,
    IOptions<StorageProcessJobOptions> storageOptions,
    IOptions<CsvMatchDataOptions> csvMatchOptions
) : IMatchResultsService
{
    private const string CsvContentType = "text/csv";

    public async Task ExportSuccessResultsAsync(
        MatchResultsBlobNames blobNames,
        string sourceBlobName,
        IReadOnlyCollection<ProcessedMatchRecord<CsvRecordDto>> matchedResults,
        CancellationToken cancellationToken
    )
    {
        var successfulMatches = new List<SuccessfulMatchRecord>();

        var highConfidenceMatches = matchedResults
            .Where(record =>
                record is { IsSuccess: true, ApiResult.Result.IsHighConfidenceMatch: true }
            )
            .ToList();

        logger.LogInformation(
            "Found {SuccessfulMatchesCount} successful matches with high confidence in blob {SourceBlobName}.",
            highConfidenceMatches.Count,
            sourceBlobName
        );

        foreach (var matchedResult in highConfidenceMatches)
        {
            var successfulMatch = TryMapSuccessfulMatchRecord(sourceBlobName, matchedResult);

            if (successfulMatch is not null)
            {
                successfulMatches.Add(successfulMatch);
            }
        }

        if (successfulMatches.Count == 0)
        {
            return;
        }

        var csvContent = BuildSuccessCsv(successfulMatches);

        await blobStorageClient.UploadBlobAsync(
            storageOptions.Value.SuccessContainerName,
            blobNames.SuccessResultsBlobName,
            BinaryData.FromString(csvContent),
            CsvContentType,
            cancellationToken
        );
    }

    public async Task ExportFullResultsAsync(
        MatchResultsBlobNames blobNames,
        string sourceBlobName,
        IReadOnlyCollection<ProcessedMatchRecord<CsvRecordDto>> matchedResults,
        CancellationToken cancellationToken
    )
    {
        // Guard
        if (matchedResults.Count == 0)
        {
            logger.LogWarning("MatchResults is empty");
            return;
        }

        var csvContent = BuildFullResultsCsv(matchedResults);

        await blobStorageClient.UploadBlobAsync(
            storageOptions.Value.ProcessedContainerName,
            blobNames.FullResultsBlobName,
            BinaryData.FromString(csvContent),
            CsvContentType,
            cancellationToken
        );
    }

    private SuccessfulMatchRecord? TryMapSuccessfulMatchRecord(
        string sourceBlobName,
        ProcessedMatchRecord<CsvRecordDto> matchedRecord
    )
    {
        var inputIdHeader = csvMatchOptions.Value.ColumnMappings.Id;
        const string successNhsNumberHeader = "NhsNumber";
        const string nhsNoType = "NHSNo";

        if (
            !matchedRecord.OriginalData.Record.TryGetValue(inputIdHeader, out var id)
            || string.IsNullOrWhiteSpace(id)
        )
        {
            logger.LogWarning(
                "Skipping successful match record from blob {SourceBlobName} because required field {FieldName} is missing.",
                sourceBlobName,
                inputIdHeader
            );

            return null;
        }

        var nhsNumber = matchedRecord.ApiResult?.Result?.NhsNumber;

        if (string.IsNullOrWhiteSpace(nhsNumber))
        {
            logger.LogWarning(
                "Skipping successful match record from blob {SourceBlobName} for {InputIdHeader} {Id} because required field {FieldName} is missing.",
                sourceBlobName,
                inputIdHeader,
                id,
                successNhsNumberHeader
            );

            return null;
        }

        return new SuccessfulMatchRecord(id, nhsNoType, nhsNumber);
    }

    private static string BuildSuccessCsv(IReadOnlyCollection<SuccessfulMatchRecord> records)
    {
        const string successIdHeader = "LL ID";
        const string successTypeHeader = "Type";
        const string successNhsNumberHeader = "NhsNumber";

        var builder = new StringBuilder();
        using var textWriter = new StringWriter(builder, CultureInfo.InvariantCulture);
        using var csvWriter = new CsvWriter(
            textWriter,
            new CsvConfiguration(CultureInfo.InvariantCulture) { NewLine = Environment.NewLine }
        );

        csvWriter.WriteField(successIdHeader);
        csvWriter.WriteField(successTypeHeader);
        csvWriter.WriteField(successNhsNumberHeader);
        csvWriter.NextRecord();

        foreach (var record in records)
        {
            csvWriter.WriteField(record.Id);
            csvWriter.WriteField(record.Type);
            csvWriter.WriteField(record.NhsNumber);
            csvWriter.NextRecord();
        }

        return builder.ToString();
    }

    private static string BuildFullResultsCsv(
        IReadOnlyCollection<ProcessedMatchRecord<CsvRecordDto>> matchedResults
    )
    {
        const string fullStatusHeader = "SUI_Status";
        const string fullScoreHeader = "SUI_Score";
        const string fullNhsNumberHeader = "SUI_NHSNo";
        const string fullSearchIdHeader = "SUI_SearchId";

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

        csvWriter.WriteField(fullStatusHeader);
        csvWriter.WriteField(fullScoreHeader);
        csvWriter.WriteField(fullNhsNumberHeader);
        csvWriter.WriteField(fullSearchIdHeader);
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
                    ?? "-"
            );
            csvWriter.WriteField(matchedResult.ApiResult?.Result?.NhsNumber ?? "-");
            csvWriter.WriteField(matchedResult.ApiResult?.SearchId ?? "-");
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

        // Edge case: No Result means the API Match call returned null.
        return nameof(MatchStatus.Error);
    }
}
