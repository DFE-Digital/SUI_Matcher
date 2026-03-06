using System.Text.RegularExpressions;

using Shared.Models;

namespace SUI.Client.Core.Application.UseCases.ReconcilePeople;

public static class ReconciliationStatsProcessor
{
    public static void RecordMatchStatusStats(ReconciliationCsvProcessStats stats, MatchStatus? matchStatus)
    {
        stats.Count++;
        switch (matchStatus)
        {
            case MatchStatus.Match:
                stats.MatchingStatusMatch++;
                break;
            case MatchStatus.NoMatch:
                stats.MatchingStatusNoMatch++;
                break;
            case MatchStatus.PotentialMatch:
                stats.MatchingStatusPotentialMatch++;
                break;
            case MatchStatus.LowConfidenceMatch:
                stats.MatchingStatusLowConfidenceMatch++;
                break;
            case MatchStatus.ManyMatch:
                stats.MatchingStatusManyMatch++;
                break;
            default:
                stats.MatchingStatusError++;
                break;
        }
    }

    public static void RecordReconciliationStatusStats(ReconciliationCsvProcessStats stats, ReconciliationStatus? status, string differenceList)
    {
        switch (status)
        {
            case ReconciliationStatus.NoDifferences:
                stats.NoDifferenceCount++;
                break;
            case ReconciliationStatus.Differences:
                UpdateStatsForField(differenceList, stats, "BirthDate", s => s.BirthDateCount++, s => s.BirthDateNhsCount++, s => s.BirthDateLaCount++, s => s.BirthDateBothCount++);
                UpdateStatsForField(differenceList, stats, "Email", s => s.EmailCount++, s => s.EmailNhsCount++, s => s.EmailLaCount++, s => s.EmailBothCount++);
                UpdateStatsForField(differenceList, stats, "Phone", s => s.PhoneCount++, s => s.PhoneNhsCount++, s => s.PhoneLaCount++, s => s.PhoneBothCount++);
                UpdateStatsForField(differenceList, stats, "Given", s => s.GivenNameCount++, s => s.GivenNameNhsCount++, s => s.GivenNameLaCount++, s => s.GivenNameBothCount++);
                UpdateStatsForField(differenceList, stats, "Family", s => s.FamilyNameCount++, s => s.FamilyNameNhsCount++, s => s.FamilyNameLaCount++, s => s.FamilyNameBothCount++);
                UpdateStatsForField(differenceList, stats, "AddressPostalCode", s => s.PostCodeCount++, s => s.PostCodeNhsCount++, s => s.PostCodeLaCount++, s => s.PostCodeBothCount++);
                UpdateStatsForField(differenceList, stats, "MatchingNhsNumber", s => s.MatchingNhsNumberCount++, s => s.MatchingNhsNumberNhsCount++, s => s.MatchingNhsNumberLaCount++, s => s.MatchingNhsNumberBothCount++);

                stats.DifferencesCount++;
                break;
            case ReconciliationStatus.LocalNhsNumberIsSuperseded:
                stats.SupersededNhsNumber++;
                break;
            case ReconciliationStatus.LocalDemographicsDidNotMatchToAnNhsNumber:
                stats.MissingNhsNumber++;
                break;
            case ReconciliationStatus.LocalNhsNumberIsNotFoundInNhs:
                stats.PatientNotFound++;
                break;
            case ReconciliationStatus.LocalNhsNumberIsNotValid:
                stats.InvalidNhsNumber++;
                break;
            default:
                stats.ErroredCount++;
                break;
        }
    }
    
    private static void UpdateStatsForField(
        string differenceList,
        ReconciliationCsvProcessStats stats,
        string fieldName,
        Action<ReconciliationCsvProcessStats> incrementPlain,
        Action<ReconciliationCsvProcessStats> incrementNhs,
        Action<ReconciliationCsvProcessStats> incrementLa,
        Action<ReconciliationCsvProcessStats> incrementBoth)
    {
        var plainRegex = new Regex($@"\b{fieldName}\b(?!:)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(300));

        if (plainRegex.IsMatch(differenceList)) { incrementPlain(stats); }
        if (differenceList.Contains($"{fieldName}:NHS")) { incrementNhs(stats); }
        if (differenceList.Contains($"{fieldName}:LA")) { incrementLa(stats); }
        if (differenceList.Contains($"{fieldName}:Both")) { incrementBoth(stats); }
    }
}