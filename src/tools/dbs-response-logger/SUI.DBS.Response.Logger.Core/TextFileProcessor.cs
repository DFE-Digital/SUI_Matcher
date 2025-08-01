﻿using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Shared.Util;

using SUI.DBS.Response.Logger.Core.Models;

namespace SUI.DBS.Response.Logger.Core;

public interface ITxtFileProcessor
{
    Task ProcessFileAsync(string filePath);
}

public class TxtFileProcessor(ILogger<TxtFileProcessor> logger) : ITxtFileProcessor
{
    private static void AssertFileExists(string inputFilePath)
    {
        if (!File.Exists(inputFilePath))
        {
            throw new FileNotFoundException("File not found", inputFilePath);
        }
    }

    public async Task ProcessFileAsync(string filePath)
    {
        using var activity = new Activity("ProcessDbsTxtFile");

        activity.Start();

        AssertFileExists(filePath);

        string[] lines = await File.ReadAllLinesAsync(filePath);

        var recordColumns = Enum.GetValues<RecordColumn>();

        var recordCount = 0;
        var matches = 0;
        var noMatches = 0;

        // Use foreach loop to process each line
        foreach (string line in lines)
        {
            var recordData = line.Replace("\"", "").Split(",");

            if (recordData.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            ++recordCount;

            var result = ParseMatchPersonResult(recordColumns, recordData);

            StoreUniqueSearchIdFor(result);

            var matched = !string.IsNullOrWhiteSpace(result.NhsNumber);

            if (matched)
            {
                ++matches;
            }
            else
            {
                ++noMatches;
            }

            var ageGroup = !string.IsNullOrWhiteSpace(result.BirthDate)
                ? GetAgeGroup(ToDateOnly(result.BirthDate)!.Value)
                : "Unknown";

            logger.LogInformation(
                "[MATCH_COMPLETED] MatchStatus: {MatchStatus}, AgeGroup: {AgeGroup}, Gender: {Gender}, Postcode: {Postcode}",
                matched ? "Match" : "NoMatch",
                ageGroup,
                PersonSpecificationUtils.ToGenderFromNumber(result.Gender),
                ToPostCode(result.PostCode)
            );
        }

        logger.LogInformation("The DBS results file has {RecordCount} records, batch search resulted in Match='{Matches}' and NoMatch='{NoMatches}'"
            , recordCount
            , matches
            , noMatches);

        activity.Stop();
    }

    private static MatchPersonResult ParseMatchPersonResult(RecordColumn[] recordColumns, string[] recordData)
    {
        var result = new MatchPersonResult();

        foreach (var recordColumn in recordColumns)
        {
            var column = recordData[(int)recordColumn];

            switch (recordColumn)
            {
                case RecordColumn.Given:
                    result.Given = column;
                    break;
                case RecordColumn.Family:
                    result.Family = column;
                    break;
                case RecordColumn.BirthDate:
                    result.BirthDate = column;
                    break;
                case RecordColumn.Gender:
                    result.Gender = column;
                    break;
                case RecordColumn.PostCode:
                    result.PostCode = column;
                    break;
                case RecordColumn.NhsNumber:
                    result.NhsNumber = column;
                    break;
            }
        }

        return result;
    }

    private static DateOnly? ToDateOnly(string? value)
        => !string.IsNullOrWhiteSpace(value) && DateOnly.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date) ? date : null;

    public static string ToPostCode(string? value)
        => !string.IsNullOrWhiteSpace(value) ? value : "Unknown";
    public static string GetAgeGroup(DateOnly birthDate)
    {
        var dateOnlyNow = DateOnly.FromDateTime(DateTime.Now);
        var age = dateOnlyNow.Year - birthDate.Year;
        if (dateOnlyNow.DayOfYear < birthDate.DayOfYear)
        {
            age--;
        }

        return age switch
        {
            < 1 => "Less than 1 year",
            <= 3 => "1-3 years",
            <= 7 => "4-7 years",
            <= 11 => "8-11 years",
            <= 15 => "12-15 years",
            <= 18 => "16-18 years",
            _ => "Over 18 years"
        };
    }

    private static void StoreUniqueSearchIdFor(MatchPersonResult matchPersonResult)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(matchPersonResult));
        byte[] hashBytes = MD5.HashData(bytes);

        StringBuilder builder = new();

        foreach (var t in hashBytes)
        {
            builder.Append(t.ToString("x2"));
        }

        var hash = builder.ToString();

        Activity.Current?.SetBaggage("SearchId", hash);
    }


}