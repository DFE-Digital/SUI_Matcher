using Shared.Models;

using SUI.Client.Core.Application.Interfaces;

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

    public int BirthDateDifferentCount { get; set; }
    public int BirthDateNhsMissingCount { get; set; }
    public int BirthDateLaMissingCount { get; set; }
    public int BirthDateBothMissingCount { get; set; }

    public int EmailDifferenceCount { get; set; }
    public int EmailNhsMissingCount { get; set; }
    public int EmailLaMissingCount { get; set; }
    public int EmailBothMissingCount { get; set; }

    public int PhoneDifferenceCount { get; set; }
    public int PhoneNhsMissingCount { get; set; }
    public int PhoneLaMissingCount { get; set; }
    public int PhoneBothMissingCount { get; set; }

    public int GivenNameDifferenceCount { get; set; }
    public int GivenNameNhsMissingCount { get; set; }
    public int GivenNameLaMissingCount { get; set; }
    public int GivenNameBothMissingCount { get; set; }

    public int FamilyNameDifferenceCount { get; set; }
    public int FamilyNameNhsMissingCount { get; set; }
    public int FamilyNameLaMissingCount { get; set; }
    public int FamilyNameBothMissingCount { get; set; }

    public int PostCodeDifferenceCount { get; set; }
    public int PostCodeNhsMissingCount { get; set; }
    public int PostCodeLaMissingCount { get; set; }
    public int PostCodeBothMissingCount { get; set; }

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
        BirthDateDifferentCount = 0;
        BirthDateNhsMissingCount = 0;
        BirthDateLaMissingCount = 0;
        BirthDateBothMissingCount = 0;
        EmailDifferenceCount = 0;
        EmailNhsMissingCount = 0;
        EmailLaMissingCount = 0;
        EmailBothMissingCount = 0;
        PhoneDifferenceCount = 0;
        PhoneNhsMissingCount = 0;
        PhoneLaMissingCount = 0;
        PhoneBothMissingCount = 0;
        GivenNameDifferenceCount = 0;
        GivenNameNhsMissingCount = 0;
        GivenNameLaMissingCount = 0;
        GivenNameBothMissingCount = 0;
        FamilyNameDifferenceCount = 0;
        FamilyNameNhsMissingCount = 0;
        FamilyNameLaMissingCount = 0;
        FamilyNameBothMissingCount = 0;
        PostCodeDifferenceCount = 0;
        PostCodeNhsMissingCount = 0;
        PostCodeLaMissingCount = 0;
        PostCodeBothMissingCount = 0;
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

    public void RecordMatchStatusStats(MatchStatus? matchStatus)
    {
        Count++;
        switch (matchStatus)
        {
            case MatchStatus.Match:
                MatchingStatusMatch++;
                break;
            case MatchStatus.NoMatch:
                MatchingStatusNoMatch++;
                break;
            case MatchStatus.PotentialMatch:
                MatchingStatusPotentialMatch++;
                break;
            case MatchStatus.LowConfidenceMatch:
                MatchingStatusLowConfidenceMatch++;
                break;
            case MatchStatus.ManyMatch:
                MatchingStatusManyMatch++;
                break;
            default:
                MatchingStatusError++;
                break;
        }
    }

    public void RecordReconciliationStatusStats(ReconciliationStatus? status, string[] differences, string[] missingLocal, string[] missingNhs)
    {
        switch (status)
        {
            case ReconciliationStatus.NoDifferences:
                NoDifferenceCount++;
                break;
            case ReconciliationStatus.Differences:
                UpdateStatsForField(differences, missingLocal, missingNhs, "BirthDate", () => BirthDateDifferentCount++, () => BirthDateNhsMissingCount++, () => BirthDateLaMissingCount++, () => BirthDateBothMissingCount++);
                UpdateStatsForField(differences, missingLocal, missingNhs, "Email", () => EmailDifferenceCount++, () => EmailNhsMissingCount++, () => EmailLaMissingCount++, () => EmailBothMissingCount++);
                UpdateStatsForField(differences, missingLocal, missingNhs, "Phone", () => PhoneDifferenceCount++, () => PhoneNhsMissingCount++, () => PhoneLaMissingCount++, () => PhoneBothMissingCount++);
                UpdateStatsForField(differences, missingLocal, missingNhs, "Given", () => GivenNameDifferenceCount++, () => GivenNameNhsMissingCount++, () => GivenNameLaMissingCount++, () => GivenNameBothMissingCount++);
                UpdateStatsForField(differences, missingLocal, missingNhs, "Family", () => FamilyNameDifferenceCount++, () => FamilyNameNhsMissingCount++, () => FamilyNameLaMissingCount++, () => FamilyNameBothMissingCount++);
                UpdateStatsForField(differences, missingLocal, missingNhs, "AddressPostalCode", () => PostCodeDifferenceCount++, () => PostCodeNhsMissingCount++, () => PostCodeLaMissingCount++, () => PostCodeBothMissingCount++);
                UpdateStatsForField(differences, missingLocal, missingNhs, "NhsNumber", () => MatchingNhsNumberCount++, () => MatchingNhsNumberNhsCount++, () => MatchingNhsNumberLaCount++, () => MatchingNhsNumberBothCount++);

                break;
            case ReconciliationStatus.LocalNhsNumberIsSuperseded:
                SupersededNhsNumber++;
                break;
            case ReconciliationStatus.LocalDemographicsDidNotMatchToAnNhsNumber:
                MissingNhsNumber++;
                break;
            case ReconciliationStatus.LocalNhsNumberIsNotFoundInNhs:
                PatientNotFound++;
                break;
            case ReconciliationStatus.LocalNhsNumberIsNotValid:
                InvalidNhsNumber++;
                break;
            default:
                ErroredCount++;
                break;
        }
    }

    public void RecordAddressStats(AddressComparisonResult addressComparisonResult)
    {

        if (addressComparisonResult.PrimaryAddressSame)
        {
            PrimaryAddressSame++;
        }

        if (addressComparisonResult.AddressHistoriesIntersect)
        {
            AddressHistoriesIntersect++;
        }

        if (addressComparisonResult.PrimaryCMSAddressInPDSHistory)
        {
            PrimaryCMSAddressInPDSHistory++;
        }

        if (addressComparisonResult.PrimaryPDSAddressInCMSHistory)
        {
            PrimaryPDSAddressInCMSHistory++;
        }
    }

    private void UpdateStatsForField(
        string[] differences,
        string[] missingLocal,
        string[] missingNhs,
        string fieldName,
        Action incrementPlain,
        Action incrementNhs,
        Action incrementLa,
        Action incrementBoth)
    {
        bool inDifferences = differences.Contains(fieldName);
        bool inMissingLocal = missingLocal.Contains(fieldName);
        bool inMissingNhs = missingNhs.Contains(fieldName);


        // // Field is missing in both systems
        if (inMissingLocal && inMissingNhs)
        {
            incrementBoth();
        }
        // Field exists in both but has different values
        else if (inDifferences)
        {
            incrementPlain();
            DifferencesCount++;

        }
        // // Field is missing locally
        else if (inMissingLocal)
        {
            incrementLa();
        }
        // // Field is missing in NHS
        else if (inMissingNhs)
        {
            incrementNhs();
        }
    }


}