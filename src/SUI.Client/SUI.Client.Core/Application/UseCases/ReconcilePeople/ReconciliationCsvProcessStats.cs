using System.Text.RegularExpressions;

using Shared.Models;

using SUI.Client.Core.Infrastructure.FileSystem;

namespace SUI.Client.Core.Application.UseCases.ReconcilePeople;

public class ReconciliationCsvProcessStats : IStats
{
    public int Count { get; set; }
    public int ErroredCount { get; set; }
    public int NoDifferenceCount { get; set; }
    public int DifferencesCount { get; set; }
    public int SupersededNhsNumber { get; set; }
    public int InvalidNhsNumber { get; set; }
    public int PatientNotFound { get; set; }
    public int MissingNhsNumber { get; set; }
    public int BirthDateCount { get; set; }
    public int BirthDateNhsCount { get; set; }
    public int BirthDateLaCount { get; set; }
    public int BirthDateBothCount { get; set; }

    public int EmailCount { get; set; }
    public int EmailNhsCount { get; set; }
    public int EmailLaCount { get; set; }
    public int EmailBothCount { get; set; }

    public int PhoneCount { get; set; }
    public int PhoneNhsCount { get; set; }
    public int PhoneLaCount { get; set; }
    public int PhoneBothCount { get; set; }

    public int GivenNameCount { get; set; }
    public int GivenNameNhsCount { get; set; }
    public int GivenNameLaCount { get; set; }
    public int GivenNameBothCount { get; set; }

    public int FamilyNameCount { get; set; }
    public int FamilyNameNhsCount { get; set; }
    public int FamilyNameLaCount { get; set; }
    public int FamilyNameBothCount { get; set; }

    public int PostCodeCount { get; set; }
    public int PostCodeNhsCount { get; set; }
    public int PostCodeLaCount { get; set; }
    public int PostCodeBothCount { get; set; }
    public int MatchingStatusMatch { get; set; }
    public int MatchingStatusPotentialMatch { get; set; }
    public int MatchingStatusLowConfidenceMatch { get; set; }
    public int MatchingStatusNoMatch { get; set; }
    public int MatchingStatusError { get; set; }
    public int MatchingStatusManyMatch { get; set; }

    public int MatchingNhsNumberCount { get; set; }
    public int MatchingNhsNumberNhsCount { get; set; }
    public int MatchingNhsNumberLaCount { get; set; }
    public int MatchingNhsNumberBothCount { get; set; }

    public int PrimaryAddressSame { get; set; }
    public int AddressHistoriesIntersect { get; set; }
    public int PrimaryCMSAddressInPDSHistory { get; set; }
    public int PrimaryPDSAddressInCMSHistory { get; set; }

    private readonly Lazy<double> _erroredPercentage;
    private readonly Lazy<double> _noDifferencePercentage;
    private readonly Lazy<double> _differencesPercentage;
    private readonly Lazy<double> _supersededNhsNumberPercentage;
    private readonly Lazy<double> _invalidNhsNumberPercentage;
    private readonly Lazy<double> _patientNotFoundPercentage;
    private readonly Lazy<double> _missingNhsNumberPercentage;
    private readonly Lazy<double> _matchingSuccessPercentage;
    private readonly Lazy<double> _primaryAddressSamePercentage;
    private readonly Lazy<double> _addressHistoriesIntersectPercentage;
    private readonly Lazy<double> _primaryCMSAddressInPDSHistoryPercentage;
    private readonly Lazy<double> _primaryPDSAddressInCMSHistoryPercentage;

    public ReconciliationCsvProcessStats()
    {
        _erroredPercentage = new Lazy<double>(() => ComputePercentage(ErroredCount));
        _noDifferencePercentage = new Lazy<double>(() => ComputePercentage(NoDifferenceCount));
        _differencesPercentage = new Lazy<double>(() => ComputePercentage(DifferencesCount));
        _supersededNhsNumberPercentage = new Lazy<double>(() => ComputePercentage(SupersededNhsNumber));
        _invalidNhsNumberPercentage = new Lazy<double>(() => ComputePercentage(InvalidNhsNumber));
        _patientNotFoundPercentage = new Lazy<double>(() => ComputePercentage(PatientNotFound));
        _missingNhsNumberPercentage = new Lazy<double>(() => ComputePercentage(MissingNhsNumber));
        _matchingSuccessPercentage = new Lazy<double>(() => ComputePercentage(MatchingStatusMatch));
        _primaryAddressSamePercentage = new Lazy<double>(() => ComputePercentage(PrimaryAddressSame));
        _addressHistoriesIntersectPercentage = new Lazy<double>(() => ComputePercentage(AddressHistoriesIntersect));
        _primaryCMSAddressInPDSHistoryPercentage = new Lazy<double>(() => ComputePercentage(PrimaryCMSAddressInPDSHistory));
        _primaryPDSAddressInCMSHistoryPercentage = new Lazy<double>(() => ComputePercentage(PrimaryPDSAddressInCMSHistory));
    }

    private double ComputePercentage(int count) => Count == 0 ? 0 : Math.Round((double)count / Count * 100, 2);

    public double ErroredPercentage => _erroredPercentage.Value;
    public double NoDifferencePercentage => _noDifferencePercentage.Value;
    public double ManyDifferencesPercentage => _differencesPercentage.Value;
    public double SupersededNhsNumberPercentage => _supersededNhsNumberPercentage.Value;
    public double InvalidNhsNumberPercentage => _invalidNhsNumberPercentage.Value;
    public double PatientNotFoundPercentage => _patientNotFoundPercentage.Value;
    public double MissingNhsNumberPercentage => _missingNhsNumberPercentage.Value;
    public double MatchingSuccessPercentage => _matchingSuccessPercentage.Value;
    public double PrimaryAddressSamePercentage => _primaryAddressSamePercentage.Value;
    public double AddressHistoriesIntersectPercentage => _addressHistoriesIntersectPercentage.Value;
    public double PrimaryCMSAddressInPDSHistoryPercentage => _primaryCMSAddressInPDSHistoryPercentage.Value;
    public double PrimaryPDSAddressInCMSHistoryPercentage => _primaryPDSAddressInCMSHistoryPercentage.Value;

    public void ResetStats()
    {
        Count = 0;
        ErroredCount = 0;
        NoDifferenceCount = 0;
        DifferencesCount = 0;
        SupersededNhsNumber = 0;
        InvalidNhsNumber = 0;
        PatientNotFound = 0;
        MissingNhsNumber = 0;
        BirthDateCount = 0;
        BirthDateNhsCount = 0;
        BirthDateLaCount = 0;
        BirthDateBothCount = 0;
        EmailCount = 0;
        EmailNhsCount = 0;
        EmailLaCount = 0;
        EmailBothCount = 0;
        PhoneCount = 0;
        PhoneNhsCount = 0;
        PhoneLaCount = 0;
        PhoneBothCount = 0;
        GivenNameCount = 0;
        GivenNameNhsCount = 0;
        GivenNameLaCount = 0;
        GivenNameBothCount = 0;
        FamilyNameCount = 0;
        FamilyNameNhsCount = 0;
        FamilyNameLaCount = 0;
        FamilyNameBothCount = 0;
        PostCodeCount = 0;
        PostCodeNhsCount = 0;
        PostCodeLaCount = 0;
        PostCodeBothCount = 0;
        MatchingStatusMatch = 0;
        MatchingStatusPotentialMatch = 0;
        MatchingStatusNoMatch = 0;
        MatchingStatusError = 0;
        MatchingStatusManyMatch = 0;
        PrimaryAddressSame = 0;
        AddressHistoriesIntersect = 0;
        PrimaryCMSAddressInPDSHistory = 0;
        PrimaryPDSAddressInCMSHistory = 0;
    }
    
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
    
    public static void RecordAddressStats(ReconciliationCsvProcessStats stats, AddressComparisonResult addressComparisonResult)
    {

        if (addressComparisonResult.PrimaryAddressSame)
        {
            stats.PrimaryAddressSame++;
        }

        if (addressComparisonResult.AddressHistoriesIntersect)
        {
            stats.AddressHistoriesIntersect++;
        }

        if (addressComparisonResult.PrimaryCMSAddressInPDSHistory)
        {
            stats.PrimaryCMSAddressInPDSHistory++;
        }

        if (addressComparisonResult.PrimaryPDSAddressInCMSHistory)
        {
            stats.PrimaryPDSAddressInCMSHistory++;
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