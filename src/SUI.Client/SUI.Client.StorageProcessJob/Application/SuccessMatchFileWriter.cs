using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SUI.Client.Core.Application.Models;
using SUI.Client.Core.Application.UseCases.MatchPeople;
using SUI.Client.StorageProcessJob.Application.Interfaces;

namespace SUI.Client.StorageProcessJob.Application;

public sealed class SuccessMatchFileWriter(
    TimeProvider timeProvider,
    ILogger<SuccessMatchFileWriter> logger,
    IBlobStorageClient blobStorageClient,
    IOptions<StorageProcessJobOptions> options
) : ISuccessMatchFileWriter
{
    private const string IdHeader = "LL ID";
    private const string TypeHeader = "Type";
    private const string NhsNumberHeader = "NhsNumber";
    private const string InputIdHeader = "Id";
    private const string NhsNoType = "NHSNo";
    private const string CsvContentType = "text/csv";

    /// <inheritdoc/>
    public async Task WriteAsync(
        string sourceBlobName,
        IReadOnlyCollection<ProcessedMatchRecord<CsvRecordDto>> matchedResults,
        CancellationToken cancellationToken
    )
    {
        var successfulMatches = new List<SuccessfulMatchRecord>();

        var areMatchedSuccess = matchedResults.Where(record =>
            record is { IsSuccess: true, ApiResult.Result.IsHighConfidenceMatch: true }
        );

        foreach (var matchedResult in areMatchedSuccess)
        {
            var successfulMatch = TryMapSuccessfulMatchRecord(sourceBlobName, matchedResult);

            if (successfulMatch is not null)
            {
                successfulMatches.Add(successfulMatch);
            }
        }

        var csvContent = BuildCsv(successfulMatches);
        var destinationBlobName = BuildSuccessBlobName(sourceBlobName);

        await blobStorageClient.UploadBlobAsync(
            options.Value.SuccessContainerName,
            destinationBlobName,
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
        if (
            !matchedRecord.OriginalData.Record.TryGetValue(InputIdHeader, out var id)
            || string.IsNullOrWhiteSpace(id)
        )
        {
            logger.LogWarning(
                "Skipping successful match record from blob {SourceBlobName} because required field {FieldName} is missing.",
                sourceBlobName,
                InputIdHeader
            );

            return null;
        }

        var nhsNumber = matchedRecord.ApiResult?.Result?.NhsNumber;

        if (string.IsNullOrWhiteSpace(nhsNumber))
        {
            logger.LogWarning(
                "Skipping successful match record from blob {SourceBlobName} for {InputIdHeader} {Id} because required field {FieldName} is missing.",
                sourceBlobName,
                InputIdHeader,
                id,
                NhsNumberHeader
            );

            return null;
        }

        return new SuccessfulMatchRecord(id, NhsNoType, nhsNumber);
    }

    private string BuildSuccessBlobName(string sourceBlobName)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceBlobName);
        var timestamp = timeProvider
            .GetUtcNow()
            .ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        return $"{timestamp}_{fileNameWithoutExtension}/{fileNameWithoutExtension}_success.csv";
    }

    private static string BuildCsv(IReadOnlyCollection<SuccessfulMatchRecord> records)
    {
        var builder = new StringBuilder();
        using var textWriter = new StringWriter(builder, CultureInfo.InvariantCulture);
        using var csvWriter = new CsvWriter(
            textWriter,
            new CsvConfiguration(CultureInfo.InvariantCulture) { NewLine = Environment.NewLine }
        );

        csvWriter.WriteField(IdHeader);
        csvWriter.WriteField(TypeHeader);
        csvWriter.WriteField(NhsNumberHeader);
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
}
