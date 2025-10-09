using SUI.Client.Core.Models;

namespace SUI.Client.Core;

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
    public int MatchingStatusNoMatch { get; set; }
    public int MatchingStatusError { get; set; }
    public int MatchingStatusManyMatch { get; set; }

    private readonly Lazy<double> _erroredPercentage;
    private readonly Lazy<double> _noDifferencePercentage;
    private readonly Lazy<double> _differencesPercentage;
    private readonly Lazy<double> _supersededNhsNumberPercentage;
    private readonly Lazy<double> _invalidNhsNumberPercentage;
    private readonly Lazy<double> _patientNotFoundPercentage;
    private readonly Lazy<double> _missingNhsNumberPercentage;
    private readonly Lazy<double> _matchingSuccessPercentage;

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
    }
}