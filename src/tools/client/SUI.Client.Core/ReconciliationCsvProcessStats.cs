using SUI.Client.Core.Models;

namespace SUI.Client.Core;

public class ReconciliationCsvProcessStats : IStats
{
    public int Count { get; set; }
    public int ErroredCount { get; set; }
    public int NoDifferenceCount { get; set; }
    public int OneDifferenceCount { get; set; }
    public int ManyDifferencesCount { get; set; }
    public int SupersededNhsNumber { get; set; }
    public int InvalidNhsNumber { get; set; }
    public int PatientNotFound { get; set; }
    public int MissingNhsNumber { get; set; }

    private readonly Lazy<double> _erroredPercentage;
    private readonly Lazy<double> _noDifferencePercentage;
    private readonly Lazy<double> _oneDifferencePercentage;
    private readonly Lazy<double> _manyDifferencesPercentage;
    private readonly Lazy<double> _supersededNhsNumberPercentage;
    private readonly Lazy<double> _invalidNhsNumberPercentage;
    private readonly Lazy<double> _patientNotFoundPercentage;
    private readonly Lazy<double> _missingNhsNumberPercentage;

    public ReconciliationCsvProcessStats()
    {
        _erroredPercentage = new Lazy<double>(() => ComputePercentage(ErroredCount));
        _noDifferencePercentage = new Lazy<double>(() => ComputePercentage(NoDifferenceCount));
        _oneDifferencePercentage = new Lazy<double>(() => ComputePercentage(OneDifferenceCount));
        _manyDifferencesPercentage = new Lazy<double>(() => ComputePercentage(ManyDifferencesCount));
        _supersededNhsNumberPercentage = new Lazy<double>(() => ComputePercentage(SupersededNhsNumber));
        _invalidNhsNumberPercentage = new Lazy<double>(() => ComputePercentage(InvalidNhsNumber));
        _patientNotFoundPercentage = new Lazy<double>(() => ComputePercentage(PatientNotFound));
        _missingNhsNumberPercentage = new Lazy<double>(() => ComputePercentage(MissingNhsNumber));
    }

    private double ComputePercentage(int count) => Count == 0 ? 0 : Math.Round((double)count / Count * 100, 2);

    public double ErroredPercentage => _erroredPercentage.Value;
    public double NoDifferencePercentage => _noDifferencePercentage.Value;
    public double OneDifferencePercentage => _oneDifferencePercentage.Value;
    public double ManyDifferencesPercentage => _manyDifferencesPercentage.Value;
    public double SupersededNhsNumberPercentage => _supersededNhsNumberPercentage.Value;
    public double InvalidNhsNumberPercentage => _invalidNhsNumberPercentage.Value;
    public double PatientNotFoundPercentage => _patientNotFoundPercentage.Value;
    public double MissingNhsNumberPercentage => _missingNhsNumberPercentage.Value;


    public void ResetStats()
    {
        Count = 0;
        ErroredCount = 0;
        NoDifferenceCount = 0;
        OneDifferenceCount = 0;
        ManyDifferencesCount = 0;
        SupersededNhsNumber = 0;
        InvalidNhsNumber = 0;
        PatientNotFound = 0;
        MissingNhsNumber = 0;
    }
}