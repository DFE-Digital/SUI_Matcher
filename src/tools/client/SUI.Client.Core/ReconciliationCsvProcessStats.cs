namespace SUI.Client.Core;

public class ReconciliationCsvProcessStats
{
    public int Count { get; set; }
    public int ErroredCount { get; set; }
    public int NoDifferenceCount { get; set; }
    public int OneDifferenceCount { get; set; }
    public int ManyDifferencesCount { get; set; }
    public int SupersededNhsNumber { get; set; }

    private readonly Lazy<double> _erroredPercentage;
    private readonly Lazy<double> _noDifferencePercentage;
    private readonly Lazy<double> _oneDifferencePercentage;
    private readonly Lazy<double> _manyDifferencesPercentage;
    private readonly Lazy<double> _supersededNhsNumberPercentage;

    public ReconciliationCsvProcessStats()
    {
        _erroredPercentage = new Lazy<double>(() => ComputePercentage(ErroredCount));
        _noDifferencePercentage = new Lazy<double>(() => ComputePercentage(NoDifferenceCount));
        _oneDifferencePercentage = new Lazy<double>(() => ComputePercentage(OneDifferenceCount));
        _manyDifferencesPercentage = new Lazy<double>(() => ComputePercentage(ManyDifferencesCount));
        _supersededNhsNumberPercentage = new Lazy<double>(() => ComputePercentage(SupersededNhsNumber));
    }

    private double ComputePercentage(int count) => Count == 0 ? 0 : Math.Round((double)count / Count * 100, 2);

    public double ErroredPercentage => _erroredPercentage.Value;
    public double MatchedPercentage => _noDifferencePercentage.Value;
    public double PotentialMatchPercentage => _oneDifferencePercentage.Value;
    public double ManyMatchPercentage => _manyDifferencesPercentage.Value;
    public double NoMatchPercentage => _supersededNhsNumberPercentage.Value;
}